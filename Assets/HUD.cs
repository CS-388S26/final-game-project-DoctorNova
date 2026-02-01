using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
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

    public TextMeshProUGUI readToFire;
    public TextMeshProUGUI reloading;

    Vector2 targetScreenCoordinate = new Vector2(0, 0);

    public float maxTargetRangeSq = 100 * 100;
    float minTargetDetectionInterval = 0.1f;
    float timeSinceLastTargetCheck = 0;

    private bool IsPointInCircle(Vector3 point, Vector3 circleCenter, float radius)
    {
        Vector3 distance = point - circleCenter;
        return distance.sqrMagnitude < radius * radius;
    }

    private void Start()
    {

    }

    private void CheckForTargetsInCrosshair()
    {
        playerFighter.target = null;
        float distance = float.MaxValue;
        Vector3 screenCoordinate = new Vector3(0, 0, 0);

        foreach (FighterAI fighter in playerFighter.GetEnemyTeam())
        {
            Vector3 direction = fighter.transform.position - playerFighter.transform.position;
            float newDistance = direction.sqrMagnitude;

            if (newDistance > maxTargetRangeSq || Vector3.Dot(direction.normalized, playerFighter.transform.forward) <= 0)
            {
                continue;
            }

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(fighter.transform.position);
            if (!IsPointInCircle(screenPosition, crosshairOutline.rectTransform.position, crosshairOutline.rectTransform.rect.width))
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

        bool isReloading = playerFighter.gun.lastTimeShot < playerFighter.gun.reloadTime;
        reloading.gameObject.SetActive(isReloading);
        readToFire.gameObject.SetActive(!isReloading);
    }

    public void SetHP(float current, float max)
    {
        float p = current / max;
        healthbar.fillAmount = p;
        percentage.text = $"{Mathf.Round(p * 100)}%";
        
        if (p < 0.25)
        {
            status.text = "CRITICAL";
        }
        else if (p < 0.5)
        {
            status.text = "WARNING";
        }
        else
        {
            status.text = "OPTIMAL";
        }
        
    }
}
