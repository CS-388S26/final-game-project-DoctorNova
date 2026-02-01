using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipGun : MonoBehaviour
{
    public GameObject projectilePrefab;
    public List<GameObject> locations = new List<GameObject>();
    public float reloadTime = 1;

    public float lastTimeShot;
    int lastLocation = 0;

    private void Start()
    {
        lastTimeShot = reloadTime;
    }

    private void Update()
    {
        lastTimeShot += Time.deltaTime;
    }

    private int GetNextSpawnLocation()
    {
        return ++lastLocation % locations.Count;
    }

    // Update is called once per frame
    public Projectile Shoot()
    {
        if (lastTimeShot < reloadTime)
        {
            return null;
        }

        lastTimeShot = 0;

        Vector3 spawnPoint = locations[GetNextSpawnLocation()].transform.position;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPoint, transform.rotation);

        Projectile projectileComp = projectileObj.GetComponent<Projectile>();
        SpaceshipShield shield = GetComponent<SpaceshipShield>();
        if (shield == null)
        {
            shield = GetComponentInChildren<SpaceshipShield>();
        }
        projectileComp.shooter = shield.gameObject;

        return projectileComp;
    }

    public void ShootAt(FighterAI obj)
    {
        Projectile projectile = Shoot();
        if (projectile)
        {
            projectile.target = obj;
        }
    }

    public void ShootAt(Vector3 position)
    {
        Projectile projectile = Shoot();
        if (projectile)
        {
            projectile.transform.LookAt(position);
        }
    }
}
