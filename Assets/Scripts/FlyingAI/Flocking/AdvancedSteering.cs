using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using FlyingAI.Core;
using FlyingAI.Utils;

namespace FlyingAI.Flocking
{
    /// <summary>
    /// Path smoother for flying AI movement.
    /// Provides Catmull-Rom spline smoothing for natural flight paths.
    /// </summary>
    public class PathSmoother
    {
        #region Private Fields
        
        private List<Vector3> _rawPath = new List<Vector3>();
        private List<Vector3> _smoothedPath = new List<Vector3>();
        private int _smoothingResolution;
        private float _tension;
        
        #endregion

        #region Constructor
        
        public PathSmoother(int resolution = 10, float tension = 0.5f)
        {
            _smoothingResolution = resolution;
            _tension = tension;
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Smooths a path using Catmull-Rom splines.
        /// </summary>
        public List<Vector3> SmoothPath(List<Vector3> rawPath)
        {
            _rawPath.Clear();
            _rawPath.AddRange(rawPath);
            _smoothedPath.Clear();
            
            if (_rawPath.Count < 2)
            {
                _smoothedPath.AddRange(_rawPath);
                return _smoothedPath;
            }
            
            // Add phantom points for smooth endpoints
            _rawPath.Insert(0, _rawPath[0] + (_rawPath[0] - _rawPath[1]));
            _rawPath.Add(_rawPath[^1] + (_rawPath[^1] - _rawPath[^2]));
            
            // Generate smooth points
            for (int i = 1; i < _rawPath.Count - 2; i++)
            {
                for (int j = 0; j < _smoothingResolution; j++)
                {
                    float t = j / (float)_smoothingResolution;
                    Vector3 point = CatmullRom(
                        _rawPath[i - 1],
                        _rawPath[i],
                        _rawPath[i + 1],
                        _rawPath[i + 2],
                        t,
                        _tension
                    );
                    _smoothedPath.Add(point);
                }
            }
            
            // Add final point
            _smoothedPath.Add(_rawPath[^2]);
            
            return _smoothedPath;
        }
        
        /// <summary>
        /// Gets a smoothed target position along the path.
        /// </summary>
        public Vector3 GetSmoothedTarget(Vector3 currentPosition, List<Vector3> path, float lookAhead, out int nextIndex)
        {
            nextIndex = 0;
            if (path.Count == 0) return currentPosition;
            
            // Find closest point on path
            float minDist = float.MaxValue;
            int closestIndex = 0;
            
            for (int i = 0; i < path.Count; i++)
            {
                float dist = Vector3.Distance(currentPosition, path[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = i;
                }
            }
            
            // Find look-ahead point
            float accumulatedDist = 0f;
            nextIndex = closestIndex;
            
            for (int i = closestIndex; i < path.Count - 1; i++)
            {
                accumulatedDist += Vector3.Distance(path[i], path[i + 1]);
                nextIndex = i + 1;
                
                if (accumulatedDist >= lookAhead)
                {
                    break;
                }
            }
            
            return path[nextIndex];
        }
        
        #endregion

        #region Catmull-Rom Spline
        
        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            float s = (1f - tension) / 2f;
            
            float b1 = s * ((-t3 + 2f * t2 - t));
            float b2 = s * ((-t3 + t2)) + (2f * t3 - 3f * t2 + 1f);
            float b3 = s * ((t3 - 2f * t2 + t)) + (-2f * t3 + 3f * t2);
            float b4 = s * ((t3 - t2));
            
            return p0 * b1 + p1 * b2 + p2 * b3 + p3 * b4;
        }
        
        #endregion
    }

    /// <summary>
    /// Handles reaggregation behavior - returning scattered units to formation.
    /// </summary>
    public class ReaggregationSystem
    {
        #region Settings
        
        private float _reaggregationSpeed = 1.5f;
        private float _scatterThreshold = 15f;
        private float _cohesionBoostMultiplier = 2f;
        private float _formationPriorityThreshold = 0.5f;
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Calculates reaggregation force for a scattered unit.
        /// </summary>
        public Vector3 CalculateReaggregationForce(FlyingEnemyBase enemy, Vector3 formationCenter, 
            Vector3 swarmCenter, float formationWeight)
        {
            Vector3 enemyPos = enemy.Position;
            
            float distToFormation = Vector3.Distance(enemyPos, formationCenter);
            float distToSwarm = Vector3.Distance(enemyPos, swarmCenter);
            
            Vector3 force = Vector3.zero;
            
            // If too far from formation
            if (distToFormation > _scatterThreshold)
            {
                Vector3 toFormation = (formationCenter - enemyPos).normalized;
                float urgency = Mathf.Clamp01((distToFormation - _scatterThreshold) / _scatterThreshold);
                force += toFormation * urgency * _reaggregationSpeed * _cohesionBoostMultiplier;
            }
            
            // Also consider swarm center for isolated units
            if (enemy.CurrentFormation == null && distToSwarm > _scatterThreshold * 2f)
            {
                Vector3 toSwarm = (swarmCenter - enemyPos).normalized;
                float urgency = Mathf.Clamp01((distToSwarm - _scatterThreshold * 2f) / _scatterThreshold);
                force += toSwarm * urgency * _reaggregationSpeed;
            }
            
            // Blend with formation weight
            if (formationWeight > _formationPriorityThreshold && enemy.CurrentFormation != null)
            {
                Vector3 toSlot = enemy.GetFormationTargetPosition() - enemyPos;
                force = Vector3.Lerp(force, toSlot.normalized * _reaggregationSpeed, formationWeight);
            }
            
            return force;
        }
        
        /// <summary>
        /// Checks if unit is considered scattered.
        /// </summary>
        public bool IsScattered(FlyingEnemyBase enemy, Vector3 groupCenter)
        {
            return Vector3.Distance(enemy.Position, groupCenter) > _scatterThreshold;
        }
        
        /// <summary>
        /// Gets priority multiplier for scattered units (higher = rejoin faster).
        /// </summary>
        public float GetScatterPriority(FlyingEnemyBase enemy, Vector3 groupCenter)
        {
            float dist = Vector3.Distance(enemy.Position, groupCenter);
            if (dist <= _scatterThreshold) return 1f;
            
            return 1f + Mathf.Clamp01((dist - _scatterThreshold) / (_scatterThreshold * 2f));
        }
        
        #endregion

        #region Configuration
        
        public void Configure(float reaggregationSpeed, float scatterThreshold, 
            float cohesionBoost, float formationPriority)
        {
            _reaggregationSpeed = reaggregationSpeed;
            _scatterThreshold = scatterThreshold;
            _cohesionBoostMultiplier = cohesionBoost;
            _formationPriorityThreshold = formationPriority;
        }
        
        #endregion
    }

    /// <summary>
    /// Advanced steering behaviors for flying AI.
    /// </summary>
    public static class SteeringBehaviors
    {
        /// <summary>
        /// Calculates arrival steering with smooth deceleration.
        /// </summary>
        public static Vector3 Arrive(Vector3 position, Vector3 target, float maxSpeed, float slowingRadius)
        {
            Vector3 toTarget = target - position;
            float distance = toTarget.magnitude;
            
            if (distance < 0.01f) return Vector3.zero;
            
            float targetSpeed = maxSpeed;
            if (distance < slowingRadius)
            {
                targetSpeed = maxSpeed * (distance / slowingRadius);
            }
            
            Vector3 desiredVelocity = toTarget.normalized * targetSpeed;
            return desiredVelocity;
        }
        
        /// <summary>
        /// Calculates pursuit steering for predicting target movement.
        /// </summary>
        public static Vector3 Pursuit(Vector3 position, Vector3 velocity, 
            Vector3 targetPos, Vector3 targetVelocity, float maxSpeed)
        {
            Vector3 toTarget = targetPos - position;
            float distance = toTarget.magnitude;
            
            // Predict future position
            float predictionTime = distance / maxSpeed;
            Vector3 predictedPos = targetPos + targetVelocity * predictionTime;
            
            return Seek(position, predictedPos, maxSpeed);
        }
        
        /// <summary>
        /// Basic seek steering.
        /// </summary>
        public static Vector3 Seek(Vector3 position, Vector3 target, float maxSpeed)
        {
            Vector3 desiredVelocity = (target - position).normalized * maxSpeed;
            return desiredVelocity;
        }
        
        /// <summary>
        /// Flee steering - opposite of seek.
        /// </summary>
        public static Vector3 Flee(Vector3 position, Vector3 threat, float maxSpeed)
        {
            Vector3 desiredVelocity = (position - threat).normalized * maxSpeed;
            return desiredVelocity;
        }
        
        /// <summary>
        /// Wander steering for natural random movement.
        /// </summary>
        public static Vector3 Wander(Vector3 forward, float wanderRadius, float wanderDistance, 
            float wanderJitter, ref Vector3 wanderTarget)
        {
            // Add random jitter to wander target
            wanderTarget += new Vector3(
                UnityEngine.Random.Range(-1f, 1f) * wanderJitter,
                UnityEngine.Random.Range(-0.5f, 0.5f) * wanderJitter,
                UnityEngine.Random.Range(-1f, 1f) * wanderJitter
            );
            
            // Project onto circle
            wanderTarget = wanderTarget.normalized * wanderRadius;
            
            // Project in front of agent
            Vector3 targetLocal = wanderTarget + forward * wanderDistance;
            
            return targetLocal.normalized;
        }
        
        /// <summary>
        /// Offset pursuit - follow at an offset position.
        /// </summary>
        public static Vector3 OffsetPursuit(Vector3 position, Vector3 velocity,
            Vector3 leaderPos, Vector3 leaderVelocity, Vector3 offset, float maxSpeed)
        {
            Vector3 worldOffset = leaderPos + offset;
            
            Vector3 toOffset = worldOffset - position;
            float lookAhead = toOffset.magnitude / maxSpeed;
            
            Vector3 predictedPos = worldOffset + leaderVelocity * lookAhead;
            
            return Arrive(position, predictedPos, maxSpeed, 5f);
        }
        
        /// <summary>
        /// Interpose - position between two targets.
        /// </summary>
        public static Vector3 Interpose(Vector3 position, Vector3 targetA, Vector3 targetB, 
            Vector3 velA, Vector3 velB, float maxSpeed)
        {
            float midpointDist = ((targetA + targetB) * 0.5f - position).magnitude;
            float timeToMidpoint = midpointDist / maxSpeed;
            
            Vector3 futureA = targetA + velA * timeToMidpoint;
            Vector3 futureB = targetB + velB * timeToMidpoint;
            
            Vector3 midpoint = (futureA + futureB) * 0.5f;
            
            return Arrive(position, midpoint, maxSpeed, 3f);
        }
    }
}
