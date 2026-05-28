using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Utils;

namespace FlyingAI.Managers
{
    /// <summary>
    /// Tracks enemy importance and provides sorted priority lists.
    /// Enables identification of key targets.
    /// </summary>
    public class EnemyPrioritySystem : MonoBehaviour
    {
        #region Singleton
        
        private static EnemyPrioritySystem _instance;
        public static EnemyPrioritySystem Instance => _instance;
        
        #endregion

        #region Private Fields
        
        private Dictionary<int, FlyingEnemyBase> _trackedEnemies = new Dictionary<int, FlyingEnemyBase>();
        private List<FlyingEnemyBase> _sortedByPriority = new List<FlyingEnemyBase>();
        private bool _isDirty = true;
        
        #endregion

        #region Properties
        
        public int TrackedCount => _trackedEnemies.Count;
        
        /// <summary>Gets the highest priority enemy.</summary>
        public FlyingEnemyBase HighestPriority => GetHighestPriorityEnemies(1).FirstOrDefault();
        
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
            
            FlyingAIEvents.OnEnemySpawned += TrackEnemy;
            FlyingAIEvents.OnEnemyDied += UntrackEnemy;
            FlyingAIEvents.OnPriorityChanged += OnPriorityUpdated;
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemySpawned -= TrackEnemy;
            FlyingAIEvents.OnEnemyDied -= UntrackEnemy;
            FlyingAIEvents.OnPriorityChanged -= OnPriorityUpdated;
            
            if (_instance == this) _instance = null;
        }
        
        #endregion

        #region Tracking
        
        public void TrackEnemy(FlyingEnemyBase enemy)
        {
            if (enemy == null) return;
            
            if (!_trackedEnemies.ContainsKey(enemy.TargetId))
            {
                _trackedEnemies[enemy.TargetId] = enemy;
                _isDirty = true;
                
                DebugLogger.LogVerbose($"Tracking enemy {enemy.TargetId} with priority {enemy.CurrentPriority}", 
                    LogCategory.General);
            }
        }
        
        public void UntrackEnemy(FlyingEnemyBase enemy)
        {
            if (enemy == null) return;
            
            if (_trackedEnemies.Remove(enemy.TargetId))
            {
                _isDirty = true;
                DebugLogger.LogVerbose($"Untracked enemy {enemy.TargetId}", LogCategory.General);
            }
        }
        
        private void OnPriorityUpdated(FlyingEnemyBase enemy, int oldPri, int newPri)
        {
            _isDirty = true;
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Gets enemies sorted by priority (highest first).
        /// </summary>
        public List<FlyingEnemyBase> GetSortedByPriority()
        {
            if (_isDirty)
            {
                RefreshSortedList();
            }
            return _sortedByPriority;
        }
        
        /// <summary>
        /// Gets the top N highest priority enemies.
        /// </summary>
        public IEnumerable<FlyingEnemyBase> GetHighestPriorityEnemies(int count)
        {
            return GetSortedByPriority().Take(count);
        }
        
        /// <summary>
        /// Gets enemies within a specific priority level range.
        /// </summary>
        public IEnumerable<FlyingEnemyBase> GetByPriorityLevel(PriorityLevel level)
        {
            return _trackedEnemies.Values.Where(e => e.Level == level);
        }
        
        /// <summary>
        /// Gets all elite enemies.
        /// </summary>
        public IEnumerable<FlyingEnemyBase> GetElites()
        {
            return _trackedEnemies.Values.Where(e => e.IsElite);
        }
        
        /// <summary>
        /// Gets enemy by ID.
        /// </summary>
        public FlyingEnemyBase GetById(int targetId)
        {
            _trackedEnemies.TryGetValue(targetId, out var enemy);
            return enemy;
        }
        
        /// <summary>
        /// Gets nearest enemy to a position.
        /// </summary>
        public FlyingEnemyBase GetNearest(Vector3 position)
        {
            FlyingEnemyBase nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var enemy in _trackedEnemies.Values)
            {
                if (!enemy.IsValidTarget) continue;
                
                float dist = Vector3.Distance(position, enemy.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Gets nearest high-priority enemy.
        /// </summary>
        public FlyingEnemyBase GetNearestHighPriority(Vector3 position, int minPriority = 7)
        {
            FlyingEnemyBase nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var enemy in _trackedEnemies.Values)
            {
                if (!enemy.IsValidTarget || enemy.CurrentPriority < minPriority) continue;
                
                float dist = Vector3.Distance(position, enemy.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
            
            return nearest;
        }
        
        #endregion

        #region Private Methods
        
        private void RefreshSortedList()
        {
            _sortedByPriority.Clear();
            _sortedByPriority.AddRange(_trackedEnemies.Values.Where(e => e.IsValidTarget));
            _sortedByPriority.Sort((a, b) => b.CurrentPriority.CompareTo(a.CurrentPriority));
            _isDirty = false;
        }
        
        #endregion
    }
}
