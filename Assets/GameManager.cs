using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    private static LoadingScreen Instance;
    public Image loadingbar;
    public TextMeshProUGUI loadingPercentage;
    public List<Button> startGameButtons = new();
    public List<SpaceshipSpawner> spawners = new();
    public MeshGenerator terrain;
    public GameObject loadingScreen;

    public bool IsReady { get; private set; }

    public static bool IsGameReady()
    {
        return Instance == null || Instance.IsReady;
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        loadingScreen.SetActive(true);
    }

    public void Start()
    {
        Time.timeScale = 0f;
        SetStartButtons(false);
        loadingbar.fillAmount = 0;
        StartCoroutine(LoadGameScene());
    }

    private void SetStartButtons(bool active)
    {
        foreach (Button button in startGameButtons)
        {
            button.gameObject.SetActive(active);
        }
    }

    public void StartLevel1()
    {
        Time.timeScale = 1f;
        SetStartButtons(false);
        SceneManager.LoadScene(1, LoadSceneMode.Additive);
        loadingScreen.SetActive(false);
        terrain.IsGeneratingTerrain = false;
        terrain.gameObject.SetActive(false);
    }

    public void StartLevel2()
    {
        Time.timeScale = 1f;
        SetStartButtons(false);
        SceneManager.LoadScene(2, LoadSceneMode.Additive);
        loadingScreen.SetActive(false);
    }

    IEnumerator LoadGameScene()
    {
        float percentageOfTerrain = 50.0f;
        float totalNumerOfSpaceships = 0;
        float currentlySpawned = 0;

        foreach(SpaceshipSpawner spawner in spawners)
        {
            totalNumerOfSpaceships += spawner.count;
        }

        totalNumerOfSpaceships += percentageOfTerrain;

        foreach (SpaceshipSpawner spawner in spawners)
        {
            for (int i = 0; i < spawner.count; i++)
            {
                spawner.Spawn();
                currentlySpawned++;

                loadingbar.fillAmount = currentlySpawned / totalNumerOfSpaceships;
                loadingPercentage.text = $"{Mathf.Round(loadingbar.fillAmount * 100)}%";
            }
            yield return null;
        }

        terrain.GenerateTerrain();

        // wait an extra frame
        yield return null;

        loadingbar.fillAmount = 1;
        loadingPercentage.text = "100%";
        SetStartButtons(true);
        IsReady = true;
    }
}
