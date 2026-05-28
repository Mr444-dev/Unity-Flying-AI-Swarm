using UnityEngine;

namespace FlyingAI.ScriptableObjects
{
    /// <summary>
    /// Configuration ScriptableObject for flying enemy properties.
    /// Centralizes enemy data for easy balancing and modification.
    /// </summary>
    [CreateAssetMenu(fileName = "FlyingEnemyConfig", menuName = "FlyingAI/Configs/Flying Enemy")]
    public class FlyingEnemyConfig : ScriptableObject
    {
        #region Identity
        
        [Header("Identity")]
        [Tooltip("Unique identifier for this enemy type")]
        [SerializeField] private string _enemyId = "default_flyer";
        
        [Tooltip("Display name for UI")]
        [SerializeField] private string _displayName = "Flying Enemy";
        
        [Tooltip("Enemy prefab reference")]
        [SerializeField] private GameObject _prefab;
        
        #endregion

        #region Health & Combat
        
        [Header("Health & Combat")]
        [Tooltip("Maximum health points")]
        [SerializeField] private float _maxHealth = 100f;
        
        [Tooltip("Base damage dealt on collision")]
        [SerializeField] private float _collisionDamage = 10f;
        
        [Tooltip("Damage multiplier for charge attacks")]
        [SerializeField] private float _chargeDamageMultiplier = 2f;
        
        [Tooltip("Time invulnerable after taking damage")]
        [SerializeField] private float _invulnerabilityDuration = 0.2f;
        
        #endregion

        #region Movement
        
        [Header("Movement")]
        [Tooltip("Movement speed range")]
        [SerializeField] private Vector2 _speedRange = new Vector2(5f, 15f);
        
        [Tooltip("Base movement speed")]
        [SerializeField] private float _baseSpeed = 10f;
        
        [Tooltip("Maximum acceleration")]
        [SerializeField] private float _maxAcceleration = 20f;
        
        [Tooltip("Rotation speed (degrees per second)")]
        [SerializeField] private float _rotationSpeed = 180f;
        
        [Tooltip("Vertical movement amplitude for hovering")]
        [SerializeField] private float _hoverAmplitude = 0.5f;
        
        [Tooltip("Hover frequency")]
        [SerializeField] private float _hoverFrequency = 2f;
        
        #endregion

        #region Flocking Behavior
        
        [Header("Flocking Behavior")]
        [Tooltip("Separation weight - avoid crowding")]
        [Range(0f, 5f)]
        [SerializeField] private float _separationWeight = 1.5f;
        
        [Tooltip("Alignment weight - match velocity")]
        [Range(0f, 5f)]
        [SerializeField] private float _alignmentWeight = 1f;
        
        [Tooltip("Cohesion weight - stay together")]
        [Range(0f, 5f)]
        [SerializeField] private float _cohesionWeight = 1f;
        
        [Tooltip("Formation weight - follow formation slot")]
        [Range(0f, 5f)]
        [SerializeField] private float _formationWeight = 2f;
        
        [Tooltip("Perception radius for flocking")]
        [SerializeField] private float _perceptionRadius = 10f;
        
        [Tooltip("Avoidance radius for separation")]
        [SerializeField] private float _avoidanceRadius = 3f;
        
        #endregion

        #region Combat Behavior
        
        [Header("Combat Behavior")]
        [Tooltip("Detection radius for targets")]
        [SerializeField] private float _detectionRadius = 30f;
        
        [Tooltip("Attack range for shooting")]
        [SerializeField] private float _attackRange = 15f;
        
        [Tooltip("Minimum distance to maintain from target")]
        [SerializeField] private float _minAttackDistance = 5f;
        
        [Tooltip("Charge attack speed multiplier")]
        [SerializeField] private float _chargeSpeedMultiplier = 3f;
        
        [Tooltip("Chance to perform charge attack (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeChance = 0.2f;
        
        [Tooltip("Cooldown between charge attacks")]
        [SerializeField] private float _chargeCooldown = 5f;
        
        #endregion

        #region Priority System
        
        [Header("Priority System")]
        [Tooltip("Base priority level (1-10, higher = more important)")]
        [Range(1, 10)]
        [SerializeField] private int _basePriority = 5;
        
        [Tooltip("Priority increase when low health")]
        [SerializeField] private int _lowHealthPriorityBonus = 2;
        
        [Tooltip("Health threshold for low health bonus (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowHealthThreshold = 0.3f;
        
        [Tooltip("Is this a leader/elite enemy?")]
        [SerializeField] private bool _isElite;
        
        [Tooltip("Priority bonus for elite enemies")]
        [SerializeField] private int _elitePriorityBonus = 3;
        
        #endregion

        #region Visual Feedback
        
        [Header("Visual Feedback")]
        [Tooltip("Scale punch on hit")]
        [SerializeField] private float _hitScalePunch = 0.2f;
        
        [Tooltip("Color flash on damage")]
        [SerializeField] private Color _damageFlashColor = Color.red;
        
        [Tooltip("Duration of damage flash")]
        [SerializeField] private float _damageFlashDuration = 0.1f;
        
        [Tooltip("Death explosion scale")]
        [SerializeField] private float _deathExplosionScale = 1.5f;
        
        #endregion

        #region Properties
        
        public string EnemyId => _enemyId;
        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        
        public float MaxHealth => _maxHealth;
        public float CollisionDamage => _collisionDamage;
        public float ChargeDamageMultiplier => _chargeDamageMultiplier;
        public float InvulnerabilityDuration => _invulnerabilityDuration;
        
        public Vector2 SpeedRange => _speedRange;
        public float BaseSpeed => _baseSpeed;
        public float MaxAcceleration => _maxAcceleration;
        public float RotationSpeed => _rotationSpeed;
        public float HoverAmplitude => _hoverAmplitude;
        public float HoverFrequency => _hoverFrequency;
        
        public float SeparationWeight => _separationWeight;
        public float AlignmentWeight => _alignmentWeight;
        public float CohesionWeight => _cohesionWeight;
        public float FormationWeight => _formationWeight;
        public float PerceptionRadius => _perceptionRadius;
        public float AvoidanceRadius => _avoidanceRadius;
        
        public float DetectionRadius => _detectionRadius;
        public float AttackRange => _attackRange;
        public float MinAttackDistance => _minAttackDistance;
        public float ChargeSpeedMultiplier => _chargeSpeedMultiplier;
        public float ChargeChance => _chargeChance;
        public float ChargeCooldown => _chargeCooldown;
        
        public int BasePriority => _basePriority;
        public int LowHealthPriorityBonus => _lowHealthPriorityBonus;
        public float LowHealthThreshold => _lowHealthThreshold;
        public bool IsElite => _isElite;
        public int ElitePriorityBonus => _elitePriorityBonus;
        
        public float HitScalePunch => _hitScalePunch;
        public Color DamageFlashColor => _damageFlashColor;
        public float DamageFlashDuration => _damageFlashDuration;
        public float DeathExplosionScale => _deathExplosionScale;
        
        #endregion
    }
}
