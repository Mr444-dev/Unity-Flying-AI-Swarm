using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Utils;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Types of threats that enemies can respond to.
    /// </summary>
    public enum ThreatType
    {
        None,
        Projectile,
        Player,
        AreaDamage,
        Collision,
        EnvironmentalHazard
    }

    /// <summary>
    /// Threat data for processing.
    /// </summary>
    public struct ThreatData
    {
        public ThreatType Type;
        public Vector3 Position;
        public Vector3 Velocity;
        public float Radius;
        public float DangerLevel;
        public float TimeToImpact;
        public Transform Source;
        
        public static ThreatData Create(ThreatType type, Vector3 pos, float danger)
        {
            return new ThreatData
            {
                Type = type,
                Position = pos,
                Velocity = Vector3.zero,
                Radius = 1f,
                DangerLevel = danger,
                TimeToImpact = float.MaxValue,
                Source = null
            };
        }
    }

    /// <summary>
    /// System for detecting and responding to threats.
    /// </summary>
    public class ThreatResponseSystem : MonoBehaviour
    {
        #region Singleton
        
        private static ThreatResponseSystem _instance;
        public static ThreatResponseSystem Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Detection")]
        [SerializeField] private float _threatDetectionRadius = 20f;
        [SerializeField] private LayerMask _threatLayers;
        [SerializeField] private float _updateInterval = 0.1f;
        
        [Header("Response")]
        [SerializeField] private float _projectileDodgeReactionTime = 0.3f;
        [SerializeField] private float _minDangerToReact = 0.3f;
        [SerializeField] private float _evasionForceMultiplier = 2f;
        
        #endregion

        #region Private Fields
        
        private List<ThreatData> _activeThreats = new List<ThreatData>();
        private Dictionary<int, float> _enemyLastDodgeTime = new Dictionary<int, float>();
        private float _lastUpdateTime;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        
        private void Update()
        {
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                CleanupExpiredThreats();
                _lastUpdateTime = Time.time;
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Registers a new threat.
        /// </summary>
        public void RegisterThreat(ThreatData threat)
        {
            _activeThreats.Add(threat);
            DebugLogger.LogVerbose($"Threat registered: {threat.Type} at {threat.Position}", LogCategory.Combat);
        }
        
        /// <summary>
        /// Registers a projectile as a threat.
        /// </summary>
        public void RegisterProjectileThreat(Vector3 position, Vector3 velocity, float radius, Transform source)
        {
            ThreatData threat = new ThreatData
            {
                Type = ThreatType.Projectile,
                Position = position,
                Velocity = velocity,
                Radius = radius,
                DangerLevel = 1f,
                TimeToImpact = 0f,
                Source = source
            };
            _activeThreats.Add(threat);
        }
        
        /// <summary>
        /// Calculates evasion force for an enemy based on nearby threats.
        /// </summary>
        public Vector3 CalculateEvasionForce(FlyingEnemyBase enemy)
        {
            Vector3 totalEvasion = Vector3.zero;
            Vector3 enemyPos = enemy.Position;
            int entityId = enemy.TargetId;
            
            foreach (var threat in _activeThreats)
            {
                // Skip own projectiles
                if (threat.Source != null && threat.Source.gameObject == enemy.gameObject)
                    continue;
                
                Vector3 toThreat = threat.Position - enemyPos;
                float distance = toThreat.magnitude;
                
                if (distance > _threatDetectionRadius) continue;
                
                float dangerFactor = CalculateDanger(enemy, threat);
                
                if (dangerFactor < _minDangerToReact) continue;
                
                // Calculate evasion direction
                Vector3 evasionDir = CalculateEvasionDirection(enemy, threat);
                float urgency = Mathf.Clamp01(dangerFactor) * _evasionForceMultiplier;
                
                totalEvasion += evasionDir * urgency;
            }
            
            return totalEvasion;
        }
        
        /// <summary>
        /// Checks if enemy should perform emergency dodge.
        /// </summary>
        public bool ShouldEmergencyDodge(FlyingEnemyBase enemy, out Vector3 dodgeDirection)
        {
            dodgeDirection = Vector3.zero;
            int entityId = enemy.TargetId;
            
            // Cooldown check
            if (_enemyLastDodgeTime.TryGetValue(entityId, out float lastDodge))
            {
                if (Time.time - lastDodge < _projectileDodgeReactionTime * 2f)
                    return false;
            }
            
            ThreatData? criticalThreat = null;
            float minTimeToImpact = float.MaxValue;
            
            foreach (var threat in _activeThreats)
            {
                if (threat.Type != ThreatType.Projectile) continue;
                
                float timeToImpact = CalculateTimeToImpact(enemy, threat);
                
                if (timeToImpact < _projectileDodgeReactionTime && timeToImpact < minTimeToImpact)
                {
                    minTimeToImpact = timeToImpact;
                    criticalThreat = threat;
                }
            }
            
            if (criticalThreat.HasValue)
            {
                dodgeDirection = CalculateEmergencyDodge(enemy, criticalThreat.Value);
                _enemyLastDodgeTime[entityId] = Time.time;
                return true;
            }
            
            return false;
        }
        
        #endregion

        #region Calculation Methods
        
        private float CalculateDanger(FlyingEnemyBase enemy, ThreatData threat)
        {
            Vector3 enemyPos = enemy.Position;
            Vector3 enemyVel = enemy.Velocity;
            
            if (threat.Type == ThreatType.Projectile)
            {
                // Predict collision
                float timeToImpact = CalculateTimeToImpact(enemy, threat);
                if (timeToImpact < 0 || timeToImpact > 3f) return 0f;
                
                return threat.DangerLevel * (1f - timeToImpact / 3f);
            }
            
            // Distance-based danger for other types
            float distance = Vector3.Distance(enemyPos, threat.Position);
            float normalizedDist = distance / _threatDetectionRadius;
            return threat.DangerLevel * (1f - Mathf.Clamp01(normalizedDist));
        }
        
        private float CalculateTimeToImpact(FlyingEnemyBase enemy, ThreatData threat)
        {
            Vector3 relativePos = threat.Position - enemy.Position;
            Vector3 relativeVel = threat.Velocity - enemy.Velocity;
            
            float speedSq = relativeVel.sqrMagnitude;
            if (speedSq < 0.001f) return float.MaxValue;
            
            float closestTime = -Vector3.Dot(relativePos, relativeVel) / speedSq;
            if (closestTime < 0) return float.MaxValue;
            
            Vector3 closestPos = relativePos + relativeVel * closestTime;
            float closestDist = closestPos.magnitude;
            
            float hitRadius = threat.Radius + 2f; // Enemy radius estimate
            if (closestDist > hitRadius) return float.MaxValue;
            
            return closestTime;
        }
        
        private Vector3 CalculateEvasionDirection(FlyingEnemyBase enemy, ThreatData threat)
        {
            Vector3 toThreat = (threat.Position - enemy.Position).normalized;
            Vector3 threatVelNorm = threat.Velocity.normalized;
            
            // Perpendicular to threat velocity
            Vector3 perpendicular = Vector3.Cross(threatVelNorm, Vector3.up).normalized;
            
            // Choose side based on current velocity
            if (Vector3.Dot(enemy.Velocity, perpendicular) < 0)
            {
                perpendicular = -perpendicular;
            }
            
            // Blend with away-from-threat direction
            Vector3 awayDir = -toThreat;
            return (perpendicular * 0.7f + awayDir * 0.3f).normalized;
        }
        
        private Vector3 CalculateEmergencyDodge(FlyingEnemyBase enemy, ThreatData threat)
        {
            Vector3 threatVel = threat.Velocity.normalized;
            
            // Barrel roll direction - perpendicular to threat approach
            Vector3 right = Vector3.Cross(threatVel, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, threatVel).normalized;
            
            // Randomize dodge direction
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 dodgeDir = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
            
            return dodgeDir;
        }
        
        private void CleanupExpiredThreats()
        {
            _activeThreats.RemoveAll(t =>
            {
                if (t.Type == ThreatType.Projectile)
                {
                    // Remove projectiles that have traveled far
                    return t.Source == null;
                }
                return false;
            });
        }
        
        #endregion
    }
}
