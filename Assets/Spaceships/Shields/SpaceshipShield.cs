using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipShield : MonoBehaviour
{
    public int health = 3;
    int currentHealth = 0;
    public HUD hud;

    private void Start()
    {
        currentHealth = health;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth <= 0)
        {
            if (transform.parent)
            {
                Destroy(transform.parent.gameObject);
            } 
            else {
                Destroy(gameObject);
            }
        }
    }

    public void Damage(int damage)
    {
        currentHealth -= damage;

        if (hud)
        {
            hud.SetHP(currentHealth, health);
        }
    }
}
