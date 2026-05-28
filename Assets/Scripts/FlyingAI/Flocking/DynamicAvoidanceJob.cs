using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Burst-compiled job for dynamic obstacle avoidance.
    /// Handles moving obstacles, other entities, and predictive collision avoidance.
    /// </summary>
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct DynamicAvoidanceJob : IJobParallelFor
    {
        #region Input Data
        
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Velocities;
        [ReadOnly] public NativeArray<float> Radii;
        
        /// <summary>Dynamic obstacles positions.</summary>
        [ReadOnly] public NativeArray<float3> ObstaclePositions;
        
        /// <summary>Dynamic obstacles velocities.</summary>
        [ReadOnly] public NativeArray<float3> ObstacleVelocities;
        
        /// <summary>Dynamic obstacles radii.</summary>
        [ReadOnly] public NativeArray<float> ObstacleRadii;
        
        public int ObstacleCount;
        public float LookAheadTime;
        public float AvoidanceStrength;
        public float MinSeparation;
        
        #endregion

        #region Output
        
        [WriteOnly] public NativeArray<float3> AvoidanceForces;
        
        #endregion

        #region Execution
        
        public void Execute(int index)
        {
            float3 position = Positions[index];
            float3 velocity = Velocities[index];
            float radius = Radii[index];
            
            float3 totalAvoidance = float3.zero;
            float closestCollisionTime = float.MaxValue;
            float3 closestAvoidance = float3.zero;
            
            // Check against dynamic obstacles
            for (int i = 0; i < ObstacleCount; i++)
            {
                float3 obstaclePos = ObstaclePositions[i];
                float3 obstacleVel = ObstacleVelocities[i];
                float obstacleRadius = ObstacleRadii[i];
                
                // Calculate time to closest approach
                float3 relativePos = obstaclePos - position;
                float3 relativeVel = obstacleVel - velocity;
                
                float relativeSpeedSq = math.lengthsq(relativeVel);
                if (relativeSpeedSq < 0.0001f)
                {
                    // Static relative motion - just use distance
                    float dist = math.length(relativePos);
                    float combinedRadius = radius + obstacleRadius + MinSeparation;
                    
                    if (dist < combinedRadius)
                    {
                        float3 avoidDir = math.normalizesafe(-relativePos);
                        float strength = 1f - (dist / combinedRadius);
                        totalAvoidance += avoidDir * strength * AvoidanceStrength;
                    }
                    continue;
                }
                
                // Time to closest point of approach
                float timeToClosest = -math.dot(relativePos, relativeVel) / relativeSpeedSq;
                
                if (timeToClosest < 0 || timeToClosest > LookAheadTime)
                {
                    continue; // Collision in past or too far in future
                }
                
                // Position at closest approach
                float3 myFuturePos = position + velocity * timeToClosest;
                float3 obstacleFuturePos = obstaclePos + obstacleVel * timeToClosest;
                
                float3 separation = myFuturePos - obstacleFuturePos;
                float separationDist = math.length(separation);
                float combinedRadii = radius + obstacleRadius + MinSeparation;
                
                // Will collide
                if (separationDist < combinedRadii)
                {
                    float3 avoidDir = math.normalizesafe(separation);
                    
                    // Stronger avoidance for sooner collisions
                    float urgency = 1f - (timeToClosest / LookAheadTime);
                    float overlap = 1f - (separationDist / combinedRadii);
                    float strength = urgency * overlap * AvoidanceStrength;
                    
                    if (timeToClosest < closestCollisionTime)
                    {
                        closestCollisionTime = timeToClosest;
                        closestAvoidance = avoidDir * strength;
                    }
                    
                    totalAvoidance += avoidDir * strength * 0.5f;
                }
            }
            
            // Prioritize closest collision
            if (closestCollisionTime < float.MaxValue)
            {
                totalAvoidance += closestAvoidance;
            }
            
            AvoidanceForces[index] = totalAvoidance;
        }
        
        #endregion
    }

    /// <summary>
    /// Job for velocity obstacle (VO) based collision avoidance.
    /// More sophisticated predictive avoidance using velocity obstacle cones.
    /// </summary>
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct VelocityObstacleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Velocities;
        [ReadOnly] public NativeArray<float3> PreferredVelocities;
        [ReadOnly] public NativeArray<float> Radii;
        
        public int EntityCount;
        public float TimeHorizon;
        public float MaxSpeed;
        
        public NativeArray<float3> NewVelocities;
        
        public void Execute(int index)
        {
            float3 position = Positions[index];
            float3 velocity = Velocities[index];
            float3 preferredVel = PreferredVelocities[index];
            float radius = Radii[index];
            
            // ORCA-style velocity selection
            float3 bestVelocity = preferredVel;
            float minPenalty = float.MaxValue;
            
            // Sample candidate velocities
            int samples = 16;
            for (int s = 0; s < samples; s++)
            {
                float angle = (s / (float)samples) * math.PI * 2f;
                float speed = MaxSpeed * ((s % 4) + 1) / 4f;
                
                float3 candidate = new float3(
                    math.cos(angle) * speed,
                    0,
                    math.sin(angle) * speed
                );
                
                float penalty = CalculateVelocityPenalty(index, position, candidate, radius);
                float distToPreferred = math.length(candidate - preferredVel);
                float totalPenalty = penalty + distToPreferred * 0.5f;
                
                if (totalPenalty < minPenalty)
                {
                    minPenalty = totalPenalty;
                    bestVelocity = candidate;
                }
            }
            
            NewVelocities[index] = bestVelocity;
        }
        
        private float CalculateVelocityPenalty(int selfIndex, float3 position, float3 candidateVel, float radius)
        {
            float totalPenalty = 0f;
            
            for (int i = 0; i < EntityCount; i++)
            {
                if (i == selfIndex) continue;
                
                float3 otherPos = Positions[i];
                float3 otherVel = Velocities[i];
                float otherRadius = Radii[i];
                
                float3 relativePos = otherPos - position;
                float3 relativeVel = candidateVel - otherVel;
                
                float combinedRadius = radius + otherRadius;
                float distSq = math.lengthsq(relativePos);
                
                if (distSq < combinedRadius * combinedRadius)
                {
                    totalPenalty += 1000f; // Already colliding
                    continue;
                }
                
                // Time to collision
                float a = math.lengthsq(relativeVel);
                float b = 2f * math.dot(relativePos, relativeVel);
                float c = distSq - combinedRadius * combinedRadius;
                
                float discriminant = b * b - 4f * a * c;
                
                if (discriminant > 0 && a > 0.0001f)
                {
                    float sqrtDisc = math.sqrt(discriminant);
                    float t1 = (-b - sqrtDisc) / (2f * a);
                    float t2 = (-b + sqrtDisc) / (2f * a);
                    
                    float tCollision = math.min(t1, t2);
                    
                    if (tCollision > 0 && tCollision < TimeHorizon)
                    {
                        // Penalty inversely proportional to collision time
                        totalPenalty += (TimeHorizon - tCollision) / TimeHorizon * 10f;
                    }
                }
            }
            
            return totalPenalty;
        }
    }

    /// <summary>
    /// Boundary avoidance job - keeps entities within defined bounds.
    /// </summary>
    [BurstCompile]
    public struct BoundaryAvoidanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions;
        
        public float3 BoundsMin;
        public float3 BoundsMax;
        public float BoundaryMargin;
        public float AvoidanceStrength;
        
        [WriteOnly] public NativeArray<float3> BoundaryForces;
        
        public void Execute(int index)
        {
            float3 pos = Positions[index];
            float3 force = float3.zero;
            
            // X bounds
            if (pos.x < BoundsMin.x + BoundaryMargin)
            {
                float penetration = (BoundsMin.x + BoundaryMargin - pos.x) / BoundaryMargin;
                force.x += penetration * AvoidanceStrength;
            }
            else if (pos.x > BoundsMax.x - BoundaryMargin)
            {
                float penetration = (pos.x - (BoundsMax.x - BoundaryMargin)) / BoundaryMargin;
                force.x -= penetration * AvoidanceStrength;
            }
            
            // Y bounds
            if (pos.y < BoundsMin.y + BoundaryMargin)
            {
                float penetration = (BoundsMin.y + BoundaryMargin - pos.y) / BoundaryMargin;
                force.y += penetration * AvoidanceStrength;
            }
            else if (pos.y > BoundsMax.y - BoundaryMargin)
            {
                float penetration = (pos.y - (BoundsMax.y - BoundaryMargin)) / BoundaryMargin;
                force.y -= penetration * AvoidanceStrength;
            }
            
            // Z bounds
            if (pos.z < BoundsMin.z + BoundaryMargin)
            {
                float penetration = (BoundsMin.z + BoundaryMargin - pos.z) / BoundaryMargin;
                force.z += penetration * AvoidanceStrength;
            }
            else if (pos.z > BoundsMax.z - BoundaryMargin)
            {
                float penetration = (pos.z - (BoundsMax.z - BoundaryMargin)) / BoundaryMargin;
                force.z -= penetration * AvoidanceStrength;
            }
            
            BoundaryForces[index] = force;
        }
    }
}
