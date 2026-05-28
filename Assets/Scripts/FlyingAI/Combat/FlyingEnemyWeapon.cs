using UnityEngine;
using FlyingAI.Core;
using FlyingAI.Pooling;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace FlyingAI.Combat
{
    /// <summary>
    /// Component managing enemy weapon logic and firing patterns.
    /// </summary>
    public class FlyingEnemyWeapon : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private WeaponConfig _config;
        
        [Header("References")]
        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private ProjectilePool _projectilePool;
        
        #endregion

        #region Private Fields
        
        private float _lastFireTime;
        private float _currentAccuracy;
        private Transform _currentTarget;
        private CancellationTokenSource _cts;
        private bool _isFiring;
        
        #endregion

        #region Properties
        
        public WeaponConfig Config => _config;
        public bool CanFire => Time.time >= _lastFireTime + _config.FireInterval;
        public bool IsFiring => _isFiring;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_muzzlePoint == null)
            {
                _muzzlePoint = transform;
            }
            
            _currentAccuracy = _config?.BaseAccuracy ?? 0.9f;
        }
        
        private void Update()
        {
            RecoverAccuracy();
        }
        
        private void OnDisable()
        {
            StopFiring();
        }
        
        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        
        #endregion

        #region Public API
        
        public void SetConfig(WeaponConfig config)
        {
            _config = config;
            _currentAccuracy = config?.BaseAccuracy ?? 0.9f;
        }
        
        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }
        
        public void SetProjectilePool(ProjectilePool pool)
        {
            _projectilePool = pool;
        }
        
        /// <summary>
        /// Fires the weapon once based on firing pattern.
        /// </summary>
        public void Fire()
        {
            if (_config == null || !CanFire) return;
            
            _lastFireTime = Time.time;
            
            switch (_config.Pattern)
            {
                case FiringPattern.Single:
                    FireSingle();
                    break;
                    
                case FiringPattern.Burst:
                    FireBurstAsync().Forget();
                    break;
                    
                case FiringPattern.Spread:
                    FireSpread();
                    break;
                    
                case FiringPattern.Rapid:
                    FireSingle();
                    break;
            }
            
            DecayAccuracy();
        }
        
        public void StopFiring()
        {
            _isFiring = false;
            _cts?.Cancel();
        }
        
        #endregion

        #region Firing Methods
        
        private void FireSingle()
        {
            SpawnProjectile(GetAimDirection());
            SpawnMuzzleFlash();
        }
        
        private async UniTaskVoid FireBurstAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _isFiring = true;
            
            try
            {
                for (int i = 0; i < _config.BurstCount; i++)
                {
                    if (!_isFiring) break;
                    
                    SpawnProjectile(GetAimDirection());
                    SpawnMuzzleFlash();
                    
                    if (i < _config.BurstCount - 1)
                    {
                        await UniTask.Delay((int)(_config.BurstDelay * 1000), cancellationToken: _cts.Token);
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                // Cancelled
            }
            
            _isFiring = false;
        }
        
        private void FireSpread()
        {
            Vector3 baseDir = GetAimDirection();
            float totalAngle = _config.SpreadAngle;
            float angleStep = totalAngle / (_config.SpreadCount - 1);
            float startAngle = -totalAngle / 2f;
            
            for (int i = 0; i < _config.SpreadCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector3 spreadDir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
                SpawnProjectile(spreadDir);
            }
            
            SpawnMuzzleFlash();
        }
        
        #endregion

        #region Helper Methods
        
        private Vector3 GetAimDirection()
        {
            Vector3 baseDirection = _muzzlePoint.forward;
            
            if (_currentTarget != null)
            {
                baseDirection = (_currentTarget.position - _muzzlePoint.position).normalized;
            }
            
            // Apply accuracy spread
            float spread = (1f - _currentAccuracy) * _config.MaxRandomSpread;
            Vector3 randomSpread = new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                Random.Range(-spread, spread)
            );
            
            Vector3 finalDirection = (baseDirection + randomSpread * 0.1f).normalized;
            return finalDirection;
        }
        
        private void SpawnProjectile(Vector3 direction)
        {
            if (_projectilePool == null)
            {
                DebugLogger.LogWarning("No projectile pool assigned!", LogCategory.Combat, this);
                return;
            }
            
            ProjectileBase projectile = _projectilePool.Spawn(_muzzlePoint.position, Quaternion.LookRotation(direction));
            if (projectile != null)
            {
                projectile.Initialize(_config, gameObject, direction, _currentTarget);
            }
        }
        
        private void SpawnMuzzleFlash()
        {
            if (_config?.MuzzleFlashPrefab != null)
            {
                var flash = Instantiate(_config.MuzzleFlashPrefab, _muzzlePoint.position, _muzzlePoint.rotation);
                Destroy(flash, 1f);
            }
        }
        
        private void DecayAccuracy()
        {
            _currentAccuracy -= _config.AccuracyDecayPerShot;
            _currentAccuracy = Mathf.Max(_currentAccuracy, 0.3f);
        }
        
        private void RecoverAccuracy()
        {
            if (_currentAccuracy < _config.BaseAccuracy)
            {
                _currentAccuracy += _config.AccuracyRecoveryRate * Time.deltaTime;
                _currentAccuracy = Mathf.Min(_currentAccuracy, _config.BaseAccuracy);
            }
        }
        
        #endregion
    }
}
