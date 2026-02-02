using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosestEnemyCalculator
{
    FighterAI self;
    Vector3 closestEnemyDirection = new Vector3(0, 0, 0);
    float closestEnemyDistanceSq;
    float maxDistanceSq;
    float separationDistance;

    public ClosestEnemyCalculator(FighterAI self, float closestEnemyDistanceSq, float separationDistance)
    {
        this.self = self;
        this.closestEnemyDistanceSq = this.maxDistanceSq = closestEnemyDistanceSq;
        this.separationDistance = separationDistance;
    }

    public void Evaluate(FighterAI other)
    {
        Vector3 direction = (other.transform.position - other.transform.forward * separationDistance) - self.transform.position;
        float distanceSq = direction.sqrMagnitude;
        if (distanceSq < closestEnemyDistanceSq)
        {
            closestEnemyDirection = direction;
            closestEnemyDistanceSq = distanceSq;
        }
    }

    public Vector3 CalculateResult(float coefficient)
    {
        return closestEnemyDirection.normalized * (1 - closestEnemyDistanceSq / maxDistanceSq) * coefficient;
    }
}
