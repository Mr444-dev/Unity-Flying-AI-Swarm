using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Events;
using FlyingAI.Formations;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;
using FlyingAIEvents = FlyingAI.Events.FlyingAIEvents;

namespace FlyingAI.Managers
{
    /// <summary>
    /// Manages formations and handles dynamic slot reassignment.
    /// </summary>
    public class FormationManager : MonoBehaviour
    {
        #region Singleton
        
        private static FormationManager _instance;
        public static FormationManager Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Settings")]
        [SerializeField] private float _slotReassignmentDelay = 0.5f;
        
        #endregion

        #region Private Fields
        
        private Dictionary<int, IFormation> _activeFormations = new Dictionary<int, IFormation>();
        private int _formationIdCounter;
        
        #endregion

        #region Properties
        
        public int ActiveFormationCount => _activeFormations.Count;
        
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
            
            FlyingAIEvents.OnEnemyDied += OnEnemyDied;
        }
        
        private void OnDestroy()
        {
            FlyingAIEvents.OnEnemyDied -= OnEnemyDied;
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Creates a new formation of the specified type at the given position.
        /// </summary>
        public IFormation CreateFormation(FormationConfig config, Vector3 position, Vector3 forward)
        {
            if (config == null) return null;
            
            IFormation formation = FormationFactory.Create(config);
            if (formation != null)
            {
                int id = _formationIdCounter++;
                _activeFormations.Add(id, formation);
                
                formation.Initialize(position, forward);
                
                DebugLogger.Log($"Created {config.Type} formation (ID: {id})", LogCategory.Formation);
            }
            
            return formation;
        }

        /// <summary>
        /// Creates a circle formation programmatically without a config.
        /// </summary>
        public IFormation CreateCircleFormation(Vector3 center, Vector3 forward, int slotCount, float radius)
        {
            var formation = new CircleFormation(slotCount, radius);
            int id = _formationIdCounter++;
            _activeFormations.Add(id, formation);
            
            formation.Initialize(center, forward);
            
            DebugLogger.Log($"Created Dynamic Circle formation (ID: {id}, Slots: {slotCount})", LogCategory.Formation);
            
            return formation;
        }
        
        /// <summary>
        /// Assigns a list of enemies to a formation.
        /// </summary>
        public void AssignEnemiesToFormation(IFormation formation, List<FlyingEnemyBase> enemies)
        {
            if (formation == null || enemies == null) return;
            
            foreach (var enemy in enemies)
            {
                formation.AddUnit(enemy);
            }
        }
        
        /// <summary>
        /// Disbands an active formation.
        /// </summary>
        public void DisbandFormation(IFormation formation)
        {
            if (formation == null) return;
            
            int targetId = -1;
            foreach (var kvp in _activeFormations)
            {
                if (kvp.Value == formation)
                {
                    targetId = kvp.Key;
                    break;
                }
            }
            
            if (targetId != -1)
            {
                formation.Disband();
                _activeFormations.Remove(targetId);
                DebugLogger.Log($"Disbanded formation (ID: {targetId})", LogCategory.Formation);
            }
        }
        
        #endregion

        #region Event Handlers
        
        private void OnEnemyDied(FlyingEnemyBase enemy)
        {
            // Formation re-evaluation happens automatically in IFormation implementations
            // but we could trigger additional logic here if needed.
        }
        
        #endregion
    }
}
