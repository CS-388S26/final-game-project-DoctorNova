using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager Instance;
    public Image loadingbar;
    public TextMeshProUGUI loadingState;
    public TextMeshProUGUI loadingPercentage;
    public Button startGameButton;

    public bool IsReady { get; private set; }
    public bool CanStart { get; private set; }

    public static bool IsGameReady()
    {
        return Instance == null || Instance.IsReady;
    }

    private AsyncOperation op;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Start()
    {
        startGameButton.gameObject.SetActive(false);
        loadingState.text = "Loading...";
        loadingbar.fillAmount = 0;
        StartCoroutine(LoadGameScene());
    }
    public void SetReady()
    {
        CanStart = true;
    }

    public static void StartGame()
    {
        if (!GameManager.Instance)
            return;

        GameManager.Instance.ChangeScene();
    }

    private void ChangeScene()
    {
        startGameButton.interactable = false;
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        // Allow the scene to activate
        op.allowSceneActivation = true;

        // Wait ONE frame so Unity activates it
        yield return null;

        // Deactivate the loading scene's audio listener
        Scene loadingScene = SceneManager.GetSceneByName("LoadingScene");
        foreach (GameObject go in loadingScene.GetRootGameObjects())
        {
            AudioListener listener = go.GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        // Now the scene actually exists and is activeable
        Scene gameScene = SceneManager.GetSceneByName("GameScene");

        if (gameScene.IsValid())
        {
            SceneManager.SetActiveScene(gameScene);
        }
        else
        {
            Debug.LogError("GameScene not found after activation!");
            yield break;
        }

        // WAIT AN EXTRA FRAME so lighting/shadows settle
        yield return null;
        // Force update of environment (realtime GI, reflection probes)
        DynamicGI.UpdateEnvironment();

        // Now it is SAFE to unload the loading scene
        yield return SceneManager.UnloadSceneAsync("LoadingScene");

        IsReady = true;
    }

    IEnumerator LoadGameScene()
    {
        op = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            // update progress bar here
            loadingbar.fillAmount = op.progress;
            loadingPercentage.text = $"{Mathf.Round(loadingbar.fillAmount * 100)}%";
            yield return null;
        }

        loadingbar.fillAmount = 1;
        loadingPercentage.text = "100%";
        loadingState.text = "Ready to launch";
        startGameButton.gameObject.SetActive(true);
        GameManager.Instance.SetReady();
    }
}
