using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipShield : MonoBehaviour
{
    public int health = 3;

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
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
        health -= damage;
    }
}
