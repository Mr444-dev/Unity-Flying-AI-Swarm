using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using FlyingAI.Core;
using FlyingAI.Utils;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Represents a dynamic obstacle that can move and affect AI avoidance.
    /// </summary>
    public interface IDynamicObstacle
    {
        int ObstacleId { get; }
        Vector3 Position { get; }
        Vector3 Velocity { get; }
        float Radius { get; }
        bool IsActive { get; }
    }

    /// <summary>
    /// Component for dynamic obstacles that flying AI should avoid.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DynamicObstacle : MonoBehaviour, IDynamicObstacle
    {
        #region Serialized Fields
        
        [Header("Obstacle Settings")]
        [SerializeField] private float _radius = 2f;
        [SerializeField] private float _dangerMultiplier = 1f;
        
        [Header("Prediction")]
        [SerializeField] private bool _useVelocityPrediction = true;
        [SerializeField] private float _velocitySmoothing = 0.1f;
        
        #endregion

        #region Private Fields
        
        private static int _idCounter;
        private int _obstacleId;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private Vector3 _smoothedVelocity;
        private Rigidbody _rigidbody;
        
        #endregion

        #region Properties
        
        public int ObstacleId => _obstacleId;
        public Vector3 Position => transform.position;
        public Vector3 Velocity => _useVelocityPrediction ? _smoothedVelocity : Vector3.zero;
        public float Radius => _radius * _dangerMultiplier;
        public bool IsActive => gameObject.activeInHierarchy;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _obstacleId = ++_idCounter;
            _rigidbody = GetComponent<Rigidbody>();
            _lastPosition = transform.position;
        }
        
        private void Start()
        {
            DynamicObstacleManager.Instance?.RegisterObstacle(this);
        }
        
        private void OnDestroy()
        {
            DynamicObstacleManager.Instance?.UnregisterObstacle(this);
        }
        
        private void FixedUpdate()
        {
            UpdateVelocity();
        }
        
        #endregion

        #region Velocity Tracking
        
        private void UpdateVelocity()
        {
            if (_rigidbody != null)
            {
                _velocity = _rigidbody.linearVelocity;
            }
            else
            {
                _velocity = (transform.position - _lastPosition) / Time.fixedDeltaTime;
                _lastPosition = transform.position;
            }
            
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, _velocity, _velocitySmoothing);
        }
        
        #endregion

        #region Gizmos
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
            
            if (_useVelocityPrediction && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + _smoothedVelocity);
            }
        }
        
        #endregion
    }

    /// <summary>
    /// Manages all dynamic obstacles for efficient batch processing.
    /// </summary>
    public class DynamicObstacleManager : MonoBehaviour
    {
        #region Singleton
        
        private static DynamicObstacleManager _instance;
        public static DynamicObstacleManager Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Settings")]
        [SerializeField] private int _maxObstacles = 50;
        [SerializeField] private float _updateInterval = 0.05f;
        
        #endregion

        #region Private Fields
        
        private List<IDynamicObstacle> _obstacles = new List<IDynamicObstacle>();
        private NativeArray<float3> _obstaclePositions;
        private NativeArray<float3> _obstacleVelocities;
        private NativeArray<float> _obstacleRadii;
        private float _lastUpdateTime;
        private bool _isInitialized;
        
        #endregion

        #region Properties
        
        public int ObstacleCount => _obstacles.Count;
        public NativeArray<float3> ObstaclePositions => _obstaclePositions;
        public NativeArray<float3> ObstacleVelocities => _obstacleVelocities;
        public NativeArray<float> ObstacleRadii => _obstacleRadii;
        
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
            
            InitializeArrays();
        }
        
        private void OnDestroy()
        {
            DisposeArrays();
            if (_instance == this) _instance = null;
        }
        
        private void FixedUpdate()
        {
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateObstacleData();
                _lastUpdateTime = Time.time;
            }
        }
        
        #endregion

        #region Initialization
        
        private void InitializeArrays()
        {
            _obstaclePositions = new NativeArray<float3>(_maxObstacles, Allocator.Persistent);
            _obstacleVelocities = new NativeArray<float3>(_maxObstacles, Allocator.Persistent);
            _obstacleRadii = new NativeArray<float>(_maxObstacles, Allocator.Persistent);
            _isInitialized = true;
        }
        
        private void DisposeArrays()
        {
            if (_obstaclePositions.IsCreated) _obstaclePositions.Dispose();
            if (_obstacleVelocities.IsCreated) _obstacleVelocities.Dispose();
            if (_obstacleRadii.IsCreated) _obstacleRadii.Dispose();
            _isInitialized = false;
        }
        
        #endregion

        #region Registration
        
        public void RegisterObstacle(IDynamicObstacle obstacle)
        {
            if (_obstacles.Count >= _maxObstacles)
            {
                DebugLogger.LogWarning($"Max obstacles reached: {_maxObstacles}", LogCategory.Flocking);
                return;
            }
            
            if (!_obstacles.Contains(obstacle))
            {
                _obstacles.Add(obstacle);
                DebugLogger.LogVerbose($"Dynamic obstacle registered: {obstacle.ObstacleId}", LogCategory.Flocking);
            }
        }
        
        public void UnregisterObstacle(IDynamicObstacle obstacle)
        {
            _obstacles.Remove(obstacle);
        }
        
        #endregion

        #region Updates
        
        private void UpdateObstacleData()
        {
            if (!_isInitialized) return;
            
            // Remove inactive obstacles
            _obstacles.RemoveAll(o => o == null || !o.IsActive);
            
            int count = Mathf.Min(_obstacles.Count, _maxObstacles);
            
            for (int i = 0; i < count; i++)
            {
                var obstacle = _obstacles[i];
                _obstaclePositions[i] = obstacle.Position;
                _obstacleVelocities[i] = obstacle.Velocity;
                _obstacleRadii[i] = obstacle.Radius;
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Gets obstacles within a radius.
        /// </summary>
        public List<IDynamicObstacle> GetNearbyObstacles(Vector3 position, float radius)
        {
            List<IDynamicObstacle> nearby = new List<IDynamicObstacle>();
            float radiusSq = radius * radius;
            
            foreach (var obstacle in _obstacles)
            {
                if (obstacle == null || !obstacle.IsActive) continue;
                
                float distSq = (obstacle.Position - position).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    nearby.Add(obstacle);
                }
            }
            
            return nearby;
        }
        
        #endregion
    }
}
