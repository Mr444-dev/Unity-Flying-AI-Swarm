using UnityEngine;
using System;

namespace FlyingAI.ScriptableObjects
{
    /// <summary>
    /// Defines a single spawn group within a wave.
    /// </summary>
    [Serializable]
    public class SpawnGroup
    {
        [Tooltip("Enemy configuration to spawn")]
        public FlyingEnemyConfig EnemyConfig;
        
        [Tooltip("Number of enemies in this group")]
        [Range(1, 50)]
        public int Count = 5;
        
        [Tooltip("Formation for this group")]
        public FormationConfig Formation;
        
        [Tooltip("Weapon configuration for this group")]
        public WeaponConfig Weapon;
        
        [Tooltip("Delay before spawning this group (after wave start)")]
        public float SpawnDelay;
        
        [Tooltip("Time between each enemy spawn in group")]
        public float SpawnInterval = 0.2f;
        
        [Tooltip("Spawn position offset from spawn point")]
        public Vector3 SpawnOffset;
        
        [Tooltip("Random position variance")]
        public float SpawnRandomRadius = 2f;
    }

    /// <summary>
    /// Configuration ScriptableObject for spawn wave definitions.
    /// Defines enemy groups, spawn timing, and wave conditions.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "FlyingAI/Configs/Wave")]
    public class WaveConfig : ScriptableObject
    {
        #region Identity
        
        [Header("Identity")]
        [Tooltip("Wave identifier")]
        [SerializeField] private string _waveId = "wave_01";
        
        [Tooltip("Display name for UI")]
        [SerializeField] private string _displayName = "Wave 1";
        
        [Tooltip("Wave description")]
        [TextArea(2, 4)]
        [SerializeField] private string _description = "First wave of enemies";
        
        #endregion

        #region Spawn Groups
        
        [Header("Spawn Groups")]
        [Tooltip("Groups of enemies to spawn in this wave")]
        [SerializeField] private SpawnGroup[] _spawnGroups;
        
        #endregion

        #region Timing
        
        [Header("Timing")]
        [Tooltip("Delay before wave starts")]
        [SerializeField] private float _waveStartDelay = 2f;
        
        [Tooltip("Total duration of wave (0 = until all enemies dead)")]
        [SerializeField] private float _waveDuration;
        
        [Tooltip("Time between this wave and next wave start")]
        [SerializeField] private float _postWaveDelay = 3f;
        
        #endregion

        #region Conditions
        
        [Header("Wave Completion Conditions")]
        [Tooltip("Wave ends when all enemies are killed")]
        [SerializeField] private bool _endOnAllKilled = true;
        
        [Tooltip("Wave ends after duration (if duration > 0)")]
        [SerializeField] private bool _endOnDuration;
        
        [Tooltip("Minimum enemies to kill for wave completion")]
        [SerializeField] private int _minimumKillsRequired;
        
        #endregion

        #region Difficulty Scaling
        
        [Header("Difficulty Scaling")]
        [Tooltip("Health multiplier for this wave")]
        [SerializeField] private float _healthMultiplier = 1f;
        
        [Tooltip("Damage multiplier for this wave")]
        [SerializeField] private float _damageMultiplier = 1f;
        
        [Tooltip("Speed multiplier for this wave")]
        [SerializeField] private float _speedMultiplier = 1f;
        
        [Tooltip("Spawn count multiplier based on difficulty")]
        [SerializeField] private AnimationCurve _difficultySpawnCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
        
        #endregion

        #region Rewards
        
        [Header("Rewards")]
        [Tooltip("Base score for completing wave")]
        [SerializeField] private int _baseScore = 100;
        
        [Tooltip("Bonus score per enemy killed")]
        [SerializeField] private int _scorePerKill = 10;
        
        [Tooltip("Time bonus multiplier")]
        [SerializeField] private float _timeBonusMultiplier = 1.5f;
        
        #endregion

        #region Properties
        
        public string WaveId => _waveId;
        public string DisplayName => _displayName;
        public string Description => _description;
        
        public SpawnGroup[] SpawnGroups => _spawnGroups;
        
        public float WaveStartDelay => _waveStartDelay;
        public float WaveDuration => _waveDuration;
        public float PostWaveDelay => _postWaveDelay;
        
        public bool EndOnAllKilled => _endOnAllKilled;
        public bool EndOnDuration => _endOnDuration;
        public int MinimumKillsRequired => _minimumKillsRequired;
        
        public float HealthMultiplier => _healthMultiplier;
        public float DamageMultiplier => _damageMultiplier;
        public float SpeedMultiplier => _speedMultiplier;
        public AnimationCurve DifficultySpawnCurve => _difficultySpawnCurve;
        
        public int BaseScore => _baseScore;
        public int ScorePerKill => _scorePerKill;
        public float TimeBonusMultiplier => _timeBonusMultiplier;
        
        #endregion

        #region Public Methods
        
        public int GetTotalEnemyCount()
        {
            if (_spawnGroups == null) return 0;
            
            int total = 0;
            foreach (var group in _spawnGroups)
            {
                total += group.Count;
            }
            return total;
        }
        
        public int GetAdjustedSpawnCount(int baseCount, float difficultyLevel)
        {
            float multiplier = _difficultySpawnCurve.Evaluate(difficultyLevel);
            return Mathf.RoundToInt(baseCount * multiplier);
        }
        
        #endregion
    }
}
