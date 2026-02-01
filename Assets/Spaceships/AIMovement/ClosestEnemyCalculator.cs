using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosestEnemyCalculator
{
    FighterAI self;
    Vector3 closestEnemyDirection = new Vector3(0, 0, 0);
    float closestEnemyDistanceSq;

    public ClosestEnemyCalculator(FighterAI self, float closestEnemyDistanceSq)
    {
        this.self = self;
        this.closestEnemyDistanceSq = closestEnemyDistanceSq;
    }

    public void Evaluate(FighterAI other)
    {
        Vector3 direction = other.transform.position - self.transform.position;
        float distanceSq = direction.sqrMagnitude;
        if (distanceSq < closestEnemyDistanceSq)
        {
            closestEnemyDirection = direction;
            closestEnemyDistanceSq = distanceSq;
        }
    }

    public Vector3 CalculateResult(float coefficient)
    {
        return closestEnemyDirection.normalized * coefficient;
    }
}
