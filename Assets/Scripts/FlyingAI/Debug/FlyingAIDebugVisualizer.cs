using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Flocking;
using FlyingAI.Managers;

#if UNITY_EDITOR
namespace FlyingAI.Debugging
{
    /// <summary>
    /// Visual debug overlay for flying AI system.
    /// Shows velocities, formations, priorities, and flocking info.
    /// </summary>
    public class FlyingAIDebugVisualizer : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Enable/Disable")]
        [SerializeField] private bool _showVelocities = true;
        [SerializeField] private bool _showFormations = true;
        [SerializeField] private bool _showPriorities = true;
        [SerializeField] private bool _showFlockingRadius = false;
        [SerializeField] private bool _showNeighborConnections = false;
        [SerializeField] private bool _showThreatVectors = true;
        [SerializeField] private bool _showSteeringForces = false;
        
        [Header("Visual Settings")]
        [SerializeField] private float _velocityScale = 0.5f;
        [SerializeField] private float _forceScale = 0.3f;
        [SerializeField] private float _labelOffset = 2f;
        
        [Header("Colors")]
        [SerializeField] private Color _velocityColor = Color.green;
        [SerializeField] private Color _formationColor = Color.cyan;
        [SerializeField] private Color _priorityHighColor = Color.red;
        [SerializeField] private Color _priorityLowColor = Color.gray;
        [SerializeField] private Color _flockingRadiusColor = new Color(1, 1, 0, 0.2f);
        [SerializeField] private Color _neighborColor = new Color(0, 1, 1, 0.3f);
        [SerializeField] private Color _threatColor = Color.magenta;
        [SerializeField] private Color _steeringSeparation = Color.red;
        [SerializeField] private Color _steeringAlignment = Color.blue;
        [SerializeField] private Color _steeringCohesion = Color.green;
        
        #endregion

        #region Private Fields
        
        private GUIStyle _labelStyle;
        
        #endregion

        #region Unity Lifecycle
        
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            if (_showVelocities) DrawVelocities();
            if (_showFormations) DrawFormations();
            if (_showFlockingRadius) DrawFlockingRadii();
            if (_showNeighborConnections) DrawNeighborConnections();
            if (_showThreatVectors) DrawThreatVectors();
        }
        
        private void OnGUI()
        {
            if (!Application.isPlaying || !_showPriorities) return;
            
            InitializeStyle();
            DrawPriorityLabels();
            DrawSwarmInfo();
        }
        
        #endregion

        #region Gizmo Drawing
        
        private void DrawVelocities()
        {
            if (EnemyPrioritySystem.Instance == null) return;
            
            foreach (var enemy in EnemyPrioritySystem.Instance.GetSortedByPriority())
            {
                if (enemy == null) continue;
                
                Vector3 pos = enemy.Position;
                Vector3 vel = enemy.Velocity;
                
                Gizmos.color = _velocityColor;
                DrawArrow(pos, pos + vel * _velocityScale);
            }
        }
        
        private void DrawFormations()
        {
            if (FormationManager.Instance == null) return;
            
            // Draw formation slots and connections
            // This would need access to active formations
        }
        
        private void DrawFlockingRadii()
        {
            if (EnemyPrioritySystem.Instance == null) return;
            
            foreach (var enemy in EnemyPrioritySystem.Instance.GetSortedByPriority())
            {
                if (enemy == null || enemy.Config == null) continue;
                
                Gizmos.color = _flockingRadiusColor;
                Gizmos.DrawWireSphere(enemy.Position, enemy.Config.PerceptionRadius);
                
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                Gizmos.DrawWireSphere(enemy.Position, enemy.Config.AvoidanceRadius);
            }
        }
        
        private void DrawNeighborConnections()
        {
            if (FlockingSystem.Instance == null || EnemyPrioritySystem.Instance == null) return;
            
            var enemies = EnemyPrioritySystem.Instance.GetSortedByPriority();
            
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.Config == null) continue;
                
                float radius = enemy.Config.PerceptionRadius;
                
                foreach (var other in enemies)
                {
                    if (other == null || other == enemy) continue;
                    
                    float dist = Vector3.Distance(enemy.Position, other.Position);
                    if (dist <= radius)
                    {
                        Gizmos.color = _neighborColor;
                        Gizmos.DrawLine(enemy.Position, other.Position);
                    }
                }
            }
        }
        
        private void DrawThreatVectors()
        {
            if (ThreatResponseSystem.Instance == null || EnemyPrioritySystem.Instance == null) return;
            
            foreach (var enemy in EnemyPrioritySystem.Instance.GetSortedByPriority())
            {
                if (enemy == null) continue;
                
                Vector3 evasionForce = ThreatResponseSystem.Instance.CalculateEvasionForce(enemy);
                if (evasionForce.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = _threatColor;
                    DrawArrow(enemy.Position, enemy.Position + evasionForce * _forceScale);
                }
            }
        }
        
        private void DrawArrow(Vector3 start, Vector3 end)
        {
            Gizmos.DrawLine(start, end);
            
            Vector3 dir = (end - start).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            
            float arrowSize = 0.3f;
            Gizmos.DrawLine(end, end - dir * arrowSize + right * arrowSize * 0.5f);
            Gizmos.DrawLine(end, end - dir * arrowSize - right * arrowSize * 0.5f);
        }
        
        #endregion

        #region GUI Drawing
        
        private void InitializeStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }
        
        private void DrawPriorityLabels()
        {
            if (EnemyPrioritySystem.Instance == null || Camera.main == null) return;
            
            int rank = 1;
            foreach (var enemy in EnemyPrioritySystem.Instance.GetHighestPriorityEnemies(10))
            {
                if (enemy == null) continue;
                
                Vector3 screenPos = Camera.main.WorldToScreenPoint(
                    enemy.Position + Vector3.up * _labelOffset
                );
                
                if (screenPos.z > 0)
                {
                    float t = enemy.CurrentPriority / 10f;
                    _labelStyle.normal.textColor = Color.Lerp(_priorityLowColor, _priorityHighColor, t);
                    
                    Rect labelRect = new Rect(screenPos.x - 30, Screen.height - screenPos.y, 60, 25);
                    GUI.Label(labelRect, $"#{rank} P:{enemy.CurrentPriority}", _labelStyle);
                    rank++;
                }
            }
        }
        
        private void DrawSwarmInfo()
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Flying AI Debug", EditorStyles.boldLabel);
            
            if (FlockingSystem.Instance != null)
            {
                GUILayout.Label($"Entities: {FlockingSystem.Instance.EntityCount}");
            }
            
            if (SwarmController.Instance != null)
            {
                GUILayout.Label($"Swarm Size: {SwarmController.Instance.SwarmSize}");
                GUILayout.Label($"Active Attackers: {SwarmController.Instance.ActiveAttackerCount}");
                GUILayout.Label($"Aggression: {SwarmController.Instance.AggressionLevel:F2}");
            }
            
            if (SwarmTacticsController.Instance != null)
            {
                GUILayout.Label($"Tactic: {SwarmTacticsController.Instance.CurrentTactic}");
                if (SwarmTacticsController.Instance.IsTacticActive)
                {
                    GUILayout.Label($"Progress: {SwarmTacticsController.Instance.TacticProgress:P0}");
                }
            }
            
            if (DynamicObstacleManager.Instance != null)
            {
                GUILayout.Label($"Dynamic Obstacles: {DynamicObstacleManager.Instance.ObstacleCount}");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
#endif
