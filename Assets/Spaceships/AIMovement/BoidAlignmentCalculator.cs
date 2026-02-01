using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AlignmentCalculator
{
    Vector3 steering = new Vector3(0, 0, 0);
    FighterAI self;
    float visualDistanceSq;
    int total = 0;

    public AlignmentCalculator(FighterAI self, float visualDistanceSq)
    {
        this.visualDistanceSq = visualDistanceSq;
        this.self = self;
    }

    public void Evaluate(FighterAI other)
    {
        float distanceSq = (self.transform.position - other.transform.position).sqrMagnitude;
        float weight = Mathf.Clamp(1 - (distanceSq / this.visualDistanceSq), 0, 1);
        this.steering += (other.transform.forward * weight);
        if (weight > 0)
        {
            this.total++;
        }
    }

    public Vector3 CalculateResult(float coefficient)
    {
        if (this.steering.sqrMagnitude == 0)
        {
            return this.steering;
        }

        return this.steering * (coefficient / this.total);
    }
}
