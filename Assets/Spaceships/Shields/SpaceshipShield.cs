using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SpaceshipShield : MonoBehaviour
{
    public int health = 3;
    int currentHealth = 0;
    public HUD hud;

    [SerializeField]
    private UnityEvent onShieldDestroyed;

    private void Start()
    {
        currentHealth = health;
    }

    public void Damage(int damage)
    {
        currentHealth -= damage;

        if (hud)
        {
            hud.SetHP(currentHealth, health);
        }

        if (currentHealth <= 0)
        {
            onShieldDestroyed?.Invoke();
        }
    }
}
