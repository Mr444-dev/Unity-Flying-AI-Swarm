using UnityEngine;
using System;
using FlyingAI.Core;
using FlyingAI.Utils;

namespace FlyingAI.States
{
    /// <summary>
    /// Concrete state machine implementation for flying AI.
    /// </summary>
    public class FlyingAIStateMachine : IStateMachine
    {
        #region Private Fields
        
        private IState _currentState;
        private IState _previousState;
        private float _timeInState;
        private FlyingEnemyBase _owner;
        
        #endregion

        #region Properties
        
        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;
        public float TimeInCurrentState => _timeInState;
        public FlyingEnemyBase Owner => _owner;
        
        #endregion

        #region Events
        
        public event Action<IState, IState> OnStateChanged;
        
        #endregion

        #region Constructor
        
        public FlyingAIStateMachine(FlyingEnemyBase owner)
        {
            _owner = owner;
        }
        
        #endregion

        #region Public Methods
        
        public void ChangeState(IState newState)
        {
            if (newState == null)
            {
                DebugLogger.LogWarning("Attempted to change to null state", LogCategory.StateMachine);
                return;
            }
            
            if (_currentState == newState) return;
            
            _previousState = _currentState;
            _currentState?.Exit();
            
            _currentState = newState;
            _timeInState = 0f;
            
            DebugLogger.Log($"State changed: {_previousState?.GetType().Name ?? "null"} -> {_currentState.GetType().Name}", 
                LogCategory.StateMachine);
            
            _currentState.Enter();
            OnStateChanged?.Invoke(_previousState, _currentState);
        }
        
        public void RevertToPreviousState()
        {
            if (_previousState != null)
            {
                ChangeState(_previousState);
            }
        }
        
        public void Update()
        {
            _currentState?.Execute();
            _timeInState += Time.deltaTime;
        }
        
        public void FixedUpdate()
        {
            _currentState?.FixedExecute();
        }
        
        #endregion
    }

    /// <summary>
    /// Abstract base state for all flying AI states.
    /// </summary>
    public abstract class FlyingAIStateBase : IState
    {
        #region Protected Fields
        
        protected FlyingEnemyBase _owner;
        protected FlyingAIStateMachine _stateMachine;
        
        #endregion

        #region Properties
        
        public FlyingEnemyBase Owner => _owner;
        public FlyingAIStateMachine StateMachine => _stateMachine;
        
        #endregion

        #region Constructor
        
        public FlyingAIStateBase(FlyingEnemyBase owner, FlyingAIStateMachine stateMachine)
        {
            _owner = owner;
            _stateMachine = stateMachine;
        }
        
        #endregion

        #region Virtual Methods
        
        public virtual void Enter()
        {
            DebugLogger.LogVerbose($"{GetType().Name} Enter", LogCategory.StateMachine);
        }
        
        public virtual void Execute() { }
        
        public virtual void FixedExecute() { }
        
        public virtual void Exit()
        {
            DebugLogger.LogVerbose($"{GetType().Name} Exit", LogCategory.StateMachine);
        }
        
        #endregion

        #region Protected Methods
        
        protected bool HasTarget() => _owner.CurrentTarget != null;
        
        protected float DistanceToTarget()
        {
            if (!HasTarget()) return float.MaxValue;
            return Vector3.Distance(_owner.Position, _owner.CurrentTarget.position);
        }
        
        protected Vector3 DirectionToTarget()
        {
            if (!HasTarget()) return _owner.Forward;
            return (_owner.CurrentTarget.position - _owner.Position).normalized;
        }
        
        #endregion
    }
}
