using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipShieldImpactSender : MonoBehaviour
{
    public GameObject shield;
    Material mat;

    const int maxImpacts = 32;
    Vector4[] impacts = new Vector4[maxImpacts];
    GameObject[] impactSources = new GameObject[maxImpacts];

    int FindNewImpactIndex(GameObject obj)
    {
        int index = 0;
        float smallestTime = float.MaxValue;

        for (int i = 1; i < maxImpacts; i++)
        {
            if (impacts[i].w != -2 && impacts[i].w < smallestTime)
            {
                smallestTime = impacts[i].w;
                index = i;
            }

            if (impactSources[i] == obj)
            {
                return i;
            }
        }

        return index;
    }

    int FindImpactIndex(GameObject obj)
    {
        for (int i = 0; i < maxImpacts; i++)
        {
            if (impactSources[i] == obj)
            {
                return i;
            }
        }

        return -1;
    }

    void AddImpact(int index, Vector3 worldPos, GameObject source, float time)
    {
        impacts[index] = new Vector4(worldPos.x, worldPos.y, worldPos.z, time);
        impactSources[index] = source;
        mat.SetVectorArray("_ImpactPoints", impacts);
        mat.SetInt("_ImpactCount", maxImpacts);
    }

    // Called once when the object is initialized
    void Awake()
    {
        if (shield == null)
        {
            mat = GetComponent<Renderer>().material;
        }
        else
        {
            mat = shield.GetComponent<Renderer>().material;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < maxImpacts; i++)
        {
            impacts[i].w = -1;
        }
    }

    private void OnDestroy()
    {
        // cleanup prevent memory leak
        Destroy(mat);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Getting first contact is good enough, no need to iterate over all the contact points
        AddImpact(FindNewImpactIndex(collision.gameObject), collision.GetContact(0).point, collision.gameObject, -2);
    }

    private void OnCollisionExit(Collision collision)
    {
        int index = FindImpactIndex(collision.gameObject);
        if (index >= 0)
        {
            AddImpact(index, impacts[index], collision.gameObject, Time.time);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // Ignore projectile send by the ship that this shield belongs too
        Projectile projectile = other.GetComponent<Projectile>();
        // Projectiles have a capsule inside of them with the collider so most likely the Projectile script is on the parent
        if (!projectile && other.transform.parent)
        {
            projectile = other.transform.parent.GetComponent<Projectile>();
        }

        if (projectile != null && (projectile.shooter == gameObject || transform.parent && transform.parent.gameObject == projectile.shooter))
        {
            return;
        }

        // Add impact point
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        AddImpact(FindNewImpactIndex(other.gameObject), contactPoint, other.gameObject, Time.time);
    }
}
