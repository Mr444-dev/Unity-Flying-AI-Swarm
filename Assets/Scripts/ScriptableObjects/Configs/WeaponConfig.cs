using UnityEngine;

namespace FlyingAI.ScriptableObjects
{
    /// <summary>
    /// Weapon firing patterns.
    /// </summary>
    public enum FiringPattern
    {
        Single,
        Burst,
        Spread,
        Rapid,
        Beam
    }
    
    /// <summary>
    /// Projectile trajectory types.
    /// </summary>
    public enum ProjectileType
    {
        Straight,
        Homing,
        Arc,
        Spiral
    }

    /// <summary>
    /// Configuration ScriptableObject for weapon properties.
    /// Defines fire rate, damage, projectile behavior, and patterns.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "FlyingAI/Configs/Weapon")]
    public class WeaponConfig : ScriptableObject
    {
        #region Identity
        
        [Header("Identity")]
        [Tooltip("Unique weapon identifier")]
        [SerializeField] private string _weaponId = "default_weapon";
        
        [Tooltip("Display name")]
        [SerializeField] private string _displayName = "Basic Blaster";
        
        #endregion

        #region Firing Settings
        
        [Header("Firing Settings")]
        [Tooltip("Weapon firing pattern")]
        [SerializeField] private FiringPattern _firingPattern = FiringPattern.Single;
        
        [Tooltip("Projectile trajectory type")]
        [SerializeField] private ProjectileType _projectileType = ProjectileType.Straight;
        
        [Tooltip("Shots per second")]
        [SerializeField] private float _fireRate = 2f;
        
        [Tooltip("Delay between shots in burst mode")]
        [SerializeField] private float _burstDelay = 0.1f;
        
        [Tooltip("Number of shots per burst")]
        [SerializeField] private int _burstCount = 3;
        
        [Tooltip("Spread angle in degrees")]
        [SerializeField] private float _spreadAngle = 30f;
        
        [Tooltip("Number of projectiles in spread")]
        [SerializeField] private int _spreadCount = 5;
        
        #endregion

        #region Projectile Settings
        
        [Header("Projectile Settings")]
        [Tooltip("Projectile prefab")]
        [SerializeField] private GameObject _projectilePrefab;
        
        [Tooltip("Projectile speed")]
        [SerializeField] private float _projectileSpeed = 20f;
        
        [Tooltip("Projectile lifetime in seconds")]
        [SerializeField] private float _projectileLifetime = 5f;
        
        [Tooltip("Projectile size scale")]
        [SerializeField] private float _projectileScale = 1f;
        
        [Tooltip("Homing turn speed (degrees/sec)")]
        [SerializeField] private float _homingTurnSpeed = 180f;
        
        [Tooltip("Homing lock-on delay")]
        [SerializeField] private float _homingLockDelay = 0.5f;
        
        [Tooltip("Arc height")]
        [SerializeField] private float _arcHeight = 5f;
        
        [Tooltip("Spiral radius")]
        [SerializeField] private float _spiralRadius = 1f;
        
        [Tooltip("Spiral frequency")]
        [SerializeField] private float _spiralFrequency = 5f;
        
        #endregion

        #region Damage Settings
        
        [Header("Damage Settings")]
        [Tooltip("Base damage per projectile")]
        [SerializeField] private float _baseDamage = 10f;
        
        [Tooltip("Critical hit chance (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _criticalChance = 0.1f;
        
        [Tooltip("Critical damage multiplier")]
        [SerializeField] private float _criticalMultiplier = 2f;
        
        [Tooltip("Area of effect radius (0 for no AoE)")]
        [SerializeField] private float _aoeRadius;
        
        [Tooltip("AoE damage falloff with distance")]
        [SerializeField] private AnimationCurve _aoeFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        #endregion

        #region Accuracy Settings
        
        [Header("Accuracy Settings")]
        [Tooltip("Base accuracy (0-1, 1 = perfect)")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseAccuracy = 0.9f;
        
        [Tooltip("Maximum random spread angle")]
        [SerializeField] private float _maxRandomSpread = 5f;
        
        [Tooltip("Accuracy decrease per consecutive shot")]
        [SerializeField] private float _accuracyDecayPerShot = 0.05f;
        
        [Tooltip("Accuracy recovery rate per second")]
        [SerializeField] private float _accuracyRecoveryRate = 0.5f;
        
        #endregion

        #region Audio & Visual
        
        [Header("Audio & Visual")]
        [Tooltip("Muzzle flash effect")]
        [SerializeField] private GameObject _muzzleFlashPrefab;
        
        [Tooltip("Impact effect")]
        [SerializeField] private GameObject _impactEffectPrefab;
        
        [Tooltip("Projectile trail color")]
        [SerializeField] private Color _trailColor = Color.yellow;
        
        [Tooltip("Trail width")]
        [SerializeField] private float _trailWidth = 0.1f;
        
        #endregion

        #region Properties
        
        public string WeaponId => _weaponId;
        public string DisplayName => _displayName;
        
        public FiringPattern Pattern => _firingPattern;
        public ProjectileType Trajectory => _projectileType;
        public float FireRate => _fireRate;
        public float BurstDelay => _burstDelay;
        public int BurstCount => _burstCount;
        public float SpreadAngle => _spreadAngle;
        public int SpreadCount => _spreadCount;
        
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float ProjectileScale => _projectileScale;
        public float HomingTurnSpeed => _homingTurnSpeed;
        public float HomingLockDelay => _homingLockDelay;
        public float ArcHeight => _arcHeight;
        public float SpiralRadius => _spiralRadius;
        public float SpiralFrequency => _spiralFrequency;
        
        public float BaseDamage => _baseDamage;
        public float CriticalChance => _criticalChance;
        public float CriticalMultiplier => _criticalMultiplier;
        public float AoeRadius => _aoeRadius;
        public AnimationCurve AoeFalloff => _aoeFalloff;
        
        public float BaseAccuracy => _baseAccuracy;
        public float MaxRandomSpread => _maxRandomSpread;
        public float AccuracyDecayPerShot => _accuracyDecayPerShot;
        public float AccuracyRecoveryRate => _accuracyRecoveryRate;
        
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public GameObject ImpactEffectPrefab => _impactEffectPrefab;
        public Color TrailColor => _trailColor;
        public float TrailWidth => _trailWidth;
        
        public float FireInterval => 1f / _fireRate;
        
        #endregion
    }
}
