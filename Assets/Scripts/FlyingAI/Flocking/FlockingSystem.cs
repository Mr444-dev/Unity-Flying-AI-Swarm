using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Utils;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Main orchestrator for the flocking system.
    /// Manages job scheduling, NativeArrays, and applies results to entities.
    /// Now includes dynamic obstacle avoidance and threat response.
    /// </summary>
    public class FlockingSystem : MonoBehaviour
    {
        #region Singleton
        
        private static FlockingSystem _instance;
        public static FlockingSystem Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Flocking Settings")]
        [SerializeField] private float _cellSize = 5f;
        [SerializeField] private float _maxSpeed = 15f;
        [SerializeField] private float _avoidanceWeight = 1.5f;
        [SerializeField] private float _smoothSpeed = 10f;
        
        [Header("Raycasting")]
        [SerializeField] private int _raysPerEntity = 7;
        [SerializeField] private float _rayDistance = 10f;
        [SerializeField] private float _rayConeAngle = 45f;
        [SerializeField] private LayerMask _obstacleLayer;
        
        [Header("Dynamic Avoidance")]
        [SerializeField] private bool _useDynamicAvoidance = true;
        [SerializeField] private float _dynamicLookAheadTime = 2f;
        [SerializeField] private float _dynamicAvoidanceStrength = 2f;
        [SerializeField] private float _minSeparationDistance = 2f;
        
        [Header("Boundary")]
        [SerializeField] private bool _useBoundaryAvoidance = true;
        [SerializeField] private Vector3 _boundsMin = new Vector3(-100, 0, -100);
        [SerializeField] private Vector3 _boundsMax = new Vector3(100, 50, 100);
        [SerializeField] private float _boundaryMargin = 10f;
        [SerializeField] private float _boundaryStrength = 5f;
        
        [Header("Threat Response")]
        [SerializeField] private bool _useThreatResponse = true;
        [SerializeField] private float _threatResponseWeight = 1.5f;
        
        [Header("Performance")]
        [SerializeField] private int _maxEntities = 500;
        [SerializeField] private bool _useObstacleAvoidance = true;
        
        #endregion

        #region Private Fields
        
        private List<FlyingEnemyBase> _entities = new List<FlyingEnemyBase>();
        private Dictionary<int, int> _entityIdToIndex = new Dictionary<int, int>();
        
        private NativeArray<FlockingData> _flockingData;
        private NativeArray<FlockingResult> _flockingResults;
        private NativeArray<int> _sortedIndices;
        private NativeArray<int> _cellHashes;
        private NativeParallelHashMap<int, int2> _spatialHash;
        
        private NativeArray<float3> _avoidanceForces;
        private NativeArray<RaycastCommand> _raycastCommands;
        private NativeArray<RaycastHit> _raycastResults;
        private NativeArray<float3> _rayDirections;
        
        // Dynamic obstacle avoidance
        private NativeArray<float3> _dynamicAvoidanceForces;
        private NativeArray<float> _entityRadii;
        
        // Boundary avoidance
        private NativeArray<float3> _boundaryForces;
        
        // Threat response
        private NativeArray<float3> _threatForces;
        
        private NativeArray<float3> _finalPositions;
        private NativeArray<float3> _finalVelocities;
        private NativeArray<quaternion> _finalRotations;
        
        private JobHandle _flockingJobHandle;
        private bool _isInitialized;
        private int _currentEntityCount;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            DebugLogger.Log("FlockingSystem Awake", LogCategory.Flocking, this);
            InitializeNativeArrays();
        }
        
        private void OnDestroy()
        {
            DisposeNativeArrays();
            if (_instance == this) _instance = null;
        }
        
        private void FixedUpdate()
        {
            if (_currentEntityCount == 0) return;
            
            CompleteJobs();
            UpdateEntityData();
            ScheduleJobs();
        }
        
        private void LateUpdate()
        {
            if (_currentEntityCount == 0) return;
            ApplyResults();
        }
        
        #endregion

        #region Initialization
        
        private void InitializeNativeArrays()
        {
            _flockingData = new NativeArray<FlockingData>(_maxEntities, Allocator.Persistent);
            _flockingResults = new NativeArray<FlockingResult>(_maxEntities, Allocator.Persistent);
            _sortedIndices = new NativeArray<int>(_maxEntities, Allocator.Persistent);
            _cellHashes = new NativeArray<int>(_maxEntities, Allocator.Persistent);
            _spatialHash = new NativeParallelHashMap<int, int2>(_maxEntities, Allocator.Persistent);
            
            _avoidanceForces = new NativeArray<float3>(_maxEntities, Allocator.Persistent);
            _raycastCommands = new NativeArray<RaycastCommand>(_maxEntities * _raysPerEntity, Allocator.Persistent);
            _raycastResults = new NativeArray<RaycastHit>(_maxEntities * _raysPerEntity, Allocator.Persistent);
            _rayDirections = RayDirectionGenerator.GenerateConeDirections(_raysPerEntity, _rayConeAngle, Allocator.Persistent);
            
            // Dynamic avoidance
            _dynamicAvoidanceForces = new NativeArray<float3>(_maxEntities, Allocator.Persistent);
            _entityRadii = new NativeArray<float>(_maxEntities, Allocator.Persistent);
            
            // Boundary avoidance
            _boundaryForces = new NativeArray<float3>(_maxEntities, Allocator.Persistent);
            
            // Threat response
            _threatForces = new NativeArray<float3>(_maxEntities, Allocator.Persistent);
            
            _finalPositions = new NativeArray<float3>(_maxEntities, Allocator.Persistent);
            _finalVelocities = new NativeArray<float3>(_maxEntities, Allocator.Persistent);
            _finalRotations = new NativeArray<quaternion>(_maxEntities, Allocator.Persistent);
            
            _isInitialized = true;
            DebugLogger.Log($"FlockingSystem initialized with max {_maxEntities} entities", LogCategory.Flocking, this);
        }
        
        private void DisposeNativeArrays()
        {
            _flockingJobHandle.Complete();
            
            if (_flockingData.IsCreated) _flockingData.Dispose();
            if (_flockingResults.IsCreated) _flockingResults.Dispose();
            if (_sortedIndices.IsCreated) _sortedIndices.Dispose();
            if (_cellHashes.IsCreated) _cellHashes.Dispose();
            if (_spatialHash.IsCreated) _spatialHash.Dispose();
            if (_avoidanceForces.IsCreated) _avoidanceForces.Dispose();
            if (_raycastCommands.IsCreated) _raycastCommands.Dispose();
            if (_raycastResults.IsCreated) _raycastResults.Dispose();
            if (_rayDirections.IsCreated) _rayDirections.Dispose();
            if (_dynamicAvoidanceForces.IsCreated) _dynamicAvoidanceForces.Dispose();
            if (_entityRadii.IsCreated) _entityRadii.Dispose();
            if (_boundaryForces.IsCreated) _boundaryForces.Dispose();
            if (_threatForces.IsCreated) _threatForces.Dispose();
            if (_finalPositions.IsCreated) _finalPositions.Dispose();
            if (_finalVelocities.IsCreated) _finalVelocities.Dispose();
            if (_finalRotations.IsCreated) _finalRotations.Dispose();
            
            _isInitialized = false;
        }
        
        #endregion

        #region Entity Registration
        
        public void RegisterEntity(FlyingEnemyBase entity)
        {
            if (_entities.Count >= _maxEntities)
            {
                DebugLogger.LogWarning($"Max entities reached: {_maxEntities}", LogCategory.Flocking, this);
                return;
            }
            
            if (!_entityIdToIndex.ContainsKey(entity.TargetId))
            {
                int index = _entities.Count;
                _entities.Add(entity);
                _entityIdToIndex[entity.TargetId] = index;
                _currentEntityCount = _entities.Count;
                
                DebugLogger.LogVerbose($"Entity {entity.TargetId} registered at index {index}", LogCategory.Flocking, this);
            }
        }
        
        public void UnregisterEntity(FlyingEnemyBase entity)
        {
            if (_entityIdToIndex.TryGetValue(entity.TargetId, out int index))
            {
                int lastIndex = _entities.Count - 1;
                if (index != lastIndex)
                {
                    var lastEntity = _entities[lastIndex];
                    _entities[index] = lastEntity;
                    _entityIdToIndex[lastEntity.TargetId] = index;
                }
                _entities.RemoveAt(lastIndex);
                _entityIdToIndex.Remove(entity.TargetId);
                _currentEntityCount = _entities.Count;
                
                DebugLogger.LogVerbose($"Entity {entity.TargetId} unregistered", LogCategory.Flocking, this);
            }
        }
        
        #endregion

        #region Job Scheduling
        
        private void UpdateEntityData()
        {
            for (int i = 0; i < _currentEntityCount; i++)
            {
                var entity = _entities[i];
                var config = entity.Config;
                
                _flockingData[i] = new FlockingData
                {
                    EntityId = entity.TargetId,
                    Position = entity.Position,
                    Velocity = entity.Velocity,
                    Forward = entity.Forward,
                    Speed = math.length(entity.Velocity),
                    FormationSlotIndex = entity.FormationSlotIndex,
                    TargetPosition = entity.GetFormationTargetPosition(),
                    IsInFormation = entity.CurrentFormation != null,
                    SeparationWeight = config?.SeparationWeight ?? 1.5f,
                    AlignmentWeight = config?.AlignmentWeight ?? 1f,
                    CohesionWeight = config?.CohesionWeight ?? 1f,
                    FormationWeight = config?.FormationWeight ?? 2f,
                    TargetWeight = 1f,
                    PerceptionRadius = config?.PerceptionRadius ?? 10f,
                    AvoidanceRadius = config?.AvoidanceRadius ?? 3f,
                    MaxSpeed = config?.SpeedRange.y ?? _maxSpeed,
                    MaxSteerForce = config?.MaxAcceleration ?? 10f,
                    SteeringForce = float3.zero,
                    NeighborCount = 0
                };
            }
        }
        
        private void ScheduleJobs()
        {
            var spatialHashJob = new BuildSpatialHashJob
            {
                Entities = _flockingData,
                SpatialHash = _spatialHash,
                SortedIndices = _sortedIndices,
                CellHashes = _cellHashes,
                CellSize = _cellSize
            };
            var spatialHandle = spatialHashJob.Schedule();
            
            var flockingJob = new FlockingJob
            {
                Entities = _flockingData,
                SpatialHash = _spatialHash,
                SortedIndices = _sortedIndices,
                CellSize = _cellSize,
                DeltaTime = Time.fixedDeltaTime,
                Results = _flockingResults
            };
            var flockHandle = flockingJob.Schedule(_currentEntityCount, 32, spatialHandle);
            
            if (_useObstacleAvoidance)
            {
                var positions = new NativeArray<float3>(_currentEntityCount, Allocator.TempJob);
                var forwards = new NativeArray<float3>(_currentEntityCount, Allocator.TempJob);
                for (int i = 0; i < _currentEntityCount; i++)
                {
                    positions[i] = _flockingData[i].Position;
                    forwards[i] = _flockingData[i].Forward;
                }
                
                var prepRayJob = new PrepareRaycastsJob
                {
                    Positions = positions,
                    Forwards = forwards,
                    RayDirections = _rayDirections,
                    RayDistance = _rayDistance,
                    RaysPerEntity = _raysPerEntity,
                    ObstacleLayerMask = _obstacleLayer.value,
                    RaycastCommands = _raycastCommands
                };
                var prepHandle = prepRayJob.Schedule(_currentEntityCount, 32, flockHandle);
                
                var rayHandle = RaycastCommand.ScheduleBatch(_raycastCommands, _raycastResults, 32, _currentEntityCount * _raysPerEntity, prepHandle);
                
                var avoidJob = new ObstacleAvoidanceJob
                {
                    Positions = positions,
                    Velocities = _finalVelocities,
                    Forwards = forwards,
                    RaycastResults = _raycastResults,
                    RaysPerEntity = _raysPerEntity,
                    AvoidanceStrength = _avoidanceWeight,
                    AvoidanceDistance = _rayDistance,
                    AvoidanceForces = _avoidanceForces
                };
                var avoidHandle = avoidJob.Schedule(_currentEntityCount, 32, rayHandle);
                
                var applyJob = new ApplyFlockingResultsJob
                {
                    Results = _flockingResults,
                    AvoidanceForces = _avoidanceForces,
                    AvoidanceWeight = _avoidanceWeight,
                    DeltaTime = Time.fixedDeltaTime,
                    MaxSpeed = _maxSpeed,
                    FinalPositions = _finalPositions,
                    FinalVelocities = _finalVelocities,
                    FinalRotations = _finalRotations
                };
                _flockingJobHandle = applyJob.Schedule(_currentEntityCount, 32, avoidHandle);
                
                _flockingJobHandle = positions.Dispose(_flockingJobHandle);
                _flockingJobHandle = forwards.Dispose(_flockingJobHandle);
            }
            else
            {
                for (int i = 0; i < _currentEntityCount; i++) _avoidanceForces[i] = float3.zero;
                
                var applyJob = new ApplyFlockingResultsJob
                {
                    Results = _flockingResults,
                    AvoidanceForces = _avoidanceForces,
                    AvoidanceWeight = 0,
                    DeltaTime = Time.fixedDeltaTime,
                    MaxSpeed = _maxSpeed,
                    FinalPositions = _finalPositions,
                    FinalVelocities = _finalVelocities,
                    FinalRotations = _finalRotations
                };
                _flockingJobHandle = applyJob.Schedule(_currentEntityCount, 32, flockHandle);
            }
        }
        
        private void CompleteJobs()
        {
            _flockingJobHandle.Complete();
        }
        
        private void ApplyResults()
        {
            for (int i = 0; i < _currentEntityCount; i++)
            {
                var entity = _entities[i];
                if (entity == null || !entity.IsAlive) continue;
                
                // Combine all forces
                float3 combinedForce = float3.zero;
                
                if (_useDynamicAvoidance)
                {
                    combinedForce += _dynamicAvoidanceForces[i] * _dynamicAvoidanceStrength;
                }
                
                if (_useBoundaryAvoidance)
                {
                    combinedForce += _boundaryForces[i] * _boundaryStrength;
                }
                
                if (_useThreatResponse && ThreatResponseSystem.Instance != null)
                {
                    Vector3 threatForce = ThreatResponseSystem.Instance.CalculateEvasionForce(entity);
                    combinedForce += (float3)threatForce * _threatResponseWeight;
                }
                
                // Apply combined results
                float3 finalPos = _finalPositions[i] + combinedForce * Time.fixedDeltaTime;
                float3 finalVel = _finalVelocities[i] + combinedForce;
                
                // Clamp velocity
                float speed = math.length(finalVel);
                if (speed > _maxSpeed)
                {
                    finalVel = math.normalize(finalVel) * _maxSpeed;
                }
                
                entity.transform.position = finalPos;
                entity.transform.rotation = _finalRotations[i];
                entity.SetVelocity(finalVel);
            }
        }
        
        /// <summary>
        /// Schedules dynamic obstacle avoidance jobs.
        /// </summary>
        private JobHandle ScheduleDynamicAvoidance(JobHandle dependency)
        {
            if (!_useDynamicAvoidance || DynamicObstacleManager.Instance == null)
            {
                return dependency;
            }
            
            var manager = DynamicObstacleManager.Instance;
            int obstacleCount = manager.ObstacleCount;
            
            if (obstacleCount == 0) return dependency;
            
            var positions = new NativeArray<float3>(_currentEntityCount, Allocator.TempJob);
            var velocities = new NativeArray<float3>(_currentEntityCount, Allocator.TempJob);
            
            for (int i = 0; i < _currentEntityCount; i++)
            {
                positions[i] = _flockingData[i].Position;
                velocities[i] = _flockingData[i].Velocity;
                _entityRadii[i] = _entities[i].Config?.AvoidanceRadius ?? 2f;
            }
            
            var dynamicJob = new DynamicAvoidanceJob
            {
                Positions = positions,
                Velocities = velocities,
                Radii = _entityRadii,
                ObstaclePositions = manager.ObstaclePositions,
                ObstacleVelocities = manager.ObstacleVelocities,
                ObstacleRadii = manager.ObstacleRadii,
                ObstacleCount = obstacleCount,
                LookAheadTime = _dynamicLookAheadTime,
                AvoidanceStrength = _dynamicAvoidanceStrength,
                MinSeparation = _minSeparationDistance,
                AvoidanceForces = _dynamicAvoidanceForces
            };
            
            var handle = dynamicJob.Schedule(_currentEntityCount, 32, dependency);
            handle = positions.Dispose(handle);
            handle = velocities.Dispose(handle);
            
            return handle;
        }
        
        /// <summary>
        /// Schedules boundary avoidance job.
        /// </summary>
        private JobHandle ScheduleBoundaryAvoidance(JobHandle dependency)
        {
            if (!_useBoundaryAvoidance) return dependency;
            
            var positions = new NativeArray<float3>(_currentEntityCount, Allocator.TempJob);
            for (int i = 0; i < _currentEntityCount; i++)
            {
                positions[i] = _flockingData[i].Position;
            }
            
            var boundaryJob = new BoundaryAvoidanceJob
            {
                Positions = positions,
                BoundsMin = _boundsMin,
                BoundsMax = _boundsMax,
                BoundaryMargin = _boundaryMargin,
                AvoidanceStrength = _boundaryStrength,
                BoundaryForces = _boundaryForces
            };
            
            var handle = boundaryJob.Schedule(_currentEntityCount, 32, dependency);
            return positions.Dispose(handle);
        }
        
        #endregion

        #region Public API
        
        public int EntityCount => _currentEntityCount;
        public bool IsInitialized => _isInitialized;
        
        /// <summary>Bounds for boundary avoidance.</summary>
        public Bounds FlockingBounds => new Bounds(
            (_boundsMin + _boundsMax) * 0.5f,
            _boundsMax - _boundsMin
        );
        
        public FlockingData GetEntityData(int entityId)
        {
            if (_entityIdToIndex.TryGetValue(entityId, out int index))
            {
                return _flockingData[index];
            }
            return default;
        }
        
        /// <summary>
        /// Sets the flocking bounds at runtime.
        /// </summary>
        public void SetBounds(Vector3 min, Vector3 max)
        {
            _boundsMin = min;
            _boundsMax = max;
        }
        
        /// <summary>
        /// Toggle dynamic obstacle avoidance.
        /// </summary>
        public void SetDynamicAvoidanceEnabled(bool enabled)
        {
            _useDynamicAvoidance = enabled;
        }
        
        /// <summary>
        /// Toggle threat response.
        /// </summary>
        public void SetThreatResponseEnabled(bool enabled)
        {
            _useThreatResponse = enabled;
        }
        
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmosSelected()
        {
            if (_useBoundaryAvoidance)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawWireCube(
                    (_boundsMin + _boundsMax) * 0.5f,
                    _boundsMax - _boundsMin
                );
                
                Gizmos.color = new Color(1, 1, 0, 0.1f);
                Vector3 innerMin = _boundsMin + Vector3.one * _boundaryMargin;
                Vector3 innerMax = _boundsMax - Vector3.one * _boundaryMargin;
                Gizmos.DrawWireCube(
                    (innerMin + innerMax) * 0.5f,
                    innerMax - innerMin
                );
            }
        }
        
        #endregion
    }
}
