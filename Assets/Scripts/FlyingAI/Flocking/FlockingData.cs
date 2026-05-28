using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Burst-compatible data structure for flocking calculations.
    /// Uses Unity.Mathematics types for SIMD optimization.
    /// </summary>
    [BurstCompile]
    public struct FlockingData
    {
        #region Core Data
        
        /// <summary>
        /// Entity unique identifier.
        /// </summary>
        public int EntityId;
        
        /// <summary>
        /// Current world position.
        /// </summary>
        public float3 Position;
        
        /// <summary>
        /// Current velocity vector.
        /// </summary>
        public float3 Velocity;
        
        /// <summary>
        /// Forward direction (normalized).
        /// </summary>
        public float3 Forward;
        
        /// <summary>
        /// Current speed magnitude.
        /// </summary>
        public float Speed;
        
        #endregion

        #region Formation Data
        
        /// <summary>
        /// Formation slot index (-1 if not in formation).
        /// </summary>
        public int FormationSlotIndex;
        
        /// <summary>
        /// Target position from formation or other source.
        /// </summary>
        public float3 TargetPosition;
        
        /// <summary>
        /// Whether this entity is currently in a formation.
        /// </summary>
        public bool IsInFormation;
        
        #endregion

        #region Behavior Weights
        
        /// <summary>
        /// Separation behavior weight.
        /// </summary>
        public float SeparationWeight;
        
        /// <summary>
        /// Alignment behavior weight.
        /// </summary>
        public float AlignmentWeight;
        
        /// <summary>
        /// Cohesion behavior weight.
        /// </summary>
        public float CohesionWeight;
        
        /// <summary>
        /// Formation following weight.
        /// </summary>
        public float FormationWeight;
        
        /// <summary>
        /// Target seeking weight.
        /// </summary>
        public float TargetWeight;
        
        #endregion

        #region Parameters
        
        /// <summary>
        /// Perception radius for neighbor detection.
        /// </summary>
        public float PerceptionRadius;
        
        /// <summary>
        /// Avoidance radius for separation.
        /// </summary>
        public float AvoidanceRadius;
        
        /// <summary>
        /// Maximum movement speed.
        /// </summary>
        public float MaxSpeed;
        
        /// <summary>
        /// Maximum steering force.
        /// </summary>
        public float MaxSteerForce;
        
        #endregion

        #region Output
        
        /// <summary>
        /// Computed steering force after calculations.
        /// </summary>
        public float3 SteeringForce;
        
        /// <summary>
        /// Number of neighbors detected.
        /// </summary>
        public int NeighborCount;
        
        #endregion

        #region Static Factory Methods
        
        /// <summary>
        /// Creates a FlockingData with default values.
        /// </summary>
        public static FlockingData Create(int entityId, float3 position, float3 velocity)
        {
            return new FlockingData
            {
                EntityId = entityId,
                Position = position,
                Velocity = velocity,
                Forward = math.normalizesafe(velocity, new float3(0, 0, 1)),
                Speed = math.length(velocity),
                FormationSlotIndex = -1,
                TargetPosition = position,
                IsInFormation = false,
                SeparationWeight = 1.5f,
                AlignmentWeight = 1f,
                CohesionWeight = 1f,
                FormationWeight = 2f,
                TargetWeight = 1f,
                PerceptionRadius = 10f,
                AvoidanceRadius = 3f,
                MaxSpeed = 15f,
                MaxSteerForce = 10f,
                SteeringForce = float3.zero,
                NeighborCount = 0
            };
        }
        
        #endregion
    }

    /// <summary>
    /// Result data from flocking calculations.
    /// </summary>
    [BurstCompile]
    public struct FlockingResult
    {
        /// <summary>
        /// Entity identifier.
        /// </summary>
        public int EntityId;
        
        /// <summary>
        /// Computed steering force to apply.
        /// </summary>
        public float3 SteeringForce;
        
        /// <summary>
        /// New velocity after applying steering.
        /// </summary>
        public float3 NewVelocity;
        
        /// <summary>
        /// New position after applying velocity.
        /// </summary>
        public float3 NewPosition;
        
        /// <summary>
        /// Number of neighbors influencing this entity.
        /// </summary>
        public int NeighborCount;
    }

    /// <summary>
    /// Spatial hash cell for fast neighbor lookups.
    /// </summary>
    [BurstCompile]
    public struct SpatialHashCell
    {
        /// <summary>
        /// Cell coordinates in grid space.
        /// </summary>
        public int3 CellCoords;
        
        /// <summary>
        /// Start index in sorted entity array.
        /// </summary>
        public int StartIndex;
        
        /// <summary>
        /// Number of entities in this cell.
        /// </summary>
        public int EntityCount;
    }
}
