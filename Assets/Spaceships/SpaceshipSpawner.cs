using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipSpawner : MonoBehaviour
{
    public int count = 10;
    public GameObject spaceshipPrefab;

    IEnumerator SpawnAll() { 
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnerPosition = transform.position;
            Vector3 position = spawnerPosition + Random.onUnitSphere * Random.Range(0, transform.localScale.x);
            Quaternion rotation = Random.rotation;
            Instantiate(spaceshipPrefab, position, rotation);
            yield return null;
        }
        count = 0;

        yield return null;
    }

    private void Start()
    {
        StartCoroutine(SpawnAll());
    }
}
