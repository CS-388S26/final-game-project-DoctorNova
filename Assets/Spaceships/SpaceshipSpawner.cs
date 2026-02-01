using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipSpawner : MonoBehaviour
{
    public int count = 10;
    public GameObject spaceshipPrefab;
    public int maxPerFrame = 25;

    public float timeBetweenSpawning = 5;
    float timesinceLastSpawn = 0;

    // Start is called before the first frame update
    void Start()
    {
        timesinceLastSpawn = timeBetweenSpawning;
    }

    // Update is called once per frame
    void Update()
    {
        timesinceLastSpawn += Time.deltaTime;

        if (timesinceLastSpawn >= timeBetweenSpawning && count > 0)
        {
            int toSpawnThisFrame = timeBetweenSpawning == 0 ? maxPerFrame : count;
            for (int i = 0; i < toSpawnThisFrame && count > 0; i++)
            {
                Vector3 spawnerPosition = transform.position;
                Vector3 position = spawnerPosition + Random.onUnitSphere * Random.Range(0, transform.localScale.x);
                Quaternion rotation = Random.rotation;
                GameObject spaceship = Instantiate(spaceshipPrefab, position, rotation);
                SpaceshipShield shield = spaceship.GetComponentInChildren<SpaceshipShield>();
                count--;
                timesinceLastSpawn = 0;
            }
        }
    }
}
