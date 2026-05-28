using UnityEngine;

namespace FlyingAI.Core
{
    /// <summary>
    /// Priority level for target importance.
    /// </summary>
    public enum PriorityLevel
    {
        Low = 1,
        Normal = 5,
        High = 7,
        Critical = 10
    }

    /// <summary>
    /// Interface for tracking enemy importance and priority.
    /// Enables the player and systems to identify key targets.
    /// </summary>
    public interface IPriorityTarget
    {
        #region Properties
        
        /// <summary>
        /// Unique identifier for this target.
        /// </summary>
        int TargetId { get; }
        
        /// <summary>
        /// Current priority value (higher = more important).
        /// </summary>
        int CurrentPriority { get; }
        
        /// <summary>
        /// Base priority before modifiers.
        /// </summary>
        int BasePriority { get; }
        
        /// <summary>
        /// Priority level category.
        /// </summary>
        PriorityLevel Level { get; }
        
        /// <summary>
        /// Whether this target is marked as elite/leader.
        /// </summary>
        bool IsElite { get; }
        
        /// <summary>
        /// Position for targeting purposes.
        /// </summary>
        Vector3 Position { get; }
        
        /// <summary>
        /// Whether the target is valid (alive and active).
        /// </summary>
        bool IsValidTarget { get; }
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Calculates and updates the current priority value.
        /// </summary>
        /// <returns>Updated priority value.</returns>
        int CalculatePriority();
        
        /// <summary>
        /// Adds a temporary priority modifier.
        /// </summary>
        /// <param name="modifier">Priority modifier value.</param>
        /// <param name="duration">Duration in seconds (0 = permanent until removed).</param>
        /// <param name="modifierId">Identifier for this modifier.</param>
        void AddPriorityModifier(int modifier, float duration, string modifierId);
        
        /// <summary>
        /// Removes a priority modifier by ID.
        /// </summary>
        /// <param name="modifierId">Modifier to remove.</param>
        void RemovePriorityModifier(string modifierId);
        
        /// <summary>
        /// Clears all priority modifiers.
        /// </summary>
        void ClearPriorityModifiers();
        
        #endregion
    }
}
