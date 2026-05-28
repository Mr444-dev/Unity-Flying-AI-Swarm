using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Formations;
using FlyingAI.Pooling;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;
using Cysharp.Threading.Tasks;

namespace FlyingAI.Managers
{
    /// <summary>
    /// Manages wave-based enemy spawning with formation assignment.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        #region Singleton
        
        private static SpawnManager _instance;
        public static SpawnManager Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private EnemyPool _enemyPool;
        
        [Header("Wave Configuration")]
        [SerializeField] private WaveConfig[] _waves;
        [SerializeField] private bool _autoStartFirstWave = true;
        [SerializeField] private float _difficultyLevel = 0.5f;
        
        #endregion

        #region Private Fields
        
        private int _currentWaveIndex;
        private int _waveKillCount;
        private int _totalSpawnedThisWave;
        private bool _isSpawning;
        private bool _waveInProgress;
        
        #endregion

        #region Properties
        
        public int CurrentWave => _currentWaveIndex;
        public bool IsSpawning => _isSpawning;
        public bool WaveInProgress => _waveInProgress;
        public int WaveKillCount => _waveKillCount;
        
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
            
            FlyingAIEvents.OnEnemyDied += OnEnemyDied;
        }
        
        private void Start()
        {
            if (_autoStartFirstWave && _waves.Length > 0)
            {
                StartWaveAsync(_currentWaveIndex).Forget();
            }
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemyDied -= OnEnemyDied;
            if (_instance == this) _instance = null;
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Starts a specific wave.
        /// </summary>
        public async UniTask StartWaveAsync(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Length)
            {
                DebugLogger.LogWarning($"Invalid wave index: {waveIndex}", LogCategory.Spawning);
                return;
            }
            
            _currentWaveIndex = waveIndex;
            _waveKillCount = 0;
            _totalSpawnedThisWave = 0;
            _waveInProgress = true;
            
            WaveConfig wave = _waves[waveIndex];
            
            DebugLogger.Log($"Starting wave {waveIndex}: {wave.DisplayName}", LogCategory.Spawning);
            FlyingAIEvents.RaiseWaveStarted(waveIndex);
            
            await UniTask.Delay((int)(wave.WaveStartDelay * 1000));
            
            await SpawnWaveAsync(wave);
        }
        
        /// <summary>
        /// Starts the next wave.
        /// </summary>
        public void StartNextWave()
        {
            if (_currentWaveIndex + 1 < _waves.Length)
            {
                StartWaveAsync(_currentWaveIndex + 1).Forget();
            }
            else
            {
                FlyingAIEvents.RaiseAllWavesComplete();
                DebugLogger.Log("All waves complete!", LogCategory.Spawning);
            }
        }
        
        /// <summary>
        /// Spawns a group of enemies with formation.
        /// </summary>
        public async UniTask<List<FlyingEnemy>> SpawnGroupAsync(SpawnGroup group, Vector3 spawnOrigin)
        {
            List<FlyingEnemy> enemies = new List<FlyingEnemy>();
            
            // Create formation if specified
            IFormation formation = null;
            if (group.Formation != null && FormationManager.Instance != null)
            {
                formation = FormationManager.Instance.CreateFormation(group.Formation, spawnOrigin, Vector3.forward);
            }
            
            for (int i = 0; i < group.Count; i++)
            {
                Vector3 spawnPos = spawnOrigin + group.SpawnOffset;
                if (group.SpawnRandomRadius > 0)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * group.SpawnRandomRadius;
                    spawnPos += new Vector3(randomOffset.x, 0, randomOffset.y);
                }
                
                FlyingEnemy enemy = _enemyPool.SpawnWithConfig(spawnPos, Quaternion.identity, group.EnemyConfig);
                if (enemy != null)
                {
                    enemies.Add(enemy);
                    
                    // Assign weapon
                    if (group.Weapon != null && enemy.Weapon != null)
                    {
                        enemy.Weapon.SetConfig(group.Weapon);
                    }
                    
                    // Assign to formation
                    if (formation != null)
                    {
                        int slot = formation.GetNextAvailableSlot();
                        if (slot >= 0)
                        {
                            enemy.AssignToFormation(formation, slot);
                        }
                    }
                }
                
                if (group.SpawnInterval > 0 && i < group.Count - 1)
                {
                    await UniTask.Delay((int)(group.SpawnInterval * 1000));
                }
            }
            
            return enemies;
        }

        /// <summary>
        /// Spawns a single enemy.
        /// </summary>
        public FlyingEnemyBase SpawnEnemy(FlyingEnemyConfig config, Vector3 position, Quaternion rotation)
        {
            if (_enemyPool == null) return null;
            return _enemyPool.SpawnWithConfig(position, rotation, config);
        }

        /// <summary>
        /// Despawns a single enemy.
        /// </summary>
        public void DespawnEnemy(FlyingEnemyBase enemy)
        {
            if (_enemyPool == null || enemy == null) return;
            // Disable the enemy game object to return to pool
            enemy.gameObject.SetActive(false);
        }
        
        #endregion

        #region Private Methods
        
        private async UniTask SpawnWaveAsync(WaveConfig wave)
        {
            _isSpawning = true;
            
            foreach (var group in wave.SpawnGroups)
            {
                if (group.SpawnDelay > 0)
                {
                    await UniTask.Delay((int)(group.SpawnDelay * 1000));
                }
                
                Vector3 spawnOrigin = GetRandomSpawnPoint();
                int adjustedCount = wave.GetAdjustedSpawnCount(group.Count, _difficultyLevel);
                
                SpawnGroup adjustedGroup = new SpawnGroup
                {
                    EnemyConfig = group.EnemyConfig,
                    Count = adjustedCount,
                    Formation = group.Formation,
                    Weapon = group.Weapon,
                    SpawnDelay = 0,
                    SpawnInterval = group.SpawnInterval,
                    SpawnOffset = group.SpawnOffset,
                    SpawnRandomRadius = group.SpawnRandomRadius
                };
                
                var enemies = await SpawnGroupAsync(adjustedGroup, spawnOrigin);
                _totalSpawnedThisWave += enemies.Count;
            }
            
            _isSpawning = false;
            DebugLogger.Log($"Wave spawn complete. Total: {_totalSpawnedThisWave}", LogCategory.Spawning);
        }
        
        private Vector3 GetRandomSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                return transform.position;
            }
            return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
        }
        
        private void OnEnemyDied(FlyingEnemyBase enemy)
        {
            if (!_waveInProgress) return;
            
            _waveKillCount++;
            
            WaveConfig currentWave = _waves[_currentWaveIndex];
            
            // Check wave completion
            bool waveComplete = false;
            if (currentWave.EndOnAllKilled && _waveKillCount >= _totalSpawnedThisWave)
            {
                waveComplete = true;
            }
            if (currentWave.MinimumKillsRequired > 0 && _waveKillCount >= currentWave.MinimumKillsRequired)
            {
                waveComplete = true;
            }
            
            if (waveComplete)
            {
                CompleteWave();
            }
        }
        
        private void CompleteWave()
        {
            _waveInProgress = false;
            
            DebugLogger.Log($"Wave {_currentWaveIndex} complete! Kills: {_waveKillCount}", LogCategory.Spawning);
            FlyingAIEvents.RaiseWaveCompleted(_currentWaveIndex, _waveKillCount);
            
            WaveConfig wave = _waves[_currentWaveIndex];
            
            // Auto-start next wave after delay
            if (_currentWaveIndex + 1 < _waves.Length)
            {
                UniTask.Delay((int)(wave.PostWaveDelay * 1000)).ContinueWith(() => StartNextWave());
            }
            else
            {
                FlyingAIEvents.RaiseAllWavesComplete();
            }
        }
        
        #endregion
    }
}
