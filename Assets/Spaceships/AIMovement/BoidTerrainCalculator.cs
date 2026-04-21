using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainCalculator
{
    private Vector3 steering = Vector3.zero;
    private FighterAI self;
    private float collisionDistanceSq;
    private int total = 0;

    private float collisionDistance = 0.5f * 0.5f;

    public TerrainCalculator(FighterAI self, float collisionDistanceSq)
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

        if (distanceSq < collisionDistance)
        {
            //self.OnShieldDestroyed();
            return;
        }

        float weight = Mathf.Clamp01(1f - (distanceSq / collisionDistanceSq));
        Vector3 goUp = new Vector3(0, weight, 0);

        steering += goUp;
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