using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Utils;
using FlyingAIEvents = FlyingAI.Events.FlyingAIEvents;

namespace FlyingAI.Managers
{
    /// <summary>
    /// Tracks and provides statistics for spawn waves.
    /// </summary>
    public class WaveStatsTracker : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Tracking Settings")]
        [SerializeField] private bool _trackStats = true;
        [SerializeField] private bool _logWaveSummary = true;
        
        #endregion

        #region Private Fields
        
        private int _totalEnemiesSpawned;
        private int _totalEnemiesKilled;
        private int _currentWaveEnemiesSpawned;
        private int _currentWaveEnemiesKilled;
        private float _waveStartTime;
        private Dictionary<string, int> _killsByType = new Dictionary<string, int>();
        
        #endregion

        #region Properties
        
        public int TotalKills => _totalEnemiesKilled;
        public int CurrentWaveKills => _currentWaveEnemiesKilled;
        public float CurrentWaveDuration => Time.time - _waveStartTime;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            FlyingAIEvents.OnEnemySpawned += OnEnemySpawned;
            FlyingAIEvents.OnEnemyDied += OnEnemyDied;
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemySpawned -= OnEnemySpawned;
            FlyingAIEvents.OnEnemyDied -= OnEnemyDied;
        }
        
        #endregion

        #region Event Handlers
        
        private void OnEnemySpawned(FlyingEnemyBase enemy)
        {
            if (!_trackStats) return;
            
            _totalEnemiesSpawned++;
            _currentWaveEnemiesSpawned++;
        }
        
        private void OnEnemyDied(FlyingEnemyBase enemy)
        {
            if (!_trackStats) return;
            
            _totalEnemiesKilled++;
            _currentWaveEnemiesKilled++;
            
            // Track kill counts per enemy type
            string enemyId = enemy?.gameObject.name ?? "Unknown";
            if (_killsByType.ContainsKey(enemyId))
                _killsByType[enemyId]++;
            else
                _killsByType[enemyId] = 1;
        }
        
        #endregion

        #region Public Methods
        
        public void StartNewWave(string waveId)
        {
            _currentWaveEnemiesSpawned = 0;
            _currentWaveEnemiesKilled = 0;
            _waveStartTime = Time.time;
            
            DebugLogger.Log($"Starting stats tracking for wave: {waveId}", LogCategory.General);
        }
        
        public void EndWave(string waveId)
        {
            if (_logWaveSummary)
            {
                float duration = Time.time - _waveStartTime;
                DebugLogger.Log($"Wave {waveId} Summary: Duration: {duration:F1}s, Kills: {_currentWaveEnemiesKilled}/{_currentWaveEnemiesSpawned}", LogCategory.General);
            }
        }
        
        public int GetKillsForType(string enemyId)
        {
            return _killsByType.TryGetValue(enemyId, out int kills) ? kills : 0;
        }
        
        #endregion
    }
}
