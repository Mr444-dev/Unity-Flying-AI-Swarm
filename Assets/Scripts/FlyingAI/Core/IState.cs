namespace FlyingAI.Core
{
    /// <summary>
    /// Base interface for state machine states.
    /// Defines lifecycle methods for state management.
    /// </summary>
    public interface IState
    {
        #region Lifecycle Methods
        
        /// <summary>
        /// Called when entering this state.
        /// </summary>
        void Enter();
        
        /// <summary>
        /// Called every frame while in this state.
        /// </summary>
        void Execute();
        
        /// <summary>
        /// Called at fixed time intervals while in this state.
        /// </summary>
        void FixedExecute();
        
        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        void Exit();
        
        #endregion
    }
}
