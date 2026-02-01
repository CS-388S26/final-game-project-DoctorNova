using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterAI : MonoBehaviour
{
    protected static List<FighterAI> allFighters = new List<FighterAI>();
    protected static List<FighterAI>[] teams = new List<FighterAI>[2]{
        new List<FighterAI>(),
        new List<FighterAI>()
    };

    public int maxSpeed = 2000;
    public float acceleration = 100;
    public float speed = 0;

    public Rigidbody rb;
    protected int enemyDetectionRange = 100;
    protected int enemyDetectionRangeSq = 0;
    public Vector3 desiredPosition = new Vector3();

    float lastTimeTargetUpdated = 0;
    float minTimeSinceTargetUpdate = 1;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemyDetectionRangeSq = enemyDetectionRange * enemyDetectionRange;

        lastTimeTargetUpdated = minTimeSinceTargetUpdate;
    }

    public List<FighterAI> GetTeam()
    {
        return teams[gameObject.layer % 2];
    }

    public List<FighterAI> GetEnemyTeam()
    {
        return teams[(gameObject.layer + 1) % 2];
    }

    public bool IsEnemy(GameObject other)
    {
        return gameObject.layer % 2 != other.layer % 2;
    }

    private void OnEnable()
    {
        allFighters.Add(this);
        GetTeam().Add(this);
    }

    private void OnDisable()
    {
        allFighters.Remove(this);
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
        // Don't update the desired destination every frame.
        lastTimeTargetUpdated += Time.fixedDeltaTime;
        if (lastTimeTargetUpdated >= minTimeSinceTargetUpdate)
        {
            lastTimeTargetUpdated = 0;
            FighterAI closestEnemy = FindClosestEnemy();
            if (closestEnemy)
            {
                desiredPosition = closestEnemy.transform.position - closestEnemy.transform.forward.normalized * 2;
            }
        }

        Vector3 direction = desiredPosition - transform.position;
        float currentSpeed = Mathf.Lerp(speed, maxSpeed, acceleration * Time.fixedDeltaTime);
        Vector3 desiredVelocity = direction.normalized * currentSpeed;

        rb.velocity = desiredVelocity;

        // Stop moving if very close to the target destination
        // This should not happens outside of debugging because every ship is moving
        if (direction.sqrMagnitude <= 1)
        {
            rb.velocity = new Vector3(0, 0, 0);
        }

        if (rb.velocity.sqrMagnitude > 0)
        {
            transform.LookAt(transform.position + rb.velocity.normalized);
            rb.velocity = Orca.ComputeSafeVelocity(new OrcaParams(), Time.fixedDeltaTime, this, allFighters);
        }

        speed = rb.velocity.magnitude;

    }
}
