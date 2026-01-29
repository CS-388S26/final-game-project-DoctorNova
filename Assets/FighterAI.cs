using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterAI : MonoBehaviour
{
    protected static List<FighterAI>[] teams = new List<FighterAI>[2]{
        new List<FighterAI>(),
        new List<FighterAI>()
    };

    public int maxSpeed = 1500;
    public float acceleration = 10;

    protected Rigidbody rb;
    protected int enemyDetectionRange = 100;
    protected int enemyDetectionRangeSq = 0;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemyDetectionRangeSq = enemyDetectionRange * enemyDetectionRange;
    }

    public List<FighterAI> GetTeam() 
    {
        return teams[gameObject.layer % 2];
    }

    public List<FighterAI> GetEnemyTeam()
    {
        return teams[(gameObject.layer + 1) % 2];
    }

    private void OnEnable()
    {
        GetTeam().Add(this);
    }

    private void OnDisable()
    {
        GetTeam().Remove(this);
    }

    FighterAI FindClosestEnemy()
    {
        float closestDistance = enemyDetectionRangeSq;
        FighterAI closest = null;

        List<FighterAI> enemies = GetEnemyTeam();
        foreach (FighterAI enemy in enemies)
        {
            Vector3 distance = enemy.transform.position - rb.position;
            if (distance.sqrMagnitude < closestDistance)
            {
                closest = enemy;
            }
        }

        return closest;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        FighterAI closestEnemy = FindClosestEnemy();

        if (closestEnemy != null)
        {
            Vector3 direction = (closestEnemy.transform.position - transform.position) - closestEnemy.transform.forward.normalized * 2;
            Vector3 desiredVelocity = direction.normalized * maxSpeed;

            rb.velocity = Vector3.MoveTowards(
                rb.velocity,
                desiredVelocity,
                acceleration * Time.fixedDeltaTime
            );

            transform.LookAt(transform.position + rb.velocity.normalized);
        }
    }


}
