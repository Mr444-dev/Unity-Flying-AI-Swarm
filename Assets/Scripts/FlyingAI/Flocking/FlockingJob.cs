using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Burst-compiled parallel job for flocking behavior calculations.
    /// Implements separation, alignment, and cohesion using spatial hashing for performance.
    /// </summary>
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct FlockingJob : IJobParallelFor
    {
        #region Input Data
        
        /// <summary>
        /// All flocking entity data (read-only).
        /// </summary>
        [ReadOnly] public NativeArray<FlockingData> Entities;
        
        /// <summary>
        /// Spatial hash for fast neighbor lookup.
        /// Key: cell hash, Value: start index in SortedIndices.
        /// </summary>
        [ReadOnly] public NativeParallelHashMap<int, int2> SpatialHash;
        
        /// <summary>
        /// Entities sorted by spatial cell for cache-friendly access.
        /// </summary>
        [ReadOnly] public NativeArray<int> SortedIndices;
        
        /// <summary>
        /// Size of spatial hash cells.
        /// </summary>
        public float CellSize;
        
        /// <summary>
        /// Delta time for simulation.
        /// </summary>
        public float DeltaTime;
        
        #endregion

        #region Output Data
        
        /// <summary>
        /// Results of flocking calculations.
        /// </summary>
        [WriteOnly] public NativeArray<FlockingResult> Results;
        
        #endregion

        #region Job Execution
        
        public void Execute(int index)
        {
            FlockingData self = Entities[index];
            
            // Initialize accumulators
            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;
            int neighborCount = 0;
            int separationCount = 0;
            
            // Get neighboring cells to check
            int3 selfCell = GetCellCoords(self.Position);
            
            // Check 3x3x3 neighboring cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int3 neighborCell = selfCell + new int3(dx, dy, dz);
                        int cellHash = HashCell(neighborCell);
                        
                        if (SpatialHash.TryGetValue(cellHash, out int2 cellData))
                        {
                            int startIdx = cellData.x;
                            int count = cellData.y;
                            
                            // Process entities in this cell
                            for (int i = startIdx; i < startIdx + count; i++)
                            {
                                int otherIndex = SortedIndices[i];
                                if (otherIndex == index) continue;
                                
                                FlockingData other = Entities[otherIndex];
                                float3 offset = other.Position - self.Position;
                                float distSq = math.lengthsq(offset);
                                float perceptionRadiusSq = self.PerceptionRadius * self.PerceptionRadius;
                                
                                if (distSq < perceptionRadiusSq && distSq > 0.0001f)
                                {
                                    float dist = math.sqrt(distSq);
                                    float3 offsetNormalized = offset / dist;
                                    
                                    // Alignment: average velocity
                                    alignment += other.Velocity;
                                    
                                    // Cohesion: average position
                                    cohesion += other.Position;
                                    
                                    neighborCount++;
                                    
                                    // Separation: stronger when closer
                                    float avoidanceRadiusSq = self.AvoidanceRadius * self.AvoidanceRadius;
                                    if (distSq < avoidanceRadiusSq)
                                    {
                                        float strength = 1f - (dist / self.AvoidanceRadius);
                                        separation -= offsetNormalized * strength;
                                        separationCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Calculate steering forces
            float3 steeringForce = float3.zero;
            
            if (neighborCount > 0)
            {
                // Alignment steering
                alignment /= neighborCount;
                float3 alignmentSteer = SteerTowards(alignment, self);
                steeringForce += alignmentSteer * self.AlignmentWeight;
                
                // Cohesion steering
                cohesion /= neighborCount;
                float3 cohesionDirection = cohesion - self.Position;
                float3 cohesionSteer = SteerTowards(cohesionDirection, self);
                steeringForce += cohesionSteer * self.CohesionWeight;
            }
            
            // Separation steering
            if (separationCount > 0)
            {
                separation /= separationCount;
                float3 separationSteer = SteerTowards(separation, self);
                steeringForce += separationSteer * self.SeparationWeight;
            }
            
            // Formation following
            if (self.IsInFormation)
            {
                float3 toFormation = self.TargetPosition - self.Position;
                float3 formationSteer = SteerTowards(toFormation, self);
                steeringForce += formationSteer * self.FormationWeight;
            }
            
            // Target seeking (if not in formation or as additional influence)
            if (!self.IsInFormation && math.lengthsq(self.TargetPosition - self.Position) > 1f)
            {
                float3 toTarget = self.TargetPosition - self.Position;
                float3 targetSteer = SteerTowards(toTarget, self);
                steeringForce += targetSteer * self.TargetWeight;
            }
            
            // Apply steering
            float3 newVelocity = self.Velocity + steeringForce * DeltaTime;
            
            // Limit speed
            float speedSq = math.lengthsq(newVelocity);
            if (speedSq > self.MaxSpeed * self.MaxSpeed)
            {
                newVelocity = math.normalize(newVelocity) * self.MaxSpeed;
            }
            
            // Calculate new position
            float3 newPosition = self.Position + newVelocity * DeltaTime;
            
            // Write results
            Results[index] = new FlockingResult
            {
                EntityId = self.EntityId,
                SteeringForce = steeringForce,
                NewVelocity = newVelocity,
                NewPosition = newPosition,
                NeighborCount = neighborCount
            };
        }
        
        #endregion

        #region Helper Methods
        
        private int3 GetCellCoords(float3 position)
        {
            return new int3(
                (int)math.floor(position.x / CellSize),
                (int)math.floor(position.y / CellSize),
                (int)math.floor(position.z / CellSize)
            );
        }
        
        private int HashCell(int3 coords)
        {
            unchecked
            {
                int hash = coords.x * 73856093;
                hash ^= coords.y * 19349663;
                hash ^= coords.z * 83492791;
                return hash;
            }
        }
        
        private float3 SteerTowards(float3 direction, FlockingData self)
        {
            float magnitude = math.length(direction);
            if (magnitude < 0.0001f) return float3.zero;
            
            float3 desired = (direction / magnitude) * self.MaxSpeed;
            float3 steer = desired - self.Velocity;
            
            float steerMagnitude = math.length(steer);
            if (steerMagnitude > self.MaxSteerForce)
            {
                steer = (steer / steerMagnitude) * self.MaxSteerForce;
            }
            
            return steer;
        }
        
        #endregion
    }

    /// <summary>
    /// Job to build spatial hash for efficient neighbor lookups.
    /// </summary>
    [BurstCompile]
    public struct BuildSpatialHashJob : IJob
    {
        [ReadOnly] public NativeArray<FlockingData> Entities;
        public NativeParallelHashMap<int, int2> SpatialHash;
        public NativeArray<int> SortedIndices;
        public NativeArray<int> CellHashes;
        public float CellSize;
        
        public void Execute()
        {
            // Calculate cell hash for each entity
            for (int i = 0; i < Entities.Length; i++)
            {
                int3 cell = GetCellCoords(Entities[i].Position);
                CellHashes[i] = HashCell(cell);
                SortedIndices[i] = i;
            }
            
            // Sort by cell hash (bubble sort for Burst compatibility - small datasets)
            for (int i = 0; i < Entities.Length - 1; i++)
            {
                for (int j = 0; j < Entities.Length - i - 1; j++)
                {
                    if (CellHashes[j] > CellHashes[j + 1])
                    {
                        // Swap
                        int tempHash = CellHashes[j];
                        CellHashes[j] = CellHashes[j + 1];
                        CellHashes[j + 1] = tempHash;
                        
                        int tempIdx = SortedIndices[j];
                        SortedIndices[j] = SortedIndices[j + 1];
                        SortedIndices[j + 1] = tempIdx;
                    }
                }
            }
            
            // Build hash map (cell hash -> start index, count)
            SpatialHash.Clear();
            
            int currentHash = int.MinValue;
            int startIndex = 0;
            int count = 0;
            
            for (int i = 0; i < CellHashes.Length; i++)
            {
                if (CellHashes[i] != currentHash)
                {
                    if (count > 0)
                    {
                        SpatialHash.TryAdd(currentHash, new int2(startIndex, count));
                    }
                    currentHash = CellHashes[i];
                    startIndex = i;
                    count = 1;
                }
                else
                {
                    count++;
                }
            }
            
            // Add last cell
            if (count > 0)
            {
                SpatialHash.TryAdd(currentHash, new int2(startIndex, count));
            }
        }
        
        private int3 GetCellCoords(float3 position)
        {
            return new int3(
                (int)math.floor(position.x / CellSize),
                (int)math.floor(position.y / CellSize),
                (int)math.floor(position.z / CellSize)
            );
        }
        
        private int HashCell(int3 coords)
        {
            unchecked
            {
                int hash = coords.x * 73856093;
                hash ^= coords.y * 19349663;
                hash ^= coords.z * 83492791;
                return hash;
            }
        }
    }
}
