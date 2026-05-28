using UnityEngine;
using FlyingAI.Core;
using FlyingAI.Combat;
using FlyingAI.States;
using FlyingAI.Events;
using FlyingAI.Flocking;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;

namespace FlyingAI.Pooling
{
    /// <summary>
    /// Main flying enemy implementation.
    /// Combines state machine, flocking, combat, and PrimeTween animations.
    /// Gracefully falls back to coroutine animations if PrimeTween is not available.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FlyingEnemy : FlyingEnemyBase
    {
        #region Serialized Fields
        
        [Header("Weapon")]
        [SerializeField] private FlyingEnemyWeapon _weapon;
        
        [Header("Visual")]
        [SerializeField] private Renderer _mainRenderer;
        [SerializeField] private ParticleSystem _deathEffect;
        
        #endregion

        #region Private Fields
        
        private FlyingAIStateMachine _aiStateMachine;
        private Material _material;
        private Color _originalColor;
        private Coroutine _hoverCoroutine;
        private bool _isAnimating;
        
        #endregion

        #region Properties
        
        public FlyingEnemyWeapon Weapon => _weapon;
        
        #endregion

        #region Initialization
        
        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            
            if (_mainRenderer != null)
            {
                _material = _mainRenderer.material;
                _originalColor = _material.color;
            }
            
            if (_weapon == null)
            {
                _weapon = GetComponentInChildren<FlyingEnemyWeapon>();
            }
        }
        
        protected override void InitializeStateMachine()
        {
            _aiStateMachine = new FlyingAIStateMachine(this);
            _stateMachine = _aiStateMachine;
            
            // Subscribe to state changes for events
            _aiStateMachine.OnStateChanged += (prev, curr) =>
            {
                FlyingAIEvents.RaiseStateChanged(this, prev, curr);
            };
            
            // Start in idle state
            _aiStateMachine.ChangeState(new IdleState(this, _aiStateMachine));
        }
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Start()
        {
            base.Start();
            
            FlyingAIEvents.RaiseEnemySpawned(this);
            PlaySpawnAnimation();
        }
        
        private void OnEnable()
        {
            // Register with flocking system on enable (handles both first spawn and pool reuse)
            if (FlockingSystem.Instance != null)
            {
                FlockingSystem.Instance.RegisterEntity(this);
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Attack when in attack state
            if (_stateMachine?.CurrentState is AttackState && _weapon != null && _currentTarget != null)
            {
                _weapon.SetTarget(_currentTarget);
                _weapon.Fire();
            }
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (FlockingSystem.Instance != null)
            {
                FlockingSystem.Instance.UnregisterEntity(this);
            }
            
            StopAllAnimations();
        }
        
        #endregion

        #region Damage Handling
        
        protected override void OnDamageReceived(DamageInfo damageInfo, float actualDamage)
        {
            base.OnDamageReceived(damageInfo, actualDamage);
            
            FlyingAIEvents.RaiseEnemyDamaged(this, damageInfo);
            PlayDamageAnimation();
            
            // Consider evading on heavy damage
            if (HealthNormalized < 0.3f && _stateMachine?.CurrentState is not EvadeState)
            {
                if (Random.value < 0.5f)
                {
                    _aiStateMachine?.ChangeState(new EvadeState(this, _aiStateMachine));
                }
            }
        }
        
        #endregion

        #region Death
        
        protected override void Die()
        {
            FlyingAIEvents.RaiseEnemyDied(this);
            PlayDeathAnimation();
            
            base.Die();
        }
        
        protected override void OnDeathEffects()
        {
            if (_deathEffect != null)
            {
                _deathEffect.transform.SetParent(null);
                _deathEffect.Play();
                Destroy(_deathEffect.gameObject, _deathEffect.main.duration);
            }
        }
        
        #endregion

        #region PrimeTween Animations (with Fallback)
        
        private void PlaySpawnAnimation()
        {
            if (_visualRoot == null) return;
            
            _visualRoot.localScale = Vector3.zero;
            
            // Scale up with bounce
            TweenHelper.ScaleTo(_visualRoot, Vector3.one, 0.5f, () =>
            {
                StartHoverAnimation();
            });
            
            // Color flash
            if (_material != null)
            {
                _material.color = Color.white;
                TweenHelper.ColorTo(_mainRenderer, _originalColor, 0.5f);
            }
        }
        
        private void StartHoverAnimation()
        {
            if (_visualRoot == null || _config == null) return;
            
            if (_hoverCoroutine != null)
            {
                StopCoroutine(_hoverCoroutine);
            }
            
            _hoverCoroutine = StartCoroutine(HoverLoop());
        }
        
        private System.Collections.IEnumerator HoverLoop()
        {
            float amplitude = _config.HoverAmplitude * 0.5f;
            float frequency = _config.HoverFrequency;
            float baseY = _visualRoot.localPosition.y;
            
            while (true)
            {
                float y = baseY + Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
                _visualRoot.localPosition = new Vector3(
                    _visualRoot.localPosition.x,
                    y,
                    _visualRoot.localPosition.z
                );
                yield return null;
            }
        }
        
        private void PlayDamageAnimation()
        {
            if (_visualRoot == null || _config == null) return;
            
            // Scale punch
            TweenHelper.PunchScale(_visualRoot, Vector3.one * _config.HitScalePunch, 0.2f);
            
            // Color flash
            if (_material != null && _mainRenderer != null)
            {
                _material.color = _config.DamageFlashColor;
                TweenHelper.ColorTo(_mainRenderer, _originalColor, _config.DamageFlashDuration);
            }
        }
        
        private void PlayDeathAnimation()
        {
            if (_visualRoot == null || _config == null) return;
            
            StopAllAnimations();
            
            // Explosion scale
            TweenHelper.ScaleTo(_visualRoot, Vector3.one * _config.DeathExplosionScale, 0.3f);
            
            // Fade out
            if (_mainRenderer != null)
            {
                TweenHelper.FadeTo(_mainRenderer, 0f, 0.3f, () =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                StartCoroutine(DelayedDeactivate(0.3f));
            }
        }
        
        private System.Collections.IEnumerator DelayedDeactivate(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
        
        private void StopAllAnimations()
        {
            if (_hoverCoroutine != null)
            {
                StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = null;
            }
            
            TweenHelper.StopAll(_visualRoot);
        }
        
        #endregion

        #region Pool Support
        
        public override void ResetForPool()
        {
            base.ResetForPool();
            
            StopAllAnimations();
            
            if (_material != null)
            {
                _material.color = _originalColor;
            }
            
            if (_visualRoot != null)
            {
                _visualRoot.localScale = Vector3.one;
                _visualRoot.localPosition = Vector3.zero;
            }
            
            // Reset state machine
            _aiStateMachine?.ChangeState(new IdleState(this, _aiStateMachine));
        }
        
        #endregion

        #region Collision
        
        private void OnCollisionEnter(Collision collision)
        {
            // Charge attack collision damage
            if (_stateMachine?.CurrentState is ChargeState)
            {
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float damage = _config.CollisionDamage * _config.ChargeDamageMultiplier;
                    damageable.TakeDamage(new DamageInfo(damage, gameObject, collision.contacts[0].point));
                    
                    _aiStateMachine?.ChangeState(new EvadeState(this, _aiStateMachine));
                }
            }
        }
        
        #endregion
    }
}
