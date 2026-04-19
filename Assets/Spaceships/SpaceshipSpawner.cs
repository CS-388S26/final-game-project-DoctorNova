using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipSpawner : MonoBehaviour
{
    public int count = 10;
    public GameObject spaceshipPrefab;

    public GameObject Spawn()
    {
        Vector3 spawnerPosition = transform.position;

        float xScaleHalf = transform.localScale.x / 2.0f;
        float yScaleHalf = transform.localScale.y / 2.0f;
        float zScaleHalf = transform.localScale.z / 2.0f;
        Vector3 randomVector = new Vector3(
                Random.Range(-xScaleHalf, xScaleHalf),
                Random.Range(-yScaleHalf, yScaleHalf),
                Random.Range(-zScaleHalf, zScaleHalf)
        );
        Vector3 position = spawnerPosition + randomVector;
        Quaternion rotation = Random.rotation;
        return Instantiate(spaceshipPrefab, position, rotation);
    }
}
