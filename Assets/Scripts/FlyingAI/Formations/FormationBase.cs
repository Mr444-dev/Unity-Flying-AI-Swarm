using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;

namespace FlyingAI.Formations
{
    /// <summary>
    /// Abstract base class for all formation types.
    /// Provides common functionality and virtual methods for extensibility.
    /// </summary>
    public abstract class FormationBase : IFormation
    {
        #region Protected Fields
        
        protected FormationConfig _config;
        protected Vector3 _centerPosition;
        protected Vector3 _forwardDirection = Vector3.forward;
        protected float _scaleFactor = 1f;
        protected List<Vector3> _slotOffsets = new List<Vector3>();
        protected Dictionary<int, FlyingEnemyBase> _slotAssignments = new Dictionary<int, FlyingEnemyBase>();
        protected int _activeSlotCount;
        
        #endregion

        #region Properties
        
        public virtual int MaxSlotCount => _slotOffsets.Count;
        public virtual int ActiveSlotCount => _activeSlotCount;
        public Vector3 CenterPosition { get => _centerPosition; set => _centerPosition = value; }
        public Vector3 ForwardDirection { get => _forwardDirection; set => _forwardDirection = value.normalized; }
        public FormationConfig Config => _config;
        
        #endregion

        #region Constructor
        
        public FormationBase(FormationConfig config)
        {
            _config = config;
            CalculateSlotOffsets();
        }
        
        #endregion

        #region Abstract Methods
        
        /// <summary>
        /// Calculates all slot offsets for this formation type.
        /// Must be implemented by derived classes.
        /// </summary>
        protected abstract void CalculateSlotOffsets();
        
        #endregion

        #region IFormation Implementation
        
        public virtual Vector3 GetSlotPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotOffsets.Count)
            {
                return _centerPosition;
            }
            
            Vector3 offset = _slotOffsets[slotIndex] * _scaleFactor;
            Quaternion rotation = Quaternion.LookRotation(_forwardDirection, Vector3.up);
            return _centerPosition + rotation * offset;
        }
        
        public virtual Vector3 GetSlotOffset(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotOffsets.Count)
            {
                return Vector3.zero;
            }
            return _slotOffsets[slotIndex] * _scaleFactor;
        }
        
        public virtual bool AssignSlot(int slotIndex, FlyingEnemyBase entity)
        {
            if (slotIndex < 0 || slotIndex >= _slotOffsets.Count)
            {
                DebugLogger.LogWarning($"Invalid slot index: {slotIndex}", LogCategory.Formation);
                return false;
            }
            
            if (_slotAssignments.ContainsKey(slotIndex))
            {
                DebugLogger.LogWarning($"Slot {slotIndex} already occupied", LogCategory.Formation);
                return false;
            }
            
            _slotAssignments[slotIndex] = entity;
            _activeSlotCount = _slotAssignments.Count;
            
            DebugLogger.LogVerbose($"Entity assigned to slot {slotIndex}", LogCategory.Formation);
            return true;
        }
        
        public virtual void RemoveFromSlot(FlyingEnemyBase entity)
        {
            int slotToRemove = -1;
            foreach (var kvp in _slotAssignments)
            {
                if (kvp.Value == entity)
                {
                    slotToRemove = kvp.Key;
                    break;
                }
            }
            
            if (slotToRemove >= 0)
            {
                _slotAssignments.Remove(slotToRemove);
                _activeSlotCount = _slotAssignments.Count;
                DebugLogger.LogVerbose($"Entity removed from slot {slotToRemove}", LogCategory.Formation);
            }
        }
        
        public virtual int GetNextAvailableSlot()
        {
            for (int i = 0; i < _slotOffsets.Count; i++)
            {
                if (!_slotAssignments.ContainsKey(i))
                {
                    return i;
                }
            }
            return -1;
        }
        
        public virtual bool IsSlotOccupied(int slotIndex)
        {
            return _slotAssignments.ContainsKey(slotIndex);
        }
        
        public virtual void Recalculate()
        {
            CalculateSlotOffsets();
        }
        
        public virtual void Recalculate(int activeCount)
        {
            _activeSlotCount = activeCount;
            CalculateSlotOffsets();
            
            if (_config != null && _config.DynamicResize)
            {
                float fillRatio = (float)activeCount / MaxSlotCount;
                _scaleFactor = Mathf.Lerp(_config.MinScaleFactor, 1f, fillRatio);
            }
        }
        
        public virtual void OnUnitRemoved(int slotIndex)
        {
            _slotAssignments.Remove(slotIndex);
            _activeSlotCount = _slotAssignments.Count;
            
            if (_config != null && _config.SlotReassignmentDelay <= 0)
            {
                ReassignSlots();
            }
        }
        
        public virtual void Initialize(Vector3 position, Vector3 forward)
        {
            _centerPosition = position;
            _forwardDirection = forward;
            Recalculate();
        }

        public virtual bool AddUnit(FlyingEnemyBase unit)
        {
            int slot = GetNextAvailableSlot();
            if (slot >= 0)
            {
                if (AssignSlot(slot, unit))
                {
                    unit.AssignToFormation(this, slot);
                    return true;
                }
            }
            return false;
        }

        public virtual void Disband()
        {
            foreach (var enemy in _slotAssignments.Values)
            {
                if (enemy != null)
                {
                    enemy.AssignToFormation(null, -1);
                }
            }
            _slotAssignments.Clear();
            _activeSlotCount = 0;
        }

        public virtual void SetScale(float scale)
        {
            _scaleFactor = Mathf.Clamp(scale, 0.5f, 2f);
        }
        
        #endregion

        #region Protected Methods
        
        /// <summary>
        /// Reassigns all entities to fill gaps in the formation.
        /// </summary>
        protected virtual void ReassignSlots()
        {
            var entities = new List<FlyingEnemyBase>(_slotAssignments.Values);
            _slotAssignments.Clear();
            
            for (int i = 0; i < entities.Count && i < _slotOffsets.Count; i++)
            {
                _slotAssignments[i] = entities[i];
                entities[i].AssignToFormation(this, i);
            }
            
            _activeSlotCount = _slotAssignments.Count;
        }
        
        #endregion
    }
}
