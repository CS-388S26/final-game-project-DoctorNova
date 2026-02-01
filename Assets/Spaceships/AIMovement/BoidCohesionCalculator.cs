using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CohesionCalculator
{
    private static readonly float maxForce = 0.05f;

    protected Vector3 center = Vector3.zero;
    private int total = 0;
    private FighterAI self;

    public CohesionCalculator(FighterAI self)
    {
        this.self = self;
    }

    // Accumulate positions of other agents
    public void Evaluate(FighterAI other)
    {
        center += other.transform.position; 
        total++;
    }

    // Calculate the cohesion steering vector
    public Vector3 CalculateResult(float coefficient)
    {
        if (total == 0)
        {
            return Vector3.zero;
        }

        Vector3 averagePosition = center / total;
        Vector3 steering = (averagePosition - self.transform.position).normalized - self.transform.forward;

        // Limit the force to maxForce
        if (steering.sqrMagnitude > maxForce * maxForce)
        {
            return steering.normalized * maxForce * coefficient;
        }

        return steering * coefficient;
    }

    // Optional: reset for the next frame
    public void Reset()
    {
        center = Vector3.zero;
        total = 0;
    }
}