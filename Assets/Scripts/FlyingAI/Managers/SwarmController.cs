using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Utils;

namespace FlyingAI.Managers
{
    /// <summary>
    /// Global swarm orchestrator for coordinated enemy behavior.
    /// Handles target selection, group commands, and swarm-level AI.
    /// </summary>
    public class SwarmController : MonoBehaviour
    {
        #region Singleton
        
        private static SwarmController _instance;
        public static SwarmController Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Target Management")]
        [SerializeField] private Transform _primaryTarget;
        [SerializeField] private float _targetUpdateInterval = 1f;
        
        [Header("Swarm Behavior")]
        [SerializeField] private float _aggressionLevel = 0.5f;
        [SerializeField] private int _maxSimultaneousAttackers = 5;
        [SerializeField] private float _coordinationRadius = 30f;
        
        #endregion

        #region Private Fields
        
        private List<FlyingEnemyBase> _swarmMembers = new List<FlyingEnemyBase>();
        private List<FlyingEnemyBase> _activeAttackers = new List<FlyingEnemyBase>();
        private float _lastTargetUpdate;
        
        #endregion

        #region Properties
        
        public Transform PrimaryTarget => _primaryTarget;
        public int SwarmSize => _swarmMembers.Count;
        public int ActiveAttackerCount => _activeAttackers.Count;
        public float AggressionLevel { get => _aggressionLevel; set => _aggressionLevel = Mathf.Clamp01(value); }
        
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
            
            FlyingAIEvents.OnEnemySpawned += OnEnemySpawned;
            FlyingAIEvents.OnEnemyDied += OnEnemyDied;
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemySpawned -= OnEnemySpawned;
            FlyingAIEvents.OnEnemyDied -= OnEnemyDied;
            if (_instance == this) _instance = null;
        }
        
        private void Update()
        {
            if (Time.time - _lastTargetUpdate >= _targetUpdateInterval)
            {
                UpdateSwarmTargets();
                _lastTargetUpdate = Time.time;
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Sets the primary target for the swarm.
        /// </summary>
        public void SetPrimaryTarget(Transform target)
        {
            _primaryTarget = target;
            UpdateSwarmTargets();
            
            DebugLogger.Log($"Swarm primary target set: {target?.name ?? "null"}", LogCategory.General);
        }
        
        /// <summary>
        /// Orders all swarm members to attack.
        /// </summary>
        public void OrderAttack()
        {
            _aggressionLevel = 1f;
            UpdateSwarmTargets();
            
            DebugLogger.Log("Swarm attack ordered!", LogCategory.Combat);
        }
        
        /// <summary>
        /// Orders all swarm members to retreat.
        /// </summary>
        public void OrderRetreat()
        {
            _aggressionLevel = 0f;
            _activeAttackers.Clear();
            
            foreach (var enemy in _swarmMembers)
            {
                enemy.SetTarget(null);
            }
            
            DebugLogger.Log("Swarm retreat ordered!", LogCategory.Combat);
        }
        
        /// <summary>
        /// Gets swarm center of mass.
        /// </summary>
        public Vector3 GetSwarmCenter()
        {
            if (_swarmMembers.Count == 0) return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var enemy in _swarmMembers)
            {
                sum += enemy.Position;
            }
            return sum / _swarmMembers.Count;
        }
        
        /// <summary>
        /// Gets enemies within coordination radius of a point.
        /// </summary>
        public List<FlyingEnemyBase> GetNearbySwarmMembers(Vector3 point, float radius = 0)
        {
            if (radius <= 0) radius = _coordinationRadius;
            
            List<FlyingEnemyBase> nearby = new List<FlyingEnemyBase>();
            foreach (var enemy in _swarmMembers)
            {
                if (Vector3.Distance(point, enemy.Position) <= radius)
                {
                    nearby.Add(enemy);
                }
            }
            return nearby;
        }
        
        #endregion

        #region Private Methods
        
        private void UpdateSwarmTargets()
        {
            if (_primaryTarget == null) return;
            
            // Clean up dead attackers
            _activeAttackers.RemoveAll(e => e == null || !e.IsAlive);
            
            // Determine how many should attack based on aggression
            int desiredAttackers = Mathf.RoundToInt(_maxSimultaneousAttackers * _aggressionLevel);
            
            // Assign targets
            foreach (var enemy in _swarmMembers)
            {
                if (!enemy.IsAlive) continue;
                
                if (_activeAttackers.Count < desiredAttackers)
                {
                    if (!_activeAttackers.Contains(enemy))
                    {
                        _activeAttackers.Add(enemy);
                    }
                    enemy.SetTarget(_primaryTarget);
                }
                else if (!_activeAttackers.Contains(enemy))
                {
                    // Non-attackers follow formation or patrol
                    enemy.SetTarget(null);
                }
            }
        }
        
        private void OnEnemySpawned(FlyingEnemyBase enemy)
        {
            if (!_swarmMembers.Contains(enemy))
            {
                _swarmMembers.Add(enemy);
                
                if (_primaryTarget != null && _aggressionLevel > 0.5f)
                {
                    enemy.SetTarget(_primaryTarget);
                }
            }
        }
        
        private void OnEnemyDied(FlyingEnemyBase enemy)
        {
            _swarmMembers.Remove(enemy);
            _activeAttackers.Remove(enemy);
        }
        
        #endregion
    }
}
