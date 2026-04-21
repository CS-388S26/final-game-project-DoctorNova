using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class AIMovementManager : MonoBehaviour
{
    [Header("Boid Settings")]
    public float cohesionCoefficient = 0.1f;
    public float separationCoefficient = 0.5f;
    public float alignmentCoefficient = 0.2f;
    public float enemyCoefficient = 0.55f;
    public float anchorCoefficient = 0.31f;
    public float separationDistance = 10f;
    public float maxConsiderationDistance = 50f;
    float maxConsiderationDistanceSq = 0;

    public MeshGenerator terrain;

    private SeparationCalculator separationCalculator;
    private TerrainCalculator terrainCalculator;
    private CohesionCalculator cohesionCalculator;
    private AlignmentCalculator alignmentCalculator;
    private ClosestEnemyCalculator closestEnemyCalculator;

    public Vector3 anchorPoint = Vector3.zero;
    private int maxAgentsPerFrame = 100;
    private int processedLastFrame = 0;

    public bool multiThreaded = false;

    private void Start()
    {
        maxConsiderationDistanceSq = maxConsiderationDistance * maxConsiderationDistance;

        if (multiThreaded)
        {
            // Start the main boid update coroutine
            StartCoroutine(UpdateBoidsCoroutine());
        }

        processedLastFrame = 0;
    }

    private void Update()
    {
        if (!multiThreaded)
        {
            RunAgentBatch();

            if (processedLastFrame >= FighterAI.allFighters.Count)
            {
                processedLastFrame = 0;
            }
        }
    }

    private void RunAgentBatch()
    {
        // Copy lists to avoid modification during iteration
        List<FighterAI> all = new List<FighterAI>(FighterAI.allFighters);

        int batchCount = Mathf.Min(maxAgentsPerFrame, all.Count - processedLastFrame);

        for (int i = 0; i < batchCount; i++)
        {
            FighterAI agent = all[processedLastFrame + i];

            if (agent == null || !agent.useBoidMovement)
            {
                continue;
            }

            closestEnemyCalculator = new ClosestEnemyCalculator(agent, maxConsiderationDistanceSq, separationDistance);
            cohesionCalculator = new CohesionCalculator(agent);
            alignmentCalculator = new AlignmentCalculator(agent, maxConsiderationDistanceSq);
            separationCalculator = new SeparationCalculator(agent, separationDistance * separationDistance);
            terrainCalculator = new TerrainCalculator(agent, separationDistance * separationDistance);

            if (terrain && terrain.IsGeneratingTerrain)
            {
                int gridX = (int)(agent.transform.position.x - (terrain.lastGridPosition.x - terrain.xHalfSize));
                int gridZ = (int)(agent.transform.position.z - (terrain.lastGridPosition.y - terrain.zHalfSize));
                int closestTerrainVertex = gridZ * (terrain.xSize + 1) + gridX;

                if (closestTerrainVertex >= 0 && closestTerrainVertex < terrain.vertices.Length)
                {
                    for (int j = 0; j < terrain.numberOfNeighbors; j++)
                    {
                        int n = terrain.neighbors[closestTerrainVertex, j];
                        if (n == -1)
                        {
                            break;
                        }

                        terrainCalculator.Evaluate(terrain.vertices[n]);
                    }
                } 
            }

            foreach (var other in all)
            {
                if (other == agent)
                {
                    continue;
                }

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
                separationCalculator.Evaluate(other.transform.position);
            }

            // --- COMBINE FORCES ---
            Vector3 closestEnemyForce = closestEnemyCalculator.CalculateResult(enemyCoefficient);
            Vector3 separationForce = separationCalculator.CalculateResult(separationCoefficient);
            Vector3 terrainForce = terrainCalculator.CalculateResult(separationCoefficient);
            Vector3 cohesionForce = cohesionCalculator.CalculateResult(cohesionCoefficient);
            Vector3 alignmentForce = alignmentCalculator.CalculateResult(alignmentCoefficient);
            Vector3 anchorForce = (anchorPoint - agent.transform.position) / maxConsiderationDistance * anchorCoefficient;
            Vector3 finalForce = anchorForce + cohesionForce + alignmentForce + separationForce + closestEnemyForce + terrainForce;

            // --- APPLY TO AGENT ---
            agent.desiredDirection = finalForce.normalized;
        }

        processedLastFrame += batchCount;
    }

    private IEnumerator UpdateBoidsCoroutine()
    {
        while (true)
        {
            int totalCount = FighterAI.allFighters.Count;

            processedLastFrame = 0;

            while (processedLastFrame < totalCount)
            {
                RunAgentBatch();
                totalCount = FighterAI.allFighters.Count;

                yield return null; // wait one frame before next batch
            }

            yield return null; // wait for next frame
        }
    }
}
