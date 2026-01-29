using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipGun : MonoBehaviour
{
    public GameObject projectPrefab;
    public GameObject location;
    public float reloadTime = 10;

    float lastTimeShot;
    int shots = 0;

    private void Start()
    {
        if (location == null)
        {
            location = gameObject;
        }

        lastTimeShot = reloadTime;
        shots = 0;
    }

    private void Update()
    {
        lastTimeShot += Time.deltaTime;
    }

    // Update is called once per frame
    public void Shoot()
    {
        if (lastTimeShot < reloadTime)
        {
            return;
        }

        lastTimeShot = 0;
        GameObject projectileObj = Instantiate(projectPrefab, location.transform.position, transform.rotation);

        Projectile projectileComp = projectileObj.GetComponent<Projectile>();
        SpaceshipShield shield = GetComponent<SpaceshipShield>();
        if (shield == null)
        {
            shield = GetComponentInChildren<SpaceshipShield>();
        }
        projectileComp.shooter = shield.gameObject;
        shots++;
    }
}
