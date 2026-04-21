using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeparationCalculator
{
    private Vector3 steering = Vector3.zero;
    private FighterAI self;
    private float collisionDistanceSq;
    private int total = 0;

    public SeparationCalculator(FighterAI self, float collisionDistanceSq)
    {
        this.self = self;
        this.collisionDistanceSq = collisionDistanceSq;
    }

    public void Evaluate(Vector3 otherPosition)
    {
        Vector3 diff = self.transform.position - otherPosition;
        float distanceSq = diff.sqrMagnitude;

        if (distanceSq > collisionDistanceSq)
            return;

        // If two agents are on top of each other, push in a random direction
        if (distanceSq == 0f)
        {
            diff = Random.insideUnitSphere.normalized;
        }

        float weight = Mathf.Clamp01(1f - (distanceSq / collisionDistanceSq));
        diff.Normalize();
        diff *= weight;

        steering += diff;
        total++;
    }

    // Calculate the final separation vector
    public Vector3 CalculateResult(float coefficient)
    {
        if (steering.sqrMagnitude == 0f || total == 0)
            return Vector3.zero;

        return steering * (coefficient / total);
    }

}