using UnityEngine;
using FlyingAI.ScriptableObjects;

namespace FlyingAI.Formations
{
    /// <summary>
    /// V-shaped formation like flying geese.
    /// Leader at front, wings spreading back.
    /// </summary>
    public class VFormation : FormationBase
    {
        public VFormation(FormationConfig config) : base(config) { }
        
        protected override void CalculateSlotOffsets()
        {
            _slotOffsets.Clear();
            
            float angle = _config?.VAngle ?? 45f;
            float spacing = _config?.HorizontalSpacing ?? 3f;
            float depthOffset = _config?.VDepthOffset ?? 2f;
            
            float angleRad = angle * Mathf.Deg2Rad;
            
            // Leader position (slot 0)
            _slotOffsets.Add(Vector3.zero);
            
            int maxPerSide = 5;
            
            // Alternate left and right
            for (int i = 1; i <= maxPerSide; i++)
            {
                float lateralOffset = i * spacing;
                float backOffset = i * depthOffset;
                
                // Right wing
                _slotOffsets.Add(new Vector3(
                    lateralOffset * Mathf.Sin(angleRad),
                    0f,
                    -backOffset
                ));
                
                // Left wing
                _slotOffsets.Add(new Vector3(
                    -lateralOffset * Mathf.Sin(angleRad),
                    0f,
                    -backOffset
                ));
            }
        }
    }

    /// <summary>
    /// Circular formation surrounding a center point.
    /// Good for surrounding attacks.
    /// </summary>
    public class CircleFormation : FormationBase
    {
        private int _maxSlotCount = 20;
        private float _radius = 10f;
        
        public CircleFormation(FormationConfig config) : base(config) 
        { 
            _radius = config?.CircleRadius ?? 10f;
            _maxSlotCount = config?.GetMaxSlotCount() ?? 20;
        }
        
        /// <summary>
        /// Creates a circle formation with specified parameters.
        /// </summary>
        public CircleFormation(int slotCount, float radius) : base(null)
        {
            _maxSlotCount = slotCount;
            _radius = radius;
            CalculateSlotOffsets();
        }
        
        protected override void CalculateSlotOffsets()
        {
            _slotOffsets.Clear();
            
            for (int i = 0; i < _maxSlotCount; i++)
            {
                float angle = (2f * Mathf.PI * i) / _maxSlotCount;
                _slotOffsets.Add(new Vector3(
                    Mathf.Cos(angle) * _radius,
                    0f,
                    Mathf.Sin(angle) * _radius
                ));
            }
        }
    }

    /// <summary>
    /// Grid/block formation for organized assault.
    /// </summary>
    public class GridFormation : FormationBase
    {
        public GridFormation(FormationConfig config) : base(config) { }
        
        protected override void CalculateSlotOffsets()
        {
            _slotOffsets.Clear();
            
            int columns = _config?.GridColumns ?? 5;
            int rows = _config?.GridRows ?? 4;
            float hSpacing = _config?.HorizontalSpacing ?? 3f;
            float dSpacing = _config?.DepthSpacing ?? 3f;
            
            float startX = -((columns - 1) * hSpacing) / 2f;
            float startZ = 0f;
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    _slotOffsets.Add(new Vector3(
                        startX + col * hSpacing,
                        0f,
                        startZ - row * dSpacing
                    ));
                }
            }
        }
    }

    /// <summary>
    /// Single-file line formation.
    /// Good for pursuit or narrow passages.
    /// </summary>
    public class LineFormation : FormationBase
    {
        public LineFormation(FormationConfig config) : base(config) { }
        
        protected override void CalculateSlotOffsets()
        {
            _slotOffsets.Clear();
            
            float spacing = _config?.DepthSpacing ?? 3f;
            int maxSlots = 10;
            
            for (int i = 0; i < maxSlots; i++)
            {
                _slotOffsets.Add(new Vector3(0f, 0f, -i * spacing));
            }
        }
    }

    /// <summary>
    /// Diamond formation for defensive positioning.
    /// </summary>
    public class DiamondFormation : FormationBase
    {
        public DiamondFormation(FormationConfig config) : base(config) { }
        
        protected override void CalculateSlotOffsets()
        {
            _slotOffsets.Clear();
            
            float width = _config?.DiamondWidth ?? 10f;
            float depth = _config?.DiamondDepth ?? 15f;
            
            // Front point
            _slotOffsets.Add(new Vector3(0f, 0f, depth / 2f));
            
            // Left point
            _slotOffsets.Add(new Vector3(-width / 2f, 0f, 0f));
            
            // Right point
            _slotOffsets.Add(new Vector3(width / 2f, 0f, 0f));
            
            // Back point
            _slotOffsets.Add(new Vector3(0f, 0f, -depth / 2f));
            
            // Center
            _slotOffsets.Add(Vector3.zero);
            
            // Additional slots between main points
            _slotOffsets.Add(new Vector3(-width / 4f, 0f, depth / 4f));
            _slotOffsets.Add(new Vector3(width / 4f, 0f, depth / 4f));
            _slotOffsets.Add(new Vector3(-width / 4f, 0f, -depth / 4f));
            _slotOffsets.Add(new Vector3(width / 4f, 0f, -depth / 4f));
        }
    }

    /// <summary>
    /// Custom formation using predefined slot positions.
    /// </summary>
    public class CustomFormation : FormationBase
    {
        public CustomFormation(FormationConfig config) : base(config) { }
        
        protected override void CalculateSlotOffsets()
        {
            _slotOffsets.Clear();
            
            if (_config?.CustomSlotPositions != null)
            {
                foreach (var pos in _config.CustomSlotPositions)
                {
                    _slotOffsets.Add(pos);
                }
            }
        }
    }
}
