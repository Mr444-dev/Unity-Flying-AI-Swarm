using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FlyingAI.Core;
using FlyingAI.Formations;
using FlyingAI.States;
using FlyingAI.Utils;

namespace FlyingAI.Managers
{
    /// <summary>
    /// Types of coordinated swarm tactics.
    /// </summary>
    public enum SwarmTactic
    {
        /// <summary>Standard individual attacks.</summary>
        FreeAttack,
        
        /// <summary>All units attack simultaneously.</summary>
        CoordinatedStrike,
        
        /// <summary>Units attack in waves.</summary>
        WaveAssault,
        
        /// <summary>Encircle target then attack.</summary>
        SurroundAndEngage,
        
        /// <summary>Hit and run attacks.</summary>
        HitAndRun,
        
        /// <summary>Divide into groups for flanking.</summary>
        PincerMovement,
        
        /// <summary>Defensive formation around leader.</summary>
        DefensivePerimeter,
        
        /// <summary>Distract while others attack.</summary>
        DistractionTactic
    }

    /// <summary>
    /// Advanced tactical coordinator for swarm-level AI.
    /// Manages coordinated attacks, flanking, and tactical formations.
    /// </summary>
    public class SwarmTacticsController : MonoBehaviour
    {
        #region Singleton
        
        private static SwarmTacticsController _instance;
        public static SwarmTacticsController Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Tactics Settings")]
        [SerializeField] private SwarmTactic _currentTactic = SwarmTactic.FreeAttack;
        [SerializeField] private float _tacticExecutionTime = 10f;
        [SerializeField] private float _tacticCooldown = 5f;
        
        [Header("Coordinated Strike")]
        [SerializeField] private float _strikeChargeUpTime = 2f;
        [SerializeField] private float _strikeWindow = 1f;
        
        [Header("Wave Assault")]
        [SerializeField] private int _waveCount = 3;
        [SerializeField] private float _timeBetweenWaves = 2f;
        
        [Header("Surround")]
        [SerializeField] private float _surroundRadius = 15f;
        [SerializeField] private float _engageDelay = 1.5f;
        
        [Header("Hit and Run")]
        [SerializeField] private float _attackDuration = 2f;
        [SerializeField] private float _retreatDuration = 3f;
        
        [Header("Pincer")]
        [SerializeField] private float _flankAngle = 45f;
        [SerializeField] private float _flankDistance = 20f;
        
        #endregion

        #region Private Fields
        
        private float _tacticStartTime;
        private float _lastTacticEndTime;
        private bool _isTacticActive;
        private int _currentWave;
        private List<FlyingEnemyBase> _groupA = new List<FlyingEnemyBase>();
        private List<FlyingEnemyBase> _groupB = new List<FlyingEnemyBase>();
        
        #endregion

        #region Properties
        
        public SwarmTactic CurrentTactic => _currentTactic;
        public bool IsTacticActive => _isTacticActive;
        public float TacticProgress => _isTacticActive ? (Time.time - _tacticStartTime) / _tacticExecutionTime : 0f;
        
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
            if (_isTacticActive)
            {
                UpdateTactic();
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Executes a specific tactic.
        /// </summary>
        public void ExecuteTactic(SwarmTactic tactic)
        {
            if (_isTacticActive)
            {
                DebugLogger.LogWarning("Tactic already in progress", LogCategory.Combat);
                return;
            }
            
            if (Time.time - _lastTacticEndTime < _tacticCooldown)
            {
                DebugLogger.LogWarning("Tactic on cooldown", LogCategory.Combat);
                return;
            }
            
            _currentTactic = tactic;
            _tacticStartTime = Time.time;
            _isTacticActive = true;
            
            InitializeTactic(tactic);
            
            DebugLogger.Log($"Executing tactic: {tactic}", LogCategory.Combat);
        }
        
        /// <summary>
        /// Cancels the current tactic.
        /// </summary>
        public void CancelTactic()
        {
            if (!_isTacticActive) return;
            
            _isTacticActive = false;
            _lastTacticEndTime = Time.time;
            
            // Reset all units to normal behavior
            if (SwarmController.Instance != null)
            {
                SwarmController.Instance.OrderRetreat();
            }
            
            DebugLogger.Log("Tactic cancelled", LogCategory.Combat);
        }
        
        /// <summary>
        /// Gets recommended tactic based on situation.
        /// </summary>
        public SwarmTactic GetRecommendedTactic(Transform target, int swarmSize)
        {
            if (swarmSize < 3) return SwarmTactic.FreeAttack;
            if (swarmSize >= 10) return SwarmTactic.SurroundAndEngage;
            if (swarmSize >= 6) return SwarmTactic.PincerMovement;
            if (swarmSize >= 4) return SwarmTactic.WaveAssault;
            
            return SwarmTactic.CoordinatedStrike;
        }
        
        #endregion

        #region Tactic Initialization
        
        private void InitializeTactic(SwarmTactic tactic)
        {
            var swarm = GetSwarmMembers();
            
            switch (tactic)
            {
                case SwarmTactic.CoordinatedStrike:
                    InitializeCoordinatedStrike(swarm);
                    break;
                case SwarmTactic.WaveAssault:
                    InitializeWaveAssault(swarm);
                    break;
                case SwarmTactic.SurroundAndEngage:
                    InitializeSurround(swarm);
                    break;
                case SwarmTactic.HitAndRun:
                    InitializeHitAndRun(swarm);
                    break;
                case SwarmTactic.PincerMovement:
                    InitializePincer(swarm);
                    break;
                case SwarmTactic.DefensivePerimeter:
                    InitializeDefensive(swarm);
                    break;
                case SwarmTactic.DistractionTactic:
                    InitializeDistraction(swarm);
                    break;
            }
        }
        
        private void InitializeCoordinatedStrike(List<FlyingEnemyBase> swarm)
        {
            // All units prepare to attack simultaneously
            foreach (var enemy in swarm)
            {
                // Move to attack positions but don't attack yet
                var sm = enemy.StateMachine as FlyingAIStateMachine;
                if (sm != null)
                {
                    var idleState = new IdleState(enemy, sm);
                    sm.ChangeState(idleState);
                }
            }
        }
        
        private void InitializeWaveAssault(List<FlyingEnemyBase> swarm)
        {
            _currentWave = 0;
            // First wave attacks immediately
            int waveSize = Mathf.CeilToInt(swarm.Count / (float)_waveCount);
            
            for (int i = 0; i < waveSize && i < swarm.Count; i++)
            {
                swarm[i].SetTarget(SwarmController.Instance?.PrimaryTarget);
            }
        }
        
        private void InitializeSurround(List<FlyingEnemyBase> swarm)
        {
            var target = SwarmController.Instance?.PrimaryTarget;
            if (target == null) return;
            
            // Distribute units in a circle around target
            for (int i = 0; i < swarm.Count; i++)
            {
                float angle = (i / (float)swarm.Count) * Mathf.PI * 2f;
                Vector3 surroundPos = target.position + new Vector3(
                    Mathf.Cos(angle) * _surroundRadius,
                    0f,
                    Mathf.Sin(angle) * _surroundRadius
                );
                
                // Create temporary formation position
                swarm[i].SetTarget(null); // Clear target, move to position
            }
        }
        
        private void InitializeHitAndRun(List<FlyingEnemyBase> swarm)
        {
            // Start in attack phase
            foreach (var enemy in swarm)
            {
                enemy.SetTarget(SwarmController.Instance?.PrimaryTarget);
            }
        }
        
        private void InitializePincer(List<FlyingEnemyBase> swarm)
        {
            // Split into two groups
            _groupA.Clear();
            _groupB.Clear();
            
            for (int i = 0; i < swarm.Count; i++)
            {
                if (i % 2 == 0)
                    _groupA.Add(swarm[i]);
                else
                    _groupB.Add(swarm[i]);
            }
            
            // Move groups to flanking positions
            var target = SwarmController.Instance?.PrimaryTarget;
            if (target == null) return;
            
            Vector3 toTarget = target.position - GetSwarmCenter();
            Vector3 leftFlank = Quaternion.Euler(0, -_flankAngle, 0) * toTarget.normalized;
            Vector3 rightFlank = Quaternion.Euler(0, _flankAngle, 0) * toTarget.normalized;
            
            // Groups move to flank positions - actual movement handled in update
        }
        
        private void InitializeDefensive(List<FlyingEnemyBase> swarm)
        {
            // Create defensive circle formation
            if (FormationManager.Instance != null && swarm.Count > 0)
            {
                // Use circle formation for defensive perimeter
                var formation = FormationManager.Instance.CreateCircleFormation(
                    GetSwarmCenter(), 
                    Vector3.forward,
                    swarm.Count,
                    8f // radius
                );
                if (formation != null)
                {
                    FormationManager.Instance.AssignEnemiesToFormation(formation, new List<FlyingEnemyBase>(swarm));
                }
            }
        }
        
        private void InitializeDistraction(List<FlyingEnemyBase> swarm)
        {
            // First third distracts, rest attacks
            int distractors = Mathf.CeilToInt(swarm.Count / 3f);
            
            for (int i = 0; i < swarm.Count; i++)
            {
                if (i < distractors)
                {
                    // Distractor - fly erratically near target
                    var sm = swarm[i].StateMachine as FlyingAIStateMachine;
                    if (sm != null)
                    {
                        sm.ChangeState(new AdvancedEvadeState(swarm[i], sm, 
                            AdvancedEvadeState.EvasionPattern.Scissors));
                    }
                }
                else
                {
                    // Attacker
                    swarm[i].SetTarget(SwarmController.Instance?.PrimaryTarget);
                }
            }
        }
        
        #endregion

        #region Tactic Update
        
        private void UpdateTactic()
        {
            float elapsedTime = Time.time - _tacticStartTime;
            
            if (elapsedTime >= _tacticExecutionTime)
            {
                CompleteTactic();
                return;
            }
            
            switch (_currentTactic)
            {
                case SwarmTactic.CoordinatedStrike:
                    UpdateCoordinatedStrike(elapsedTime);
                    break;
                case SwarmTactic.WaveAssault:
                    UpdateWaveAssault(elapsedTime);
                    break;
                case SwarmTactic.SurroundAndEngage:
                    UpdateSurround(elapsedTime);
                    break;
                case SwarmTactic.HitAndRun:
                    UpdateHitAndRun(elapsedTime);
                    break;
                case SwarmTactic.PincerMovement:
                    UpdatePincer(elapsedTime);
                    break;
            }
        }
        
        private void UpdateCoordinatedStrike(float elapsed)
        {
            if (elapsed >= _strikeChargeUpTime && elapsed < _strikeChargeUpTime + _strikeWindow)
            {
                // Strike window - all attack!
                foreach (var enemy in GetSwarmMembers())
                {
                    enemy.SetTarget(SwarmController.Instance?.PrimaryTarget);
                }
            }
        }
        
        private void UpdateWaveAssault(float elapsed)
        {
            int targetWave = Mathf.FloorToInt(elapsed / _timeBetweenWaves);
            
            if (targetWave > _currentWave && targetWave < _waveCount)
            {
                _currentWave = targetWave;
                
                var swarm = GetSwarmMembers();
                int waveSize = Mathf.CeilToInt(swarm.Count / (float)_waveCount);
                int startIndex = _currentWave * waveSize;
                int endIndex = Mathf.Min(startIndex + waveSize, swarm.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    swarm[i].SetTarget(SwarmController.Instance?.PrimaryTarget);
                }
                
                DebugLogger.Log($"Wave {_currentWave + 1} attacking", LogCategory.Combat);
            }
        }
        
        private void UpdateSurround(float elapsed)
        {
            if (elapsed >= _engageDelay)
            {
                // All engage
                foreach (var enemy in GetSwarmMembers())
                {
                    enemy.SetTarget(SwarmController.Instance?.PrimaryTarget);
                }
            }
        }
        
        private void UpdateHitAndRun(float elapsed)
        {
            float cycleTime = _attackDuration + _retreatDuration;
            float cycleProgress = elapsed % cycleTime;
            
            bool isAttackPhase = cycleProgress < _attackDuration;
            
            foreach (var enemy in GetSwarmMembers())
            {
                if (isAttackPhase)
                {
                    enemy.SetTarget(SwarmController.Instance?.PrimaryTarget);
                }
                else
                {
                    enemy.SetTarget(null); // Retreat
                }
            }
        }
        
        private void UpdatePincer(float elapsed)
        {
            // Both groups attack from flanks after positioning
            if (elapsed >= 3f) // Give time to position
            {
                var target = SwarmController.Instance?.PrimaryTarget;
                foreach (var enemy in _groupA)
                {
                    enemy.SetTarget(target);
                }
                foreach (var enemy in _groupB)
                {
                    enemy.SetTarget(target);
                }
            }
        }
        
        private void CompleteTactic()
        {
            _isTacticActive = false;
            _lastTacticEndTime = Time.time;
            
            DebugLogger.Log($"Tactic {_currentTactic} completed", LogCategory.Combat);
        }
        
        #endregion

        #region Helpers
        
        private List<FlyingEnemyBase> GetSwarmMembers()
        {
            var members = new List<FlyingEnemyBase>();
            if (EnemyPrioritySystem.Instance != null)
            {
                members.AddRange(EnemyPrioritySystem.Instance.GetSortedByPriority());
            }
            return members;
        }
        
        private Vector3 GetSwarmCenter()
        {
            return SwarmController.Instance?.GetSwarmCenter() ?? Vector3.zero;
        }
        
        #endregion
    }
}
