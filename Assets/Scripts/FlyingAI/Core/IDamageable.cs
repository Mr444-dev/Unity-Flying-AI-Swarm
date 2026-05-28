using UnityEngine;

namespace FlyingAI.Core
{
    /// <summary>
    /// Damage information structure.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary>
        /// Amount of damage dealt.
        /// </summary>
        public float Amount;
        
        /// <summary>
        /// Source of the damage.
        /// </summary>
        public GameObject Source;
        
        /// <summary>
        /// Point where damage was applied.
        /// </summary>
        public Vector3 HitPoint;
        
        /// <summary>
        /// Direction of the damage (knockback direction).
        /// </summary>
        public Vector3 Direction;
        
        /// <summary>
        /// Was this a critical hit.
        /// </summary>
        public bool IsCritical;
        
        /// <summary>
        /// Type of damage (for resistance calculations).
        /// </summary>
        public DamageType Type;
        
        public DamageInfo(float amount, GameObject source = null, Vector3 hitPoint = default, 
            Vector3 direction = default, bool isCritical = false, DamageType type = DamageType.Normal)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            Direction = direction;
            IsCritical = isCritical;
            Type = type;
        }
    }
    
    /// <summary>
    /// Types of damage for resistance calculations.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Energy,
        Explosive,
        Collision,
        Environmental
    }

    /// <summary>
    /// Interface for objects that can take damage.
    /// </summary>
    public interface IDamageable
    {
        #region Properties
        
        /// <summary>
        /// Current health points.
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// Maximum health points.
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// Whether the entity is alive.
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Whether the entity is currently invulnerable.
        /// </summary>
        bool IsInvulnerable { get; }
        
        /// <summary>
        /// Health as a normalized value (0-1).
        /// </summary>
        float HealthNormalized { get; }
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Applies damage to this entity.
        /// </summary>
        /// <param name="damageInfo">Damage information.</param>
        /// <returns>Actual damage dealt after resistances.</returns>
        float TakeDamage(DamageInfo damageInfo);
        
        /// <summary>
        /// Heals the entity.
        /// </summary>
        /// <param name="amount">Amount to heal.</param>
        /// <returns>Actual amount healed.</returns>
        float Heal(float amount);
        
        /// <summary>
        /// Kills the entity immediately.
        /// </summary>
        void Kill();
        
        /// <summary>
        /// Makes the entity invulnerable for a duration.
        /// </summary>
        /// <param name="duration">Invulnerability duration.</param>
        void SetInvulnerable(float duration);
        
        #endregion
    }
}
