using UnityEngine;
using FlyingAI.Core;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;

namespace FlyingAI.Combat
{
    /// <summary>
    /// Base projectile class with pooling support.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileBase : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField] protected TrailRenderer _trail;
        [SerializeField] protected ParticleSystem _impactEffect;
        
        #endregion

        #region Protected Fields
        
        protected WeaponConfig _config;
        protected Rigidbody _rigidbody;
        protected float _lifetime;
        protected float _damage;
        protected GameObject _owner;
        protected Transform _homingTarget;
        protected float _homingActiveTime;
        protected Vector3 _startPosition;
        protected Vector3 _targetPosition;
        protected float _travelTime;
        
        #endregion

        #region Properties
        
        public WeaponConfig Config => _config;
        public float Damage => _damage;
        public GameObject Owner => _owner;
        
        #endregion

        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        protected virtual void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0)
            {
                Deactivate();
                return;
            }
            
            UpdateMovement();
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (_owner != null && other.gameObject == _owner) return;
            
            HandleHit(other);
        }
        
        #endregion

        #region Initialization
        
        public virtual void Initialize(WeaponConfig config, GameObject owner, Vector3 direction, Transform target = null)
        {
            _config = config;
            _owner = owner;
            _damage = config.BaseDamage;
            _lifetime = config.ProjectileLifetime;
            _homingTarget = target;
            _homingActiveTime = 0f;
            _startPosition = transform.position;
            _travelTime = 0f;
            
            transform.localScale = Vector3.one * config.ProjectileScale;
            
            if (_trail != null)
            {
                _trail.Clear();
                _trail.startColor = config.TrailColor;
                _trail.endColor = new Color(config.TrailColor.r, config.TrailColor.g, config.TrailColor.b, 0f);
                _trail.startWidth = config.TrailWidth;
            }
            
            // Initial velocity
            _rigidbody.linearVelocity = direction.normalized * config.ProjectileSpeed;
            transform.forward = direction.normalized;
        }
        
        #endregion

        #region Movement
        
        protected virtual void UpdateMovement()
        {
            if (_config == null) return;
            
            _travelTime += Time.deltaTime;
            
            switch (_config.Trajectory)
            {
                case ProjectileType.Straight:
                    // Already handled by initial velocity
                    break;
                    
                case ProjectileType.Homing:
                    UpdateHomingMovement();
                    break;
                    
                case ProjectileType.Arc:
                    UpdateArcMovement();
                    break;
                    
                case ProjectileType.Spiral:
                    UpdateSpiralMovement();
                    break;
            }
        }
        
        protected virtual void UpdateHomingMovement()
        {
            _homingActiveTime += Time.deltaTime;
            
            if (_homingActiveTime < _config.HomingLockDelay || _homingTarget == null)
            {
                return;
            }
            
            Vector3 toTarget = (_homingTarget.position - transform.position).normalized;
            Vector3 currentDir = _rigidbody.linearVelocity.normalized;
            
            float turnAmount = _config.HomingTurnSpeed * Time.deltaTime;
            Vector3 newDir = Vector3.RotateTowards(currentDir, toTarget, turnAmount * Mathf.Deg2Rad, 0f);
            
            _rigidbody.linearVelocity = newDir * _config.ProjectileSpeed;
            transform.forward = newDir;
        }
        
        protected virtual void UpdateArcMovement()
        {
            // Arc is handled via velocity modification
            float arcProgress = _travelTime / _config.ProjectileLifetime;
            float arcHeight = Mathf.Sin(arcProgress * Mathf.PI) * _config.ArcHeight;
            
            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.y = Mathf.Lerp(arcHeight, -arcHeight, arcProgress);
            _rigidbody.linearVelocity = velocity;
        }
        
        protected virtual void UpdateSpiralMovement()
        {
            float angle = _travelTime * _config.SpiralFrequency * Mathf.PI * 2f;
            float radius = _config.SpiralRadius;
            
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            
            Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            Vector3 spiralVelocity = forward * _config.ProjectileSpeed + offset * _config.SpiralFrequency;
            
            _rigidbody.linearVelocity = spiralVelocity;
        }
        
        #endregion

        #region Hit Handling
        
        protected virtual void HandleHit(Collider other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                bool isCritical = Random.value < _config.CriticalChance;
                float finalDamage = _damage * (isCritical ? _config.CriticalMultiplier : 1f);
                
                DamageInfo damageInfo = new DamageInfo(
                    finalDamage,
                    _owner,
                    transform.position,
                    _rigidbody.linearVelocity.normalized,
                    isCritical,
                    DamageType.Energy
                );
                
                damageable.TakeDamage(damageInfo);
                
                DebugLogger.LogVerbose($"Projectile hit for {finalDamage} damage{(isCritical ? " (CRIT)" : "")}", 
                    LogCategory.Combat);
            }
            
            // AoE damage
            if (_config.AoeRadius > 0)
            {
                ApplyAoEDamage();
            }
            
            SpawnImpactEffect();
            Deactivate();
        }
        
        protected virtual void ApplyAoEDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _config.AoeRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == _owner) continue;
                
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float falloff = _config.AoeFalloff.Evaluate(distance / _config.AoeRadius);
                    float aoeDamage = _damage * falloff;
                    
                    DamageInfo damageInfo = new DamageInfo(aoeDamage, _owner, transform.position);
                    damageable.TakeDamage(damageInfo);
                }
            }
        }
        
        protected virtual void SpawnImpactEffect()
        {
            if (_impactEffect != null)
            {
                _impactEffect.transform.SetParent(null);
                _impactEffect.Play();
            }
            
            if (_config?.ImpactEffectPrefab != null)
            {
                Instantiate(_config.ImpactEffectPrefab, transform.position, transform.rotation);
            }
        }
        
        #endregion

        #region Pool Support
        
        public virtual void Deactivate()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            gameObject.SetActive(false);
        }
        
        public virtual void ResetForPool()
        {
            _lifetime = 0f;
            _damage = 0f;
            _owner = null;
            _homingTarget = null;
            _config = null;
            _rigidbody.linearVelocity = Vector3.zero;
            
            if (_trail != null)
            {
                _trail.Clear();
            }
        }
        
        #endregion
    }
}
