using UnityEngine;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Utils;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace FlyingAI.Combat
{
    /// <summary>
    /// Health regeneration component for flying enemies.
    /// Supports various regeneration modes.
    /// </summary>
    public class HealthRegeneration : MonoBehaviour
    {
        #region Regeneration Mode
        
        public enum RegenMode
        {
            /// <summary>No passive regeneration.</summary>
            None,
            
            /// <summary>Constant regeneration rate.</summary>
            Constant,
            
            /// <summary>Regenerate only when not in combat.</summary>
            OutOfCombat,
            
            /// <summary>Regenerate faster at low health.</summary>
            EmergencyRegen,
            
            /// <summary>Regenerate in bursts over time.</summary>
            Burst
        }
        
        #endregion

        #region Serialized Fields
        
        [Header("Regeneration Settings")]
        [SerializeField] private RegenMode _mode = RegenMode.OutOfCombat;
        [SerializeField] private float _regenRate = 5f; // HP per second
        [SerializeField] private float _outOfCombatDelay = 5f;
        [SerializeField] private float _emergencyThreshold = 0.3f;
        [SerializeField] private float _emergencyMultiplier = 3f;
        
        [Header("Burst Settings")]
        [SerializeField] private float _burstAmount = 20f;
        [SerializeField] private float _burstInterval = 10f;
        
        [Header("Limits")]
        [SerializeField] private float _maxRegenPerSecond = 10f;
        [SerializeField] private bool _canRegenToFull = true;
        [SerializeField] private float _maxHealthPercent = 1f;
        
        #endregion

        #region Private Fields
        
        private FlyingEnemyBase _enemy;
        private float _lastDamageTime;
        private float _lastBurstTime;
        private CancellationTokenSource _cts;
        private bool _isRegenerating;
        
        #endregion

        #region Properties
        
        public bool IsRegenerating => _isRegenerating;
        public float TimeSinceLastDamage => Time.time - _lastDamageTime;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _enemy = GetComponent<FlyingEnemyBase>();
        }
        
        private void Start()
        {
            if (_enemy != null)
            {
                FlyingAIEvents.OnEnemyDamaged += OnEnemyDamaged;
            }
            
            if (_mode == RegenMode.Burst)
            {
                StartBurstRegenAsync().Forget();
            }
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemyDamaged -= OnEnemyDamaged;
            _cts?.Cancel();
            _cts?.Dispose();
        }
        
        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (_mode == RegenMode.None || _mode == RegenMode.Burst) return;
            
            ProcessRegeneration();
        }
        
        #endregion

        #region Regeneration Logic
        
        private void ProcessRegeneration()
        {
            if (!ShouldRegenerate()) return;
            
            float regenAmount = CalculateRegenAmount();
            if (regenAmount <= 0) return;
            
            float maxHealth = _enemy.MaxHealth * (_canRegenToFull ? 1f : _maxHealthPercent);
            if (_enemy.CurrentHealth >= maxHealth) return;
            
            _isRegenerating = true;
            _enemy.Heal(regenAmount * Time.deltaTime);
            
            DebugLogger.LogVerbose($"Regenerating {regenAmount:F1} HP/s", LogCategory.Combat);
        }
        
        private bool ShouldRegenerate()
        {
            switch (_mode)
            {
                case RegenMode.Constant:
                    return true;
                    
                case RegenMode.OutOfCombat:
                    return TimeSinceLastDamage >= _outOfCombatDelay;
                    
                case RegenMode.EmergencyRegen:
                    return _enemy.HealthNormalized <= _emergencyThreshold;
                    
                default:
                    return false;
            }
        }
        
        private float CalculateRegenAmount()
        {
            float rate = _regenRate;
            
            if (_mode == RegenMode.EmergencyRegen)
            {
                rate *= _emergencyMultiplier;
            }
            
            return Mathf.Min(rate, _maxRegenPerSecond);
        }
        
        private async UniTaskVoid StartBurstRegenAsync()
        {
            _cts = new CancellationTokenSource();
            
            while (!_cts.Token.IsCancellationRequested && _enemy != null && _enemy.IsAlive)
            {
                await UniTask.Delay((int)(_burstInterval * 1000), cancellationToken: _cts.Token);
                
                if (_enemy != null && _enemy.IsAlive && _enemy.CurrentHealth < _enemy.MaxHealth)
                {
                    _isRegenerating = true;
                    _enemy.Heal(_burstAmount);
                    _lastBurstTime = Time.time;
                    _isRegenerating = false;
                    
                    DebugLogger.LogVerbose($"Burst regen: {_burstAmount} HP", LogCategory.Combat);
                }
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void OnEnemyDamaged(FlyingEnemyBase enemy, DamageInfo info)
        {
            if (enemy == _enemy)
            {
                _lastDamageTime = Time.time;
                _isRegenerating = false;
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Sets regeneration mode at runtime.
        /// </summary>
        public void SetMode(RegenMode mode)
        {
            _mode = mode;
            
            if (mode == RegenMode.Burst && !_cts?.IsCancellationRequested == true)
            {
                StartBurstRegenAsync().Forget();
            }
        }
        
        /// <summary>
        /// Force triggers a burst regeneration.
        /// </summary>
        public void TriggerBurstRegen()
        {
            if (_enemy != null && _enemy.IsAlive)
            {
                _enemy.Heal(_burstAmount);
                _lastBurstTime = Time.time;
            }
        }
        
        #endregion
    }
}
