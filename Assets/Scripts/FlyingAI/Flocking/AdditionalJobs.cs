using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Burst-compiled job for path smoothing using Catmull-Rom interpolation.
    /// </summary>
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct PathSmoothingJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> CurrentPositions;
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<float3> Velocities;
        
        public float SmoothTime;
        public float DeltaTime;
        public float MaxSpeed;
        
        public NativeArray<float3> SmoothedPositions;
        public NativeArray<float3> SmoothedVelocities;
        
        public void Execute(int index)
        {
            float3 current = CurrentPositions[index];
            float3 target = TargetPositions[index];
            float3 velocity = Velocities[index];
            
            // Smooth damp
            float3 smoothed = SmoothDamp(current, target, ref velocity, SmoothTime, MaxSpeed, DeltaTime);
            
            SmoothedPositions[index] = smoothed;
            SmoothedVelocities[index] = velocity;
        }
        
        private float3 SmoothDamp(float3 current, float3 target, ref float3 velocity, 
            float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = math.max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            
            float3 change = current - target;
            float3 originalTo = target;
            
            float maxChange = maxSpeed * smoothTime;
            float changeMag = math.length(change);
            if (changeMag > maxChange)
            {
                change = change / changeMag * maxChange;
            }
            
            target = current - change;
            
            float3 temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            float3 output = target + (change + temp) * exp;
            
            // Prevent overshooting
            float3 origMinusCurrent = originalTo - current;
            float3 outMinusOrig = output - originalTo;
            
            if (math.dot(origMinusCurrent, outMinusOrig) > 0)
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }
            
            return output;
        }
    }

    /// <summary>
    /// Burst-compiled job for leader-follower offset calculations.
    /// </summary>
    [BurstCompile]
    public struct LeaderFollowerJob : IJobParallelFor
    {
        [ReadOnly] public float3 LeaderPosition;
        [ReadOnly] public float3 LeaderForward;
        [ReadOnly] public float3 LeaderVelocity;
        [ReadOnly] public NativeArray<int> FollowerIndices;
        
        public int TotalFollowers;
        public float FollowDistance;
        public float SpreadAngle;
        
        [WriteOnly] public NativeArray<float3> TargetPositions;
        
        public void Execute(int index)
        {
            int followerIndex = FollowerIndices[index];
            
            // Calculate position in V-formation behind leader
            float t = (float)followerIndex / math.max(1, TotalFollowers - 1);
            float side = (followerIndex % 2 == 0) ? 1f : -1f;
            int row = (followerIndex + 1) / 2;
            
            float angle = side * SpreadAngle * Mathf.Deg2Rad;
            float distance = FollowDistance * (1 + row * 0.5f);
            
            float3 right = math.cross(LeaderForward, new float3(0, 1, 0));
            float3 offset = -LeaderForward * distance + right * side * row * 3f;
            
            // Add velocity prediction
            float lookAhead = distance / math.max(1f, math.length(LeaderVelocity));
            float3 predictedLeaderPos = LeaderPosition + LeaderVelocity * lookAhead * 0.5f;
            
            TargetPositions[index] = predictedLeaderPos + offset;
        }
    }

    /// <summary>
    /// Burst-compiled job for inter-entity collision prediction and avoidance.
    /// </summary>
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct EntityCollisionAvoidanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Velocities;
        [ReadOnly] public NativeArray<float> Radii;
        
        public int EntityCount;
        public float LookAheadTime;
        public float AvoidanceStrength;
        
        [WriteOnly] public NativeArray<float3> AvoidanceForces;
        
        public void Execute(int index)
        {
            float3 pos = Positions[index];
            float3 vel = Velocities[index];
            float radius = Radii[index];
            
            float3 totalAvoidance = float3.zero;
            int avoidanceCount = 0;
            
            for (int i = 0; i < EntityCount; i++)
            {
                if (i == index) continue;
                
                float3 otherPos = Positions[i];
                float3 otherVel = Velocities[i];
                float otherRadius = Radii[i];
                
                // Check for potential collision
                float3 relPos = otherPos - pos;
                float3 relVel = otherVel - vel;
                
                float combinedRadius = radius + otherRadius;
                float distSq = math.lengthsq(relPos);
                
                // Already overlapping
                if (distSq < combinedRadius * combinedRadius)
                {
                    float3 away = math.normalizesafe(-relPos);
                    totalAvoidance += away * AvoidanceStrength * 2f;
                    avoidanceCount++;
                    continue;
                }
                
                // Predict collision
                float relSpeedSq = math.lengthsq(relVel);
                if (relSpeedSq < 0.01f) continue;
                
                float t = -math.dot(relPos, relVel) / relSpeedSq;
                if (t < 0 || t > LookAheadTime) continue;
                
                float3 closestPoint = relPos + relVel * t;
                float closestDist = math.length(closestPoint);
                
                if (closestDist < combinedRadius)
                {
                    float urgency = 1f - (t / LookAheadTime);
                    float3 away = math.normalizesafe(-closestPoint);
                    totalAvoidance += away * AvoidanceStrength * urgency;
                    avoidanceCount++;
                }
            }
            
            AvoidanceForces[index] = avoidanceCount > 0 ? totalAvoidance / avoidanceCount : float3.zero;
        }
    }

    /// <summary>
    /// Burst-compiled job for combining multiple steering forces.
    /// </summary>
    [BurstCompile]
    public struct CombineSteeringForcesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> FlockingForces;
        [ReadOnly] public NativeArray<float3> AvoidanceForces;
        [ReadOnly] public NativeArray<float3> FormationForces;
        [ReadOnly] public NativeArray<float3> BoundaryForces;
        [ReadOnly] public NativeArray<float3> ThreatForces;
        
        public float FlockingWeight;
        public float AvoidanceWeight;
        public float FormationWeight;
        public float BoundaryWeight;
        public float ThreatWeight;
        public float MaxForce;
        
        public NativeArray<float3> CombinedForces;
        
        public void Execute(int index)
        {
            float3 combined = 
                FlockingForces[index] * FlockingWeight +
                AvoidanceForces[index] * AvoidanceWeight +
                FormationForces[index] * FormationWeight +
                BoundaryForces[index] * BoundaryWeight +
                ThreatForces[index] * ThreatWeight;
            
            // Clamp to max force
            float magnitude = math.length(combined);
            if (magnitude > MaxForce)
            {
                combined = math.normalize(combined) * MaxForce;
            }
            
            CombinedForces[index] = combined;
        }
    }
}
