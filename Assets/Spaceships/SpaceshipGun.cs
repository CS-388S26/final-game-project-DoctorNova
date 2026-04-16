using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipGun : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject heavyProjectilePrefab;
    public List<GameObject> locations = new List<GameObject>();
    public float reloadTime = 1;
    public float heavyReloadTime = 2.0f;

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

    private Projectile SpawnProjectile(GameObject prefab)
    {
        Vector3 spawnPoint = locations[GetNextSpawnLocation()].transform.position;

        GameObject projectileObj = Instantiate(prefab, spawnPoint, transform.rotation);

        Projectile projectileComp = projectileObj.GetComponent<Projectile>();
        SpaceshipShield shield = GetComponent<SpaceshipShield>();
        if (shield == null)
        {
            shield = GetComponentInChildren<SpaceshipShield>();
        }
        projectileComp.shooter = shield.gameObject;

        return projectileComp;
    }

    // Update is called once per frame
    public List<Projectile> Shoot(bool heavyProjectile = false)
    {
        if (lastTimeShot < (heavyProjectile ? heavyReloadTime : reloadTime))
        {
            return new List<Projectile>();
        }

        lastTimeShot = 0;

        List<Projectile> projectiles = new List<Projectile>();
        if (heavyProjectile)
        {
            for(int i = 0; i < locations.Count; i++)
            {
                projectiles.Add(SpawnProjectile(heavyProjectilePrefab));
            }
        } else
        {
            projectiles.Add(SpawnProjectile(projectilePrefab));
        }

        return projectiles;
    }

    public void ShootAt(FighterAI obj, bool heavyProjectile = false)
    {
        List<Projectile> projectiles = Shoot(heavyProjectile);
        for (int i = 0; i < projectiles.Count; i++)
        {
            projectiles[i].target = obj;
        }
    }

    public void ShootAt(Vector3 position, bool heavyProjectile = false)
    {
        List<Projectile> projectiles = Shoot(heavyProjectile);
        for (int i = 0; i < projectiles.Count; i++)
        {
            projectiles[i].transform.LookAt(position);
        }
    }
}
