using UnityEngine;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Managers;
using FlyingAI.Utils;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace FlyingAI.Combat
{
    /// <summary>
    /// Types of special abilities.
    /// </summary>
    public enum AbilityType
    {
        /// <summary>Short burst of speed.</summary>
        SpeedBoost,
        
        /// <summary>Temporary invincibility.</summary>
        Shield,
        
        /// <summary>AoE attack around self.</summary>
        NovaBlast,
        
        /// <summary>Cloak and become invisible.</summary>
        Cloak,
        
        /// <summary>Call for reinforcements.</summary>
        CallReinforcements,
        
        /// <summary>Heal nearby allies.</summary>
        HealingAura,
        
        /// <summary>Kamikaze explosion on death.</summary>
        SelfDestruct,
        
        /// <summary>Teleport to random position.</summary>
        Blink
    }

    /// <summary>
    /// Special ability component for elite enemies.
    /// Uses TweenHelper for plugin-safe animations.
    /// </summary>
    public class SpecialAbility : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Ability Settings")]
        [SerializeField] private AbilityType _abilityType = AbilityType.SpeedBoost;
        
        [SerializeField] private float _cooldown = 10f;
        
        [SerializeField] private float _duration = 3f;
        
        [Header("Effect Values")]
        [SerializeField] private float _speedBoostMultiplier = 2f;
        
        [SerializeField] private float _novaRadius = 10f;
        
        [SerializeField] private float _novaDamage = 50f;
        
        [SerializeField] private float _healAmount = 30f;
        
        [SerializeField] private float _healRadius = 15f;
        
        [SerializeField] private float _selfDestructRadius = 8f;
        
        [SerializeField] private float _selfDestructDamage = 100f;
        
        [SerializeField] private float _blinkDistance = 20f;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _abilityEffect;
        
        [SerializeField] private Color _abilityColor = Color.yellow;
        
        [Header("Auto-Activation")]
        [SerializeField] private bool _autoActivate = true;
        
        [SerializeField] private float _activationHealthThreshold = 0.5f;
        
        [SerializeField] private float _randomActivationChance = 0.1f;
        
        #endregion

        #region Private Fields
        
        private FlyingEnemyBase _enemy;
        private float _lastActivationTime = -999f;
        private bool _isActive;
        private CancellationTokenSource _cts;
        private Renderer _renderer;
        private Color _originalColor;
        
        #endregion

        #region Properties
        
        public AbilityType Type => _abilityType;
        public bool CanActivate => Time.time >= _lastActivationTime + _cooldown;
        public bool IsActive => _isActive;
        public float CooldownRemaining => Mathf.Max(0, (_lastActivationTime + _cooldown) - Time.time);
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _enemy = GetComponent<FlyingEnemyBase>();
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }
        }
        
        private void Start()
        {
            FlyingAIEvents.OnEnemyDied += OnEnemyDied;
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemyDied -= OnEnemyDied;
            _cts?.Cancel();
            _cts?.Dispose();
        }
        
        private void Update()
        {
            if (!_autoActivate || _isActive || !CanActivate) return;
            if (_enemy == null || !_enemy.IsAlive) return;
            
            CheckAutoActivation();
        }
        
        #endregion

        #region Auto-Activation
        
        private void CheckAutoActivation()
        {
            // Health-based activation
            if (_enemy.HealthNormalized <= _activationHealthThreshold)
            {
                Activate();
                return;
            }
            
            // Random chance
            if (Random.value < _randomActivationChance * Time.deltaTime)
            {
                Activate();
            }
        }
        
        #endregion

        #region Activation
        
        public void Activate()
        {
            if (!CanActivate || _isActive) return;
            
            _lastActivationTime = Time.time;
            _isActive = true;
            
            DebugLogger.Log($"Ability activated: {_abilityType}", LogCategory.Combat);
            
            switch (_abilityType)
            {
                case AbilityType.SpeedBoost:
                    ActivateSpeedBoost().Forget();
                    break;
                case AbilityType.Shield:
                    ActivateShield().Forget();
                    break;
                case AbilityType.NovaBlast:
                    ActivateNovaBlast();
                    break;
                case AbilityType.Cloak:
                    ActivateCloak().Forget();
                    break;
                case AbilityType.CallReinforcements:
                    ActivateReinforcements();
                    break;
                case AbilityType.HealingAura:
                    ActivateHealingAura();
                    break;
                case AbilityType.SelfDestruct:
                    // Handled on death
                    break;
                case AbilityType.Blink:
                    ActivateBlink();
                    break;
            }
            
            PlayAbilityEffect();
        }
        
        #endregion

        #region Ability Implementations (Using TweenHelper)
        
        private async UniTaskVoid ActivateSpeedBoost()
        {
            _cts = new CancellationTokenSource();
            
            // Visual feedback
            if (_renderer != null)
            {
                TweenHelper.ColorTo(_renderer, _abilityColor, 0.2f);
            }
            
            // Store original speed
            float originalSpeed = _enemy.Config?.BaseSpeed ?? 10f;
            
            // Apply boost - handled through velocity scaling in update
            float endTime = Time.time + _duration;
            while (Time.time < endTime && !_cts.Token.IsCancellationRequested)
            {
                if (_enemy != null && _enemy.IsAlive)
                {
                    Vector3 vel = _enemy.Velocity;
                    if (vel.magnitude > 0.1f)
                    {
                        _enemy.SetVelocity(vel.normalized * originalSpeed * _speedBoostMultiplier);
                    }
                }
                await UniTask.Yield(cancellationToken: _cts.Token);
            }
            
            // Reset visual
            if (_renderer != null)
            {
                TweenHelper.ColorTo(_renderer, _originalColor, 0.3f);
            }
            
            _isActive = false;
        }
        
        private async UniTaskVoid ActivateShield()
        {
            _cts = new CancellationTokenSource();
            
            _enemy.SetInvulnerable(true);
            
            // Visual effect - scale up
            TweenHelper.ScaleTo(transform, transform.localScale * 1.2f, 0.2f);
            if (_renderer != null)
            {
                TweenHelper.ColorTo(_renderer, Color.cyan, 0.2f);
            }
            
            await UniTask.Delay((int)(_duration * 1000), cancellationToken: _cts.Token);
            
            _enemy.SetInvulnerable(false);
            
            // Reset
            TweenHelper.ScaleTo(transform, Vector3.one, 0.3f);
            if (_renderer != null)
            {
                TweenHelper.ColorTo(_renderer, _originalColor, 0.3f);
            }
            
            _isActive = false;
        }
        
        private void ActivateNovaBlast()
        {
            Collider[] hits = Physics.OverlapSphere(_enemy.Position, _novaRadius);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float dist = Vector3.Distance(_enemy.Position, hit.transform.position);
                    float falloff = 1f - (dist / _novaRadius);
                    
                    damageable.TakeDamage(new DamageInfo(
                        _novaDamage * falloff,
                        gameObject,
                        _enemy.Position
                    ));
                }
            }
            
            // Visual - punch scale
            TweenHelper.PunchScale(transform, Vector3.one * 0.5f, 0.3f);
            
            _isActive = false;
        }
        
        private async UniTaskVoid ActivateCloak()
        {
            _cts = new CancellationTokenSource();
            
            // Fade out
            if (_renderer != null)
            {
                TweenHelper.FadeTo(_renderer, 0.1f, 0.5f);
            }
            
            // Reduce priority to make enemy less targetable
            _enemy.AddPriorityModifier(new PriorityModifier
            {
                ModifierId = "Cloak",
                Value = -5,
                Duration = _duration
            });
            
            await UniTask.Delay((int)(_duration * 1000), cancellationToken: _cts.Token);
            
            // Fade back in
            if (_renderer != null)
            {
                TweenHelper.FadeTo(_renderer, 1f, 0.3f);
            }
            
            _isActive = false;
        }
        
        private void ActivateReinforcements()
        {
            // Request spawn from SpawnManager
            if (Managers.SpawnManager.Instance != null)
            {
                DebugLogger.Log("Reinforcements called!", LogCategory.Combat);
            }
            
            _isActive = false;
        }
        
        private void ActivateHealingAura()
        {
            if (EnemyPrioritySystem.Instance == null) return;
            
            foreach (var enemy in EnemyPrioritySystem.Instance.GetSortedByPriority())
            {
                if (enemy == null) continue;
                
                float dist = Vector3.Distance(_enemy.Position, enemy.Position);
                if (dist <= _healRadius)
                {
                    enemy.Heal(_healAmount);
                }
            }
            
            _isActive = false;
        }
        
        private void ActivateBlink()
        {
            // Teleport to random position
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-1f, 1f)
            ).normalized;
            
            Vector3 newPos = _enemy.Position + randomDir * _blinkDistance;
            
            // Visual effect - scale down, teleport, scale up
            TweenHelper.ScaleTo(transform, Vector3.zero, 0.15f, () =>
            {
                transform.position = newPos;
                TweenHelper.ScaleTo(transform, Vector3.one, 0.2f);
            });
            
            _isActive = false;
        }
        
        #endregion

        #region Death Handler
        
        private void OnEnemyDied(FlyingEnemyBase enemy)
        {
            if (enemy != _enemy) return;
            
            if (_abilityType == AbilityType.SelfDestruct)
            {
                // Explode on death
                Collider[] hits = Physics.OverlapSphere(_enemy.Position, _selfDestructRadius);
                
                foreach (var hit in hits)
                {
                    if (hit.gameObject == gameObject) continue;
                    
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        float dist = Vector3.Distance(_enemy.Position, hit.transform.position);
                        float falloff = 1f - (dist / _selfDestructRadius);
                        
                        damageable.TakeDamage(new DamageInfo(
                            _selfDestructDamage * falloff,
                            gameObject,
                            _enemy.Position
                        ));
                    }
                }
                
                DebugLogger.Log("Enemy self-destructed!", LogCategory.Combat);
            }
        }
        
        #endregion

        #region Visual
        
        private void PlayAbilityEffect()
        {
            if (_abilityEffect != null)
            {
                _abilityEffect.Play();
            }
        }
        
        #endregion
    }
}
