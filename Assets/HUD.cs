using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Image healthbar;
    public TextMeshProUGUI percentage;
    public TextMeshProUGUI status;
    public Camera mainCamera;
    public PlayerControls playerFighter;
    public Image crosshairOutline;
    public Image crosshairTarget;
    public Image crosshairNoTarget;
    public Canvas canvas;
    public Image enemyArrow;
    public float arrowPadding = 0.1f;
    public GameObject pauseMenu;
    public GameObject continueBtn;
    public TextMeshProUGUI pauseMenuTitle;
    public TextMeshProUGUI enemyCounter;
    public Image speedIndicator;

    Vector2 targetScreenCoordinate = new Vector2(0, 0);

    public float maxTargetRangeSq = 100 * 100;
    float minTargetDetectionInterval = 0.1f;
    float timeSinceLastTargetCheck = 0;

    float minTargetOffscreenUpdateInterval = 0.25f;
    float timeTargetOffscreenUpdate = 0;

    private void Start()
    {
        enemyArrow.gameObject.SetActive(false);
        pauseMenu.SetActive(false);
        SetHP(1, 1);
    }

    private bool IsPointInCircle(Vector3 point, Vector3 circleCenter, float radius)
    {
        Vector3 distance = point - circleCenter;
        return distance.sqrMagnitude < radius * radius;
    }

    void UpdateIndicators()
    {
        var enemies = playerFighter.GetEnemyTeam();
        RectTransform arrowRect = enemyArrow.rectTransform;
        bool arrowSet = false;

        for (int i = 0; i < enemies.Count; i++)
        {
            Transform t = enemies[i].transform;
            Vector3 vp = mainCamera.WorldToViewportPoint(t.position);

            // Off-screen check
            if (vp.z > 0 && vp.x >= 0 && vp.x <= 1 && vp.y >= 0 && vp.y <= 1)
                continue; // on-screen, skip

            // Fix behind-camera
            if (vp.z < 0)
            {
                vp.x = 1f - vp.x;
                vp.y = 1f - vp.y;
                vp.z = 0f;
            }

            // Clamp to screen edges
            float pad = arrowPadding;
            vp.x = Mathf.Clamp(vp.x, pad, 1f - pad);
            vp.y = Mathf.Clamp(vp.y, pad, 1f - pad);

            // Set position
            arrowRect.position = mainCamera.ViewportToScreenPoint(vp);

            // Set rotation
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowRect.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            arrowSet = true;
            break; // only show first off-screen enemy
        }

        enemyArrow.gameObject.SetActive(arrowSet);
    }

    public void PauseGame()
    {
        if (!pauseMenu)
            return;

        Time.timeScale = 0f;          // stop all physics, animations, and Update() calls that use Time.deltaTime
        pauseMenu.SetActive(true);    // show pause menu UI
        pauseMenuTitle.text = "Paused";
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;          // resume normal time
        pauseMenu.SetActive(false);   // hide pause menu UI
    }

    public void Restart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Won()
    {
        PauseGame();
        continueBtn.SetActive(false);
        pauseMenuTitle.text = "Victory";
    }

    public void Defeat()
    {
        if (!pauseMenu)
            return;

        PauseGame();
        continueBtn.SetActive(false);
        pauseMenuTitle.text = "Defeat";
    }

    private void CheckForTargetsInCrosshair()
    {
        playerFighter.target = null;
        float distance = float.MaxValue;
        Vector3 screenCoordinate = new Vector3(0, 0, 0);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        foreach (FighterAI fighter in playerFighter.GetEnemyTeam())
        {
            Vector3 direction = fighter.transform.position - playerFighter.transform.position;
            float newDistance = direction.sqrMagnitude;
            if (newDistance > maxTargetRangeSq || Vector3.Dot(direction.normalized, playerFighter.transform.forward) <= 0)
            {
                continue;
            }

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(fighter.transform.position);
            if (!IsPointInCircle(screenPosition, crosshairOutline.rectTransform.position, crosshairOutline.rectTransform.rect.width / 3))
            {
                continue;
            }

            if (!playerFighter.target || distance > newDistance)
            {
                distance = newDistance;
                playerFighter.target = fighter;
                screenCoordinate = screenPosition;
            }
        }

        if (playerFighter.target)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                crosshairOutline.rectTransform,
                screenCoordinate,
                null, // camera is null for Overlay canvas
                out targetScreenCoordinate
            );
        }
    }

    private void Update()
    {
        timeTargetOffscreenUpdate += Time.deltaTime;
        if (timeTargetOffscreenUpdate > minTargetOffscreenUpdateInterval)
        {
            timeTargetOffscreenUpdate = 0;
            UpdateIndicators();
        }

        // Don't need to check on every frame
        timeSinceLastTargetCheck += Time.deltaTime;
        if (timeSinceLastTargetCheck > minTargetDetectionInterval)
        {
            timeSinceLastTargetCheck = 0;
            CheckForTargetsInCrosshair();
        }

        if (playerFighter.target != null)
        {
            crosshairTarget.rectTransform.localPosition = targetScreenCoordinate;
            crosshairTarget.gameObject.SetActive(true);
            crosshairNoTarget.gameObject.SetActive(false);
        }
        else
        {
            crosshairTarget.gameObject.SetActive(false);
            crosshairNoTarget.gameObject.SetActive(true);  
        }

        enemyCounter.text = playerFighter.GetEnemyTeam().Count.ToString();
        speedIndicator.fillAmount = (playerFighter.speed - playerFighter.minSpeed) / (playerFighter.maxSpeed - playerFighter.minSpeed);

    }

    public void SetHP(float current, float max)
    {
        float p = current / max;
        healthbar.fillAmount = p;
        percentage.text = $"{Mathf.Round(p * 100)}%";
        
        if (p < 0.25)
        {
            status.text = "CRITICAL";
            status.color = percentage.color = healthbar.color = Color.red;
        }
        else if (p < 0.5)
        {
            status.text = "WARNING";
            status.color = percentage.color = healthbar.color = Color.yellow;
        }
        else
        {
            status.text = "OPTIMAL";
            status.color = percentage.color = healthbar.color = new Color(6f/256f, 182f/256f, 212f/256f);
        }
        
    }
}
