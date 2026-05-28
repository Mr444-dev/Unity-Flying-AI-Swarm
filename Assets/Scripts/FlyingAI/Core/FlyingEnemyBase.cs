using UnityEngine;
using System;
using System.Collections.Generic;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;

namespace FlyingAI.Core
{
    /// <summary>
    /// Priority modifier data structure.
    /// </summary>
    [Serializable]
    public class PriorityModifier
    {
        public string ModifierId;
        public int Value;
        public float Duration;
        public float RemainingTime;
        
        public PriorityModifier()
        {
            ModifierId = "";
            Value = 0;
            Duration = 0;
            RemainingTime = 0;
        }
        
        public PriorityModifier(string id, int value, float duration)
        {
            ModifierId = id;
            Value = value;
            Duration = duration;
            RemainingTime = duration;
        }
    }

    /// <summary>
    /// Abstract base class for all flying enemies.
    /// Provides virtual methods for extensibility and implements core interfaces.
    /// </summary>
    public abstract class FlyingEnemyBase : MonoBehaviour, IDamageable, IPriorityTarget
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [Tooltip("Enemy configuration data")]
        [SerializeField] protected FlyingEnemyConfig _config;
        
        [Header("Components")]
        [Tooltip("Transform used for visual effects")]
        [SerializeField] protected Transform _visualRoot;
        
        [Tooltip("Collider for hit detection")]
        [SerializeField] protected Collider _hitCollider;
        
        #endregion

        #region Private Fields
        
        private static int _idCounter;
        
        private int _targetId;
        private float _currentHealth;
        private bool _isInvulnerable;
        private float _invulnerabilityTimer;
        
        private List<PriorityModifier> _priorityModifiers = new List<PriorityModifier>();
        private int _cachedPriority;
        
        protected IStateMachine _stateMachine;
        protected IFormation _currentFormation;
        protected int _formationSlotIndex = -1;
        
        protected Vector3 _velocity;
        protected Vector3 _targetPosition;
        protected Transform _currentTarget;
        
        #endregion

        #region Properties - IDamageable
        
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _config != null ? _config.MaxHealth : 100f;
        public bool IsAlive => _currentHealth > 0f;
        public bool IsInvulnerable => _isInvulnerable;
        public float HealthNormalized => MaxHealth > 0 ? _currentHealth / MaxHealth : 0f;
        
        #endregion

        #region Properties - IPriorityTarget
        
        public int TargetId => _targetId;
        public int BasePriority => _config != null ? _config.BasePriority : 5;
        public bool IsElite => _config != null && _config.IsElite;
        public Vector3 Position => transform.position;
        public bool IsValidTarget => IsAlive && gameObject.activeInHierarchy;
        
        public int CurrentPriority
        {
            get
            {
                CalculatePriority();
                return _cachedPriority;
            }
        }
        
        public PriorityLevel Level
        {
            get
            {
                int priority = CurrentPriority;
                if (priority >= 9) return PriorityLevel.Critical;
                if (priority >= 7) return PriorityLevel.High;
                if (priority >= 4) return PriorityLevel.Normal;
                return PriorityLevel.Low;
            }
        }
        
        #endregion

        #region Properties - Movement
        
        public Vector3 Velocity => _velocity;
        public Vector3 Forward => transform.forward;
        public FlyingEnemyConfig Config => _config;
        public IFormation CurrentFormation => _currentFormation;
        public int FormationSlotIndex => _formationSlotIndex;
        public Transform CurrentTarget => _currentTarget;
        public IStateMachine StateMachine => _stateMachine;
        
        #endregion

        #region Events
        
        public event Action<FlyingEnemyBase> OnDeath;
        public event Action<FlyingEnemyBase, DamageInfo> OnDamaged;
        public event Action<FlyingEnemyBase, int, int> OnPriorityChanged;
        
        #endregion

        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            _targetId = ++_idCounter;
            
            DebugLogger.Log($"FlyingEnemyBase Awake - ID: {_targetId}", LogCategory.General, this);
            
            InitializeComponents();
        }
        
        protected virtual void Start()
        {
            DebugLogger.Log($"FlyingEnemyBase Start - ID: {_targetId}", LogCategory.General, this);
            
            InitializeHealth();
            InitializeStateMachine();
        }
        
        protected virtual void Update()
        {
            UpdateInvulnerability();
            UpdatePriorityModifiers();
            UpdateStateMachine();
        }
        
        protected virtual void FixedUpdate()
        {
            FixedUpdateStateMachine();
            UpdateMovement();
        }
        
        protected virtual void OnDisable()
        {
            if (_currentFormation != null)
            {
                _currentFormation.RemoveFromSlot(this);
            }
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes required components. Override in derived classes.
        /// </summary>
        protected virtual void InitializeComponents()
        {
            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }
            
            if (_hitCollider == null)
            {
                _hitCollider = GetComponent<Collider>();
            }
        }
        
        /// <summary>
        /// Initializes health values. Override for custom health logic.
        /// </summary>
        protected virtual void InitializeHealth()
        {
            _currentHealth = MaxHealth;
        }
        
        /// <summary>
        /// Initializes the state machine. Must be implemented by derived classes.
        /// </summary>
        protected abstract void InitializeStateMachine();
        
        #endregion

        #region IDamageable Implementation
        
        public virtual float TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || _isInvulnerable)
            {
                return 0f;
            }
            
            float actualDamage = CalculateDamage(damageInfo);
            _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);
            
            DebugLogger.Log($"Enemy {_targetId} took {actualDamage} damage. Health: {_currentHealth}/{MaxHealth}", 
                LogCategory.Combat, this);
            
            OnDamaged?.Invoke(this, damageInfo);
            OnDamageReceived(damageInfo, actualDamage);
            
            if (_config != null && _config.InvulnerabilityDuration > 0)
            {
                SetInvulnerable(_config.InvulnerabilityDuration);
            }
            
            if (!IsAlive)
            {
                Die();
            }
            
            return actualDamage;
        }
        
        public virtual float Heal(float amount)
        {
            if (!IsAlive) return 0f;
            
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
            float actualHeal = _currentHealth - previousHealth;
            
            DebugLogger.Log($"Enemy {_targetId} healed {actualHeal}. Health: {_currentHealth}/{MaxHealth}", 
                LogCategory.Combat, this);
            
            return actualHeal;
        }
        
        public virtual void Kill()
        {
            if (!IsAlive) return;
            
            _currentHealth = 0f;
            Die();
        }
        
        public virtual void SetInvulnerable(float duration)
        {
            _isInvulnerable = true;
            _invulnerabilityTimer = duration;
            
            DebugLogger.LogVerbose($"Enemy {_targetId} invulnerable for {duration}s", LogCategory.Combat, this);
        }
        
        public virtual void SetInvulnerable(bool invulnerable)
        {
            _isInvulnerable = invulnerable;
            if (!invulnerable)
            {
                _invulnerabilityTimer = 0f;
            }
            else
            {
                _invulnerabilityTimer = float.MaxValue;
            }
        }
        
        #endregion

        #region IPriorityTarget Implementation
        
        public virtual int CalculatePriority()
        {
            int previousPriority = _cachedPriority;
            _cachedPriority = BasePriority;
            
            // Elite bonus
            if (IsElite && _config != null)
            {
                _cachedPriority += _config.ElitePriorityBonus;
            }
            
            // Low health bonus
            if (_config != null && HealthNormalized <= _config.LowHealthThreshold)
            {
                _cachedPriority += _config.LowHealthPriorityBonus;
            }
            
            // Add modifiers
            foreach (var modifier in _priorityModifiers)
            {
                _cachedPriority += modifier.Value;
            }
            
            _cachedPriority = Mathf.Clamp(_cachedPriority, 1, 10);
            
            if (previousPriority != _cachedPriority)
            {
                OnPriorityChanged?.Invoke(this, previousPriority, _cachedPriority);
            }
            
            return _cachedPriority;
        }
        
        public virtual void AddPriorityModifier(int modifier, float duration, string modifierId)
        {
            // Remove existing modifier with same ID
            RemovePriorityModifier(modifierId);
            
            _priorityModifiers.Add(new PriorityModifier(modifierId, modifier, duration));
            CalculatePriority();
            
            DebugLogger.LogVerbose($"Enemy {_targetId} added priority modifier: {modifierId} (+{modifier})", 
                LogCategory.General, this);
        }
        
        public virtual void AddPriorityModifier(PriorityModifier modifier)
        {
            if (modifier == null) return;
            AddPriorityModifier(modifier.Value, modifier.Duration, modifier.ModifierId);
        }
        
        public virtual void RemovePriorityModifier(string modifierId)
        {
            _priorityModifiers.RemoveAll(m => m.ModifierId == modifierId);
            CalculatePriority();
        }
        
        public virtual void ClearPriorityModifiers()
        {
            _priorityModifiers.Clear();
            CalculatePriority();
        }
        
        #endregion

        #region Formation Methods
        
        /// <summary>
        /// Assigns this enemy to a formation slot.
        /// </summary>
        public virtual void AssignToFormation(IFormation formation, int slotIndex)
        {
            if (_currentFormation != null)
            {
                _currentFormation.RemoveFromSlot(this);
            }
            
            _currentFormation = formation;
            _formationSlotIndex = slotIndex;
            
            if (formation != null)
            {
                formation.AssignSlot(slotIndex, this);
            }
            
            DebugLogger.Log($"Enemy {_targetId} assigned to formation slot {slotIndex}", 
                LogCategory.Formation, this);
        }
        
        /// <summary>
        /// Removes this enemy from current formation.
        /// </summary>
        public virtual void RemoveFromFormation()
        {
            if (_currentFormation != null)
            {
                _currentFormation.RemoveFromSlot(this);
                _currentFormation = null;
                _formationSlotIndex = -1;
                
                DebugLogger.Log($"Enemy {_targetId} removed from formation", LogCategory.Formation, this);
            }
        }
        
        /// <summary>
        /// Gets the target position for this enemy's formation slot.
        /// </summary>
        public virtual Vector3 GetFormationTargetPosition()
        {
            if (_currentFormation != null && _formationSlotIndex >= 0)
            {
                return _currentFormation.GetSlotPosition(_formationSlotIndex);
            }
            return transform.position;
        }
        
        #endregion

        #region Movement Methods (Virtual)
        
        /// <summary>
        /// Updates movement each physics frame. Override for custom movement.
        /// </summary>
        protected virtual void UpdateMovement()
        {
            // Default implementation - apply velocity
            if (_velocity.sqrMagnitude > 0.001f)
            {
                transform.position += _velocity * Time.fixedDeltaTime;
                
                // Rotate to face movement direction
                if (_config != null && _velocity.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, 
                        targetRotation, 
                        _config.RotationSpeed * Time.fixedDeltaTime
                    );
                }
            }
        }
        
        /// <summary>
        /// Sets the velocity for this enemy.
        /// </summary>
        public virtual void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }
        
        /// <summary>
        /// Adds force to velocity.
        /// </summary>
        public virtual void AddVelocity(Vector3 additionalVelocity)
        {
            _velocity += additionalVelocity;
            
            // Clamp to max speed
            if (_config != null && _velocity.magnitude > _config.SpeedRange.y)
            {
                _velocity = _velocity.normalized * _config.SpeedRange.y;
            }
        }
        
        /// <summary>
        /// Sets the target to pursue/attack.
        /// </summary>
        public virtual void SetTarget(Transform target)
        {
            _currentTarget = target;
            
            DebugLogger.LogVerbose($"Enemy {_targetId} target set to: {target?.name ?? "null"}", 
                LogCategory.Combat, this);
        }
        
        #endregion

        #region State Machine Methods
        
        private void UpdateStateMachine()
        {
            _stateMachine?.Update();
        }
        
        private void FixedUpdateStateMachine()
        {
            _stateMachine?.FixedUpdate();
        }
        
        /// <summary>
        /// Changes the current AI state.
        /// </summary>
        public virtual void ChangeState(IState newState)
        {
            _stateMachine?.ChangeState(newState);
        }
        
        #endregion

        #region Protected Virtual Methods
        
        /// <summary>
        /// Calculates final damage after resistances. Override for custom damage calculation.
        /// </summary>
        protected virtual float CalculateDamage(DamageInfo damageInfo)
        {
            return damageInfo.Amount;
        }
        
        /// <summary>
        /// Called when damage is received. Override for visual/audio feedback.
        /// </summary>
        protected virtual void OnDamageReceived(DamageInfo damageInfo, float actualDamage)
        {
            // Override in derived classes for effects
        }
        
        /// <summary>
        /// Called when the enemy dies. Override for death effects.
        /// </summary>
        protected virtual void Die()
        {
            DebugLogger.Log($"Enemy {_targetId} died", LogCategory.Combat, this);
            
            if (_currentFormation != null)
            {
                _currentFormation.OnUnitRemoved(_formationSlotIndex);
            }
            
            OnDeath?.Invoke(this);
            OnDeathEffects();
        }
        
        /// <summary>
        /// Called during death for visual/audio effects. Override in derived classes.
        /// </summary>
        protected virtual void OnDeathEffects()
        {
            // Override in derived classes
        }
        
        #endregion

        #region Update Methods
        
        private void UpdateInvulnerability()
        {
            if (_isInvulnerable)
            {
                _invulnerabilityTimer -= Time.deltaTime;
                if (_invulnerabilityTimer <= 0f)
                {
                    _isInvulnerable = false;
                }
            }
        }
        
        private void UpdatePriorityModifiers()
        {
            bool changed = false;
            
            for (int i = _priorityModifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _priorityModifiers[i];
                if (modifier.Duration > 0) // Timed modifier
                {
                    modifier.RemainingTime -= Time.deltaTime;
                    if (modifier.RemainingTime <= 0)
                    {
                        _priorityModifiers.RemoveAt(i);
                        changed = true;
                    }
                }
            }
            
            if (changed)
            {
                CalculatePriority();
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Initializes the enemy with a configuration.
        /// </summary>
        public virtual void Initialize(FlyingEnemyConfig config)
        {
            _config = config;
            InitializeHealth();
            
            DebugLogger.Log($"Enemy {_targetId} initialized with config: {config?.EnemyId ?? "null"}", 
                LogCategory.Spawning, this);
        }
        
        /// <summary>
        /// Resets the enemy for object pooling reuse.
        /// </summary>
        public virtual void ResetForPool()
        {
            _currentHealth = MaxHealth;
            _isInvulnerable = false;
            _invulnerabilityTimer = 0f;
            _velocity = Vector3.zero;
            _currentTarget = null;
            _priorityModifiers.Clear();
            _cachedPriority = BasePriority;
            
            RemoveFromFormation();
            
            DebugLogger.LogVerbose($"Enemy {_targetId} reset for pool", LogCategory.Pooling, this);
        }
        
        #endregion
    }
}
