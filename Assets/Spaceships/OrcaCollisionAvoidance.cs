using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

class Orca
{
    struct Line
    {
        public Line(Vector3 ppoint, Vector3 pdirection)
        {
            this.point = ppoint;
            this.direction = pdirection;
        }

        public Vector3 point;
        public Vector3 direction;
    };

    static float FLT_EPSILON = 0.00001f;

    //static bool CollisionCircleCircle(Vector3 center1, float radius1, Vector3 center2, float radius2)
    //{
    //    Vector3 direction = center2 - center1;
    //    float radiusCombined = radius1 + radius2;
    //    return direction.sqrMagnitude < radiusCombined * radiusCombined;
    //}

    // Returns a vector to move the point outside the circle with the shortest distance
    static Vector3 CollisionPointCircle(Vector3 point, Vector3 circleCenter, float circleRadius)
    {
        Vector3 resolutionDir = point - circleCenter;
        float distanceFromCenterToPoint = resolutionDir.sqrMagnitude;
        if (distanceFromCenterToPoint == 0)
        {
            return new Vector3(0, 0, 1);
        }

        distanceFromCenterToPoint = Mathf.Sqrt(distanceFromCenterToPoint);
        float length = circleRadius - distanceFromCenterToPoint;
        if (length < 0)
        {
            return new Vector3(0, 0, 0); // Point is not in circle
        }

        return length * resolutionDir / distanceFromCenterToPoint;
    }

    // Resolves the optimal velocity under a set of linear constraints and a circular speed cap
    static int ResolveVelocityConstraints(List<Line> constraints, float speedLimit, bool favorDirection, Vector3 goalVelocity, Vector3 finalVelocity)
    {
        // Initialize velocity depending on whether we favor direction or proximity to goal
        if (favorDirection || goalVelocity.sqrMagnitude > speedLimit * speedLimit)
        {
            finalVelocity = goalVelocity.normalized * speedLimit;
        }
        else
        {
            finalVelocity = goalVelocity;
        }

        int numberOfContraints = constraints.Count;

        for (int cIndex = 0; cIndex < numberOfContraints; ++cIndex)
        {
            Line current = constraints[cIndex];

            // If the current velocity violates this constraint
            if (Vector3.Cross(current.direction, current.point - finalVelocity).sqrMagnitude > 0.0f)
            {
                // Project the constraint anchor onto its direction
                float proj = Vector3.Dot(current.point, current.direction);

                // Check for feasibility within the circle (speed constraint)
                float constraintCheck = proj * proj + speedLimit * speedLimit - current.point.sqrMagnitude;
                if (constraintCheck < 0.0f)
                {
                    return cIndex;
                }

                // Compute feasible interval [minT, maxT] for moving along the line while staying inside the circle
                float sqrtBound = Mathf.Sqrt(constraintCheck);
                float minT = -proj - sqrtBound;
                float maxT = -proj + sqrtBound;

                // Refine feasible interval using previously accepted constraints
                for (int prev = 0; prev < cIndex; ++prev)
                {
                    Line earlier = constraints[prev];
                    float denom = Vector3.Cross(current.direction, earlier.direction).sqrMagnitude;
                    float numer = Vector3.Cross(earlier.direction, current.point - earlier.point).sqrMagnitude;

                    if (denom <= FLT_EPSILON)
                    {
                        // Lines are (nearly) parallel

                        if (numer < 0.0f)
                        {
                            // Conflicting parallel constraint
                            return cIndex;
                        }
                        continue;
                    }

                    float t = numer / denom;

                    // Refine bounds based on intersection direction
                    if (denom > 0.0f)
                    {
                        maxT = Mathf.Min(maxT, t);
                    }
                    else
                    {
                        minT = Mathf.Max(minT, t);
                    }

                    if (minT > maxT)
                    {
                        return cIndex;
                    }
                }

                // Choose best velocity within refined interval
                if (favorDirection)
                {
                    finalVelocity = current.point + (Vector3.Dot(goalVelocity, current.direction) > 0.0f ? maxT : minT) * current.direction;
                }
                else
                {
                    float t = Vector3.Dot(current.direction, goalVelocity - current.point);
                    if (t < minT)
                    {
                        finalVelocity = current.point + minT * current.direction;
                    }
                    else if (t > maxT)
                    {
                        finalVelocity = current.point + maxT * current.direction;
                    }
                    else
                    {
                        finalVelocity = current.point + t * current.direction;
                    }
                }
            }
        }

        return numberOfContraints;
    }

    // Repairs velocity if it violates post-obstacle constraints by projecting back to a feasible region
    static void RefineVelocityForPostObstacles(List<Line> constraints, int startIndex, float speedCap, Vector3 correctedVelocity)
    {
        float maxViolation = 0.0f;

        for (int i = startIndex; i < constraints.Count; i++)
        {

            // Check if constraint i is violated
            if (Vector3.Cross(constraints[i].direction, constraints[i].point - correctedVelocity).sqrMagnitude > maxViolation)
            {
                // Do not adjust the constraint of static objects. An agent can not move into a wall so these orca lines have to stay.
                List<Line> adjustedConstraints = new();

                for (int j = 0; j < i; j++)
                {
                    Line mergedLine;
                    float det = Vector3.Cross(constraints[i].direction, constraints[j].direction).sqrMagnitude;

                    if (Mathf.Abs(det) <= FLT_EPSILON)
                    {
                        // Parallel constraint lines
                        if (Vector3.Dot(constraints[i].direction, constraints[j].direction) > 0.0f)
                        {
                            continue;
                        }
                        // Opposing lines: place new line halfway between
                        mergedLine.point = 0.5f * (constraints[i].point + constraints[j].point);
                    }
                    else
                    {
                        // Intersect lines and find anchor point
                        float l = Vector3.Cross(constraints[j].direction, constraints[i].point - constraints[j].point).sqrMagnitude / det;
                        mergedLine.point = constraints[i].point + l * constraints[i].direction;
                    }

                    // Direction is difference of constraints
                    mergedLine.direction = (constraints[j].direction - constraints[i].direction).normalized;
                    adjustedConstraints.Add(mergedLine);
                }

                Vector3 fallbackDirection = new Vector3(-constraints[i].direction.y, constraints[i].direction.x);

                // Recompute velocity with the new constraints
                ResolveVelocityConstraints(adjustedConstraints, speedCap, true, fallbackDirection, correctedVelocity);

                // Update max violation for future comparisons to find the velocity with the smallest violation
                maxViolation = Vector3.Cross(constraints[i].direction, constraints[i].point - correctedVelocity).sqrMagnitude;
            }
        }
    }

    static void CalculateOrcaLine(OrcaParams parameters, float invDeltaTime, Vector3 selfPosition, Vector3 selfVelocity, float selfRadius, Vector3 otherPosition, Vector3 otherVelocity, float otherRadius, bool isStatic, List<Line> lines)
    {

        Vector3 relativeVelocity = selfVelocity - otherVelocity;
        Vector3 relativePosition = otherPosition - selfPosition;
        float distanceSquared = relativePosition.sqrMagnitude;
        float combinedRadius = parameters.radiusModifier * (selfRadius + otherRadius); // Radius of velocity obstacle
        float combinedRadiusSquared = combinedRadius * combinedRadius;
        float combinedRadiusT = combinedRadius * invDeltaTime;
        float invTimeHorizon = 1.0f / parameters.timeHorizon;

        Vector3 lineDirection;
        Vector3 u;
        if (distanceSquared > combinedRadiusSquared)
        {
            // No collision now but maybe in the time horizon
            Vector3 resolutionDirection = relativeVelocity - invTimeHorizon * relativePosition;
            float resolutionDistanceSq = resolutionDirection.sqrMagnitude;
            float dotProduct = Vector3.Dot(resolutionDirection, relativePosition);

            if (dotProduct < 0.0f && dotProduct * dotProduct > combinedRadiusSquared * resolutionDistanceSq)
            {
                u = CollisionPointCircle(relativeVelocity, invTimeHorizon * relativePosition, combinedRadius * invTimeHorizon);
                lineDirection = new Vector3(u.y, -u.x, u.z);
                lineDirection.Normalize();
            }
            else
            {
                // The velocity is inside the cone part of the velocity obstacle. 
                float length = Mathf.Sqrt(distanceSquared - combinedRadiusSquared);
                // Determine which edge of the cone is the closest
                float side = Vector3.Cross(relativePosition, resolutionDirection).sqrMagnitude > 0.0f ? 1 : -1;

                Vector3 v1 = relativePosition * length;
                Vector3 v2 = relativePosition * side * combinedRadius;

                lineDirection = side * Vector3.Cross(v1, v2) / distanceSquared;
                u = Vector3.Dot(relativeVelocity, lineDirection) * lineDirection - relativeVelocity;
            }
        }
        else
        {
            u = CollisionPointCircle(relativeVelocity, invDeltaTime * relativePosition, combinedRadius * invDeltaTime);
            lineDirection = new Vector3(u.y, -u.x, u.z);
            lineDirection.Normalize();
        }

        float responsibility = isStatic ? 1 : 0.5f;
        Vector3 linePoint = selfVelocity + u * responsibility;
        lines.Add(new Line(linePoint, lineDirection));
    }

    public static Vector3 ComputeSafeVelocity(OrcaParams parameters, float deltaTime, FighterAI self, List<FighterAI> allAgents)
    {
        float invDeltaTime = 1.0f / deltaTime;
        float invTimeHorizon = 1.0f / parameters.timeHorizon;

        Vector3 selfPosition = self.rb.position;
        List<Line> lines = new List<Line>();

        foreach (FighterAI other in allAgents)
        {
            if (other == self)
                continue;

            Vector3 otherPosition = other.rb.position;
            CalculateOrcaLine(parameters, invDeltaTime, selfPosition, self.rb.velocity, self.transform.localScale.magnitude, otherPosition, other.rb.velocity, other.transform.localScale.magnitude, false, lines);
        }

        Vector3 preferedVelocity = parameters.useTargetDirection ? (self.desiredPosition - selfPosition).normalized * self.maxSpeed : self.rb.velocity;

        Vector3 newVelocity = preferedVelocity;
        int lineFail = ResolveVelocityConstraints(lines, self.speed, false, preferedVelocity, newVelocity);

        if (lineFail >= lines.Count)
            return newVelocity;

        RefineVelocityForPostObstacles(lines, lineFail, self.speed, newVelocity);

        return newVelocity;
    }

//    void GenerateStaticObstacles(FOrcaParameters parameters, const std::vector<LineSegment>& staticObstacles_, const std::vector<std::pair<Vector3, float>>& pillars)
//    {
//        staticObstacles.clear();
//    
//        int32 NumSegments = 64;
//    float Thickness = 1.0f;
//    float LifeTime = 5.0f;
//
//    // Define the circle in the XY plane (2D circle lying flat)
//    Vector3 XAxis = Vector3(1, 0, 0); // "Right" vector
//    Vector3 YAxis = Vector3(0, 1, 0); // "Up" vector
//
//    float diameter = 2 * parameters.widthOfStaticObstacleSegments;
//        for (const LineSegment&line : staticObstacles_) {
//    			Vector3 start = Vector3(line.mStart);
//    Vector3 end = Vector3(line.mEnd);
//    Vector3 vector = end - start;
//    float lineLength = vector.Length();
//    if (lineLength == 0)
//        continue;
//    
//    staticObstacles.push_back(LineObstacle{ });
//LineObstacle & obstacle = staticObstacles[staticObstacles.size() - 1];
//obstacle.direction = vector / lineLength;
//
//obstacle.radius = parameters.widthOfStaticObstacleSegments;
//Vector3 step = obstacle.direction * diameter;
//
//int numberOfPoints = static_cast<int>(lineLength / diameter) + 1;
//
//for (int i = 0; i < numberOfPoints; i++)
//{
//    Vector3 point = start + step * i;
//    obstacle.points.push_back(point);
//}
//    		}
//    
//    		for (const auto&pillar : pillars) {
//    			staticObstacles.push_back(LineObstacle{});
//LineObstacle & obstacle = staticObstacles[staticObstacles.size() - 1];
//obstacle.direction = Vector3(0, 0);
//obstacle.radius = pillar.second;
//obstacle.points.push_back(pillar.first);
//    		}
//    	}
    
}