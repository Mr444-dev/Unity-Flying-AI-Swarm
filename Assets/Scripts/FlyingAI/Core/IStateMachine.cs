using System;

namespace FlyingAI.Core
{
    /// <summary>
    /// Interface for state machine management.
    /// Handles state transitions and current state execution.
    /// </summary>
    public interface IStateMachine
    {
        #region Properties
        
        /// <summary>
        /// The currently active state.
        /// </summary>
        IState CurrentState { get; }
        
        /// <summary>
        /// The previous state before the last transition.
        /// </summary>
        IState PreviousState { get; }
        
        /// <summary>
        /// Time spent in current state.
        /// </summary>
        float TimeInCurrentState { get; }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when state changes.
        /// </summary>
        event Action<IState, IState> OnStateChanged;
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Changes to a new state.
        /// </summary>
        void ChangeState(IState newState);
        
        /// <summary>
        /// Reverts to the previous state.
        /// </summary>
        void RevertToPreviousState();
        
        /// <summary>
        /// Updates the current state.
        /// </summary>
        void Update();
        
        /// <summary>
        /// Fixed update for physics-related state logic.
        /// </summary>
        void FixedUpdate();
        
        #endregion
    }
}
