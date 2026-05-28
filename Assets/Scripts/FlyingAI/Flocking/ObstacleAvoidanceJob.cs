using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Burst-compiled job for obstacle avoidance using raycasting.
    /// </summary>
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct ObstacleAvoidanceJob : IJobParallelFor
    {
        #region Input Data
        
        /// <summary>
        /// Entity positions.
        /// </summary>
        [ReadOnly] public NativeArray<float3> Positions;
        
        /// <summary>
        /// Entity velocities.
        /// </summary>
        [ReadOnly] public NativeArray<float3> Velocities;
        
        /// <summary>
        /// Forward directions.
        /// </summary>
        [ReadOnly] public NativeArray<float3> Forwards;
        
        /// <summary>
        /// Raycast results from Physics.RaycastCommands.
        /// </summary>
        [ReadOnly] public NativeArray<RaycastHit> RaycastResults;
        
        /// <summary>
        /// Number of raycast directions per entity.
        /// </summary>
        public int RaysPerEntity;
        
        /// <summary>
        /// Avoidance force strength.
        /// </summary>
        public float AvoidanceStrength;
        
        /// <summary>
        /// Maximum avoidance distance.
        /// </summary>
        public float AvoidanceDistance;
        
        #endregion

        #region Output Data
        
        /// <summary>
        /// Computed avoidance steering forces.
        /// </summary>
        [WriteOnly] public NativeArray<float3> AvoidanceForces;
        
        #endregion

        #region Execution
        
        public void Execute(int index)
        {
            float3 avoidance = float3.zero;
            float3 position = Positions[index];
            float3 forward = Forwards[index];
            
            int rayStartIndex = index * RaysPerEntity;
            int validHits = 0;
            
            for (int r = 0; r < RaysPerEntity; r++)
            {
                int rayIndex = rayStartIndex + r;
                RaycastHit hit = RaycastResults[rayIndex];
                
                // Check if ray hit something (distance > 0 means hit)
                if (hit.distance > 0 && hit.distance < AvoidanceDistance)
                {
                    // Calculate avoidance direction (away from obstacle)
                    float3 hitPoint = new float3(hit.point.x, hit.point.y, hit.point.z);
                    float3 hitNormal = new float3(hit.normal.x, hit.normal.y, hit.normal.z);
                    
                    // Stronger avoidance when closer
                    float strength = 1f - (hit.distance / AvoidanceDistance);
                    strength = strength * strength; // Quadratic falloff
                    
                    // Use hit normal for avoidance direction
                    avoidance += hitNormal * strength;
                    validHits++;
                }
            }
            
            if (validHits > 0)
            {
                avoidance = math.normalizesafe(avoidance) * AvoidanceStrength;
            }
            
            AvoidanceForces[index] = avoidance;
        }
        
        #endregion
    }

    /// <summary>
    /// Job to prepare raycast commands for obstacle detection.
    /// </summary>
    [BurstCompile]
    public struct PrepareRaycastsJob : IJobParallelFor
    {
        #region Input Data
        
        [ReadOnly] public NativeArray<float3> Positions;
        [ReadOnly] public NativeArray<float3> Forwards;
        [ReadOnly] public NativeArray<float3> RayDirections; // Pre-computed ray spread directions
        
        public float RayDistance;
        public int RaysPerEntity;
        public int ObstacleLayerMask;
        
        #endregion

        #region Output Data
        
        [WriteOnly] public NativeArray<RaycastCommand> RaycastCommands;
        
        #endregion

        #region Execution
        
        public void Execute(int index)
        {
            float3 position = Positions[index];
            float3 forward = Forwards[index];
            
            // Create rotation from forward
            quaternion rotation = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));
            
            int rayStartIndex = index * RaysPerEntity;
            
            for (int r = 0; r < RaysPerEntity; r++)
            {
                // Rotate base ray direction by entity's orientation
                float3 localDir = RayDirections[r];
                float3 worldDir = math.mul(rotation, localDir);
                
                Vector3 pos = new Vector3(position.x, position.y, position.z);
                Vector3 dir = new Vector3(worldDir.x, worldDir.y, worldDir.z);
                
                RaycastCommands[rayStartIndex + r] = new RaycastCommand(
                    pos,
                    dir,
                    QueryParameters.Default,
                    RayDistance
                );
            }
        }
        
        #endregion
    }

    /// <summary>
    /// Static helper class for generating ray directions.
    /// </summary>
    public static class RayDirectionGenerator
    {
        /// <summary>
        /// Generates a cone of ray directions for obstacle detection.
        /// </summary>
        /// <param name="rayCount">Number of rays to generate.</param>
        /// <param name="coneAngle">Half-angle of the cone in degrees.</param>
        /// <returns>Array of normalized ray directions.</returns>
        public static NativeArray<float3> GenerateConeDirections(int rayCount, float coneAngle, Allocator allocator)
        {
            NativeArray<float3> directions = new NativeArray<float3>(rayCount, allocator);
            
            // Forward ray is always first
            directions[0] = new float3(0, 0, 1);
            
            if (rayCount == 1) return directions;
            
            // Golden ratio spiral for even distribution
            float goldenRatio = (1f + math.sqrt(5f)) / 2f;
            float angleIncrement = math.PI * 2f * goldenRatio;
            
            float coneAngleRad = math.radians(coneAngle);
            
            for (int i = 1; i < rayCount; i++)
            {
                float t = (float)i / (rayCount - 1);
                float inclination = coneAngleRad * t;
                float azimuth = angleIncrement * i;
                
                float sinInc = math.sin(inclination);
                float cosInc = math.cos(inclination);
                
                directions[i] = new float3(
                    sinInc * math.cos(azimuth),
                    sinInc * math.sin(azimuth),
                    cosInc
                );
            }
            
            return directions;
        }
    }
}
