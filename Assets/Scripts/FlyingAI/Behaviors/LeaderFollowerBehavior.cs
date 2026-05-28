using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.States;
using FlyingAI.Managers;
using FlyingAI.Utils;

namespace FlyingAI.Behaviors
{
    /// <summary>
    /// Leader behavior component for formation leaders.
    /// Provides waypoint navigation and follower management.
    /// </summary>
    public class LeaderBehavior : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Leader Settings")]
        [SerializeField] private bool _isActiveLeader = true;
        [SerializeField] private float _leaderPriorityBonus = 3f;
        
        [Header("Waypoint Navigation")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _waypointThreshold = 3f;
        [SerializeField] private bool _loopWaypoints = true;
        [SerializeField] private float _waypointPauseTime = 1f;
        
        [Header("Follower Management")]
        [SerializeField] private int _maxFollowers = 10;
        [SerializeField] private float _followerRadius = 20f;
        
        #endregion

        #region Private Fields
        
        private FlyingEnemyBase _enemy;
        private List<FlyingEnemyBase> _followers = new List<FlyingEnemyBase>();
        private int _currentWaypointIndex;
        private float _pauseTimer;
        private bool _isPaused;
        
        #endregion

        #region Properties
        
        public bool IsActiveLeader => _isActiveLeader;
        public IReadOnlyList<FlyingEnemyBase> Followers => _followers;
        public int FollowerCount => _followers.Count;
        public Vector3 CurrentWaypoint => _waypoints != null && _waypoints.Length > 0 
            ? _waypoints[_currentWaypointIndex].position 
            : transform.position;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _enemy = GetComponent<FlyingEnemyBase>();
        }
        
        private void Start()
        {
            if (_isActiveLeader && _enemy != null)
            {
                _enemy.AddPriorityModifier((int)_leaderPriorityBonus, float.MaxValue, "Leader");
            }
            
            FlyingAIEvents.OnEnemySpawned += OnEnemySpawned;
            FlyingAIEvents.OnEnemyDied += OnEnemyDied;
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemySpawned -= OnEnemySpawned;
            FlyingAIEvents.OnEnemyDied -= OnEnemyDied;
        }
        
        private void Update()
        {
            if (!_isActiveLeader || _enemy == null || !_enemy.IsAlive) return;
            
            UpdateWaypointNavigation();
            UpdateFollowers();
        }
        
        #endregion

        #region Waypoint Navigation
        
        private void UpdateWaypointNavigation()
        {
            if (_waypoints == null || _waypoints.Length == 0) return;
            
            if (_isPaused)
            {
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer <= 0)
                {
                    _isPaused = false;
                    AdvanceWaypoint();
                }
                return;
            }
            
            Vector3 target = _waypoints[_currentWaypointIndex].position;
            float distance = Vector3.Distance(_enemy.Position, target);
            
            if (distance <= _waypointThreshold)
            {
                if (_waypointPauseTime > 0)
                {
                    _isPaused = true;
                    _pauseTimer = _waypointPauseTime;
                }
                else
                {
                    AdvanceWaypoint();
                }
            }
        }
        
        private void AdvanceWaypoint()
        {
            _currentWaypointIndex++;
            
            if (_currentWaypointIndex >= _waypoints.Length)
            {
                if (_loopWaypoints)
                {
                    _currentWaypointIndex = 0;
                }
                else
                {
                    _currentWaypointIndex = _waypoints.Length - 1;
                }
            }
            
            DebugLogger.LogVerbose($"Leader advancing to waypoint {_currentWaypointIndex}", LogCategory.General);
        }
        
        #endregion

        #region Follower Management
        
        private void UpdateFollowers()
        {
            // Remove invalid followers
            _followers.RemoveAll(f => f == null || !f.IsAlive);
            
            // Check for new potential followers
            if (_followers.Count < _maxFollowers)
            {
                FindNewFollowers();
            }
        }
        
        private void FindNewFollowers()
        {
            if (EnemyPrioritySystem.Instance == null) return;
            
            foreach (var enemy in EnemyPrioritySystem.Instance.GetSortedByPriority())
            {
                if (enemy == null || enemy == _enemy) continue;
                if (_followers.Contains(enemy)) continue;
                if (_followers.Count >= _maxFollowers) break;
                
                float dist = Vector3.Distance(_enemy.Position, enemy.Position);
                if (dist <= _followerRadius)
                {
                    // Check if enemy doesn't have a leader
                    var followerComp = enemy.GetComponent<FollowerBehavior>();
                    if (followerComp == null || !followerComp.HasLeader)
                    {
                        AddFollower(enemy);
                    }
                }
            }
        }
        
        private void AddFollower(FlyingEnemyBase enemy)
        {
            _followers.Add(enemy);
            
            var followerComp = enemy.GetComponent<FollowerBehavior>();
            if (followerComp == null)
            {
                followerComp = enemy.gameObject.AddComponent<FollowerBehavior>();
            }
            followerComp.SetLeader(this);
            
            DebugLogger.LogVerbose($"Added follower: {enemy.TargetId}", LogCategory.General);
        }
        
        public void RemoveFollower(FlyingEnemyBase enemy)
        {
            _followers.Remove(enemy);
        }
        
        #endregion

        #region Event Handlers
        
        private void OnEnemySpawned(FlyingEnemyBase enemy)
        {
            if (_enemy == null) return;
            
            // Auto-recruit nearby spawns
            if (_followers.Count < _maxFollowers)
            {
                float dist = Vector3.Distance(_enemy.Position, enemy.Position);
                if (dist <= _followerRadius)
                {
                    AddFollower(enemy);
                }
            }
        }
        
        private void OnEnemyDied(FlyingEnemyBase enemy)
        {
            _followers.Remove(enemy);
            
            // If leader dies, promote a follower
            if (enemy == _enemy && _followers.Count > 0)
            {
                PromoteFollower();
            }
        }
        
        private void PromoteFollower()
        {
            if (_followers.Count == 0) return;
            
            var newLeader = _followers[0];
            var leaderComp = newLeader.GetComponent<LeaderBehavior>();
            if (leaderComp == null)
            {
                leaderComp = newLeader.gameObject.AddComponent<LeaderBehavior>();
            }
            
            leaderComp._isActiveLeader = true;
            leaderComp._waypoints = _waypoints;
            leaderComp._currentWaypointIndex = _currentWaypointIndex;
            
            // Transfer followers
            for (int i = 1; i < _followers.Count; i++)
            {
                leaderComp._followers.Add(_followers[i]);
                var followerComp = _followers[i].GetComponent<FollowerBehavior>();
                if (followerComp != null)
                {
                    followerComp.SetLeader(leaderComp);
                }
            }
            
            DebugLogger.Log($"Follower {newLeader.TargetId} promoted to leader", LogCategory.General);
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Issues a command to all followers.
        /// </summary>
        public void IssueCommand(Transform target)
        {
            foreach (var follower in _followers)
            {
                follower?.SetTarget(target);
            }
        }
        
        /// <summary>
        /// Orders all followers to regroup.
        /// </summary>
        public void OrderRegroup()
        {
            foreach (var follower in _followers)
            {
                if (follower?.StateMachine != null)
                {
                    var sm = follower.StateMachine as FlyingAIStateMachine;
                    sm?.ChangeState(new RegroupState(follower, sm));
                }
            }
        }
        
        /// <summary>
        /// Gets the index of a specific follower.
        /// </summary>
        public int GetFollowerIndex(FlyingEnemyBase enemy)
        {
            return _followers.IndexOf(enemy);
        }
        
        #endregion
    }

    /// <summary>
    /// Follower behavior component - follows a leader.
    /// </summary>
    public class FollowerBehavior : MonoBehaviour
    {
        #region Private Fields
        
        private FlyingEnemyBase _enemy;
        private LeaderBehavior _leader;
        private int _formationSlot = -1;
        
        #endregion

        #region Properties
        
        public bool HasLeader => _leader != null && _leader.IsActiveLeader;
        public LeaderBehavior Leader => _leader;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _enemy = GetComponent<FlyingEnemyBase>();
        }
        
        private void Update()
        {
            if (!HasLeader) return;
            
            FollowLeader();
        }
        
        #endregion

        #region Following Logic
        
        private void FollowLeader()
        {
            if (_leader == null || _enemy == null) return;
            
            // If in formation, use formation position
            if (_enemy.CurrentFormation != null)
            {
                return; // Formation handles positioning
            }
            
            // Otherwise, follow leader directly
            Vector3 leaderPos = _leader.transform.position;
            Vector3 followOffset = GetFollowOffset();
            Vector3 targetPos = leaderPos + followOffset;
            
            // Move towards follow position
            Vector3 toTarget = targetPos - _enemy.Position;
            if (toTarget.magnitude > 1f)
            {
                float speed = _enemy.Config?.BaseSpeed ?? 10f;
                _enemy.SetVelocity(toTarget.normalized * speed);
            }
        }
        
        private Vector3 GetFollowOffset()
        {
            // Calculate offset based on follower index
            int index = _leader.GetFollowerIndex(_enemy);
            if (index < 0) index = 0;
            
            float angle = (index / (float)Mathf.Max(1, _leader.FollowerCount)) * Mathf.PI * 2f;
            float radius = 5f + (index / 5) * 3f;
            
            return new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius - 5f // Behind leader
            );
        }
        
        #endregion

        #region Public API
        
        public void SetLeader(LeaderBehavior leader)
        {
            if (_leader != null && _leader != leader)
            {
                _leader.RemoveFollower(_enemy);
            }
            
            _leader = leader;
        }
        
        public void ClearLeader()
        {
            if (_leader != null)
            {
                _leader.RemoveFollower(_enemy);
                _leader = null;
            }
        }
        
        #endregion
    }
}
