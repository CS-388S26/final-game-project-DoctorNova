using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMovementManager : MonoBehaviour
{
    [Header("Boid Settings")]
    public float cohesionCoefficient = 0.1f;
    public float separationCoefficient = 0.5f;
    public float alignmentCoefficient = 0.2f;
    public float enemyCoefficient = 0.3f;
    public float anchorCoefficient = 0.05f;
    public float separationDistance = 10f;
    public float maxConsiderationDistance = 50f;

    private SeparationCalculator separationCalculator;
    private CohesionCalculator cohesionCalculator;
    private AlignmentCalculator alignmentCalculator;
    private ClosestEnemyCalculator closestEnemyCalculator;

    public Vector3 anchorPoint = Vector3.zero;
    private const int maxAgentsPerFrame = 100;

    private void Start()
    {
        // Start the main boid update coroutine
        StartCoroutine(UpdateBoidsCoroutine());
    }

    private IEnumerator UpdateBoidsCoroutine()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        float maxConsiderationDistanceSq = maxConsiderationDistance * maxConsiderationDistance;

        while (true)
        {
            // Copy lists to avoid modification during iteration
            List<FighterAI> all = new List<FighterAI>(FighterAI.allFighters);
            List<FighterAI> enemies = new List<FighterAI>(FighterAI.teams[0]);
            List<FighterAI> allies = new List<FighterAI>(FighterAI.teams[1]);

            int totalCount = all.Count;
            int processed = 0;

            while (processed < totalCount)
            {
                int batchCount = Mathf.Min(maxAgentsPerFrame, totalCount - processed);

                for (int i = 0; i < batchCount; i++)
                {
                    FighterAI agent = all[processed + i];

                    if (!agent.useBoidMovement)
                    {
                        continue;
                    }

                    closestEnemyCalculator = new ClosestEnemyCalculator(agent, maxConsiderationDistanceSq);
                    cohesionCalculator = new CohesionCalculator(agent);
                    alignmentCalculator = new AlignmentCalculator(agent, maxConsiderationDistanceSq);
                    separationCalculator = new SeparationCalculator(agent, separationDistance * separationDistance);
                    foreach (var other in all)
                    {
                        if (other == agent) {
                            continue;
                        };

                        Vector3 distance = other.transform.position - agent.transform.position;
                        if (distance.sqrMagnitude >= maxConsiderationDistanceSq)
                        {
                            continue;
                        }

                        if (other.IsEnemy(agent.gameObject))
                        {
                            closestEnemyCalculator.Evaluate(other);
                        }
                        // --- COHESION & ALIGNMENT only with allies ---
                        else
                        {
                            cohesionCalculator.Evaluate(other);
                            alignmentCalculator.Evaluate(other);
                        }

                        // --- SEPARATION (all fighters) ---
                        separationCalculator.Evaluate(other);
                    }

                    // --- COMBINE FORCES ---
                    Vector3 closestEnemyForce = closestEnemyCalculator.CalculateResult(enemyCoefficient);
                    Vector3 separationForce = separationCalculator.CalculateResult(separationCoefficient);
                    Vector3 cohesionForce = cohesionCalculator.CalculateResult(cohesionCoefficient);
                    Vector3 alignmentForce = alignmentCalculator.CalculateResult(alignmentCoefficient);
                    Vector3 anchorForce = anchorPoint - agent.transform.position.normalized * anchorCoefficient;
                    Vector3 finalForce = anchorForce + cohesionForce + alignmentForce + separationForce + closestEnemyForce;

                    // --- APPLY TO AGENT ---
                    agent.desiredDirection = finalForce.normalized;
                }

                processed += batchCount;
                yield return null; // wait one frame before next batch
            }

            yield return wait; // wait for next frame
        }
    }
}
