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
    public Button startGameButton;
    public List<SpaceshipSpawner> spawners = new();
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
        startGameButton.gameObject.SetActive(false);
        loadingbar.fillAmount = 0;
        StartCoroutine(LoadGameScene());
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        startGameButton.gameObject.SetActive(false);
        loadingScreen.SetActive(false);
    }

    IEnumerator LoadGameScene()
    {
        float totalNumerOfSpaceships = 0;
        float currentlySpawned = 0;

        foreach(SpaceshipSpawner spawner in spawners)
        {
            totalNumerOfSpaceships += spawner.count;
        }

        foreach (SpaceshipSpawner spawner in spawners)
        {
            for (int i = 0; i < spawner.count; i++)
            {
                Vector3 spawnerPosition = spawner.transform.position;
                Vector3 position = spawnerPosition + Random.onUnitSphere * Random.Range(0, spawner.transform.localScale.x);
                Quaternion rotation = Random.rotation;
                Instantiate(spawner.spaceshipPrefab, position, rotation);
                currentlySpawned++;

                loadingbar.fillAmount = currentlySpawned / totalNumerOfSpaceships;
                loadingPercentage.text = $"{Mathf.Round(loadingbar.fillAmount * 100)}%";
            }
            yield return null;
        }

        // wait an extra frame
        yield return null;

        loadingbar.fillAmount = 1;
        loadingPercentage.text = "100%";
        startGameButton.gameObject.SetActive(true);
        IsReady = true;
    }
}
