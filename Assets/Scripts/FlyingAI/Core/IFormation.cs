using UnityEngine;

namespace FlyingAI.Core
{
    /// <summary>
    /// Interface for formation pattern calculations.
    /// Defines methods for slot position management.
    /// </summary>
    public interface IFormation
    {
        #region Properties
        
        /// <summary>
        /// Maximum number of slots in this formation.
        /// </summary>
        int MaxSlotCount { get; }
        
        /// <summary>
        /// Current number of active slots.
        /// </summary>
        int ActiveSlotCount { get; }
        
        /// <summary>
        /// Formation center position in world space.
        /// </summary>
        Vector3 CenterPosition { get; set; }
        
        /// <summary>
        /// Formation forward direction.
        /// </summary>
        Vector3 ForwardDirection { get; set; }
        
        #endregion

        #region Slot Management
        
        /// <summary>
        /// Gets the world position for a specific slot index.
        /// </summary>
        /// <param name="slotIndex">Slot index (0-based).</param>
        /// <returns>World position of the slot.</returns>
        Vector3 GetSlotPosition(int slotIndex);
        
        /// <summary>
        /// Gets the local offset for a slot relative to formation center.
        /// </summary>
        /// <param name="slotIndex">Slot index (0-based).</param>
        /// <returns>Local offset vector.</returns>
        Vector3 GetSlotOffset(int slotIndex);
        
        /// <summary>
        /// Assigns an entity to a slot.
        /// </summary>
        /// <param name="slotIndex">Slot to assign.</param>
        /// <param name="entity">Entity to assign.</param>
        /// <returns>True if assignment successful.</returns>
        bool AssignSlot(int slotIndex, FlyingEnemyBase entity);
        
        /// <summary>
        /// Removes an entity from its assigned slot.
        /// </summary>
        /// <param name="entity">Entity to remove.</param>
        void RemoveFromSlot(FlyingEnemyBase entity);
        
        /// <summary>
        /// Gets the next available empty slot.
        /// </summary>
        /// <returns>Slot index or -1 if no slots available.</returns>
        int GetNextAvailableSlot();
        
        /// <summary>
        /// Checks if a slot is occupied.
        /// </summary>
        /// <param name="slotIndex">Slot to check.</param>
        /// <returns>True if occupied.</returns>
        bool IsSlotOccupied(int slotIndex);
        
        #endregion

        #region Formation Updates
        
        /// <summary>
        /// Recalculates all slot positions based on current settings.
        /// </summary>
        void Recalculate();
        
        /// <summary>
        /// Recalculates positions for a specific number of active units.
        /// </summary>
        /// <param name="activeCount">Number of active units.</param>
        void Recalculate(int activeCount);
        
        /// <summary>
        /// Called when a unit is removed, handles slot reassignment.
        /// </summary>
        void OnUnitRemoved(int slotIndex);
        
        /// <summary>
        /// Sets the formation scale factor (for dynamic resizing).
        /// </summary>
        /// <param name="scale">Scale factor (0.5 - 1.0).</param>
        void SetScale(float scale);
        
        /// <summary>
        /// Initializes the formation with position and orientation.
        /// </summary>
        void Initialize(Vector3 position, Vector3 forward);
        
        /// <summary>
        /// Adds a unit to the next available slot.
        /// </summary>
        bool AddUnit(FlyingEnemyBase unit);
        
        /// <summary>
        /// Disbands the formation, releasing all units.
        /// </summary>
        void Disband();

        #endregion
    }
}
