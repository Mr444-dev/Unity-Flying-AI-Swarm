using UnityEngine;

namespace FlyingAI.ScriptableObjects
{
    /// <summary>
    /// Available formation types for enemy groups.
    /// </summary>
    public enum FormationType
    {
        V,
        Circle,
        Grid,
        Line,
        Diamond,
        Custom
    }

    /// <summary>
    /// Configuration ScriptableObject for formation patterns.
    /// Defines formation shapes, spacing, and dynamic adjustment parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "FormationConfig", menuName = "FlyingAI/Configs/Formation")]
    public class FormationConfig : ScriptableObject
    {
        #region Formation Type
        
        [Header("Formation Type")]
        [Tooltip("Type of formation pattern")]
        [SerializeField] private FormationType _formationType = FormationType.V;
        
        [Tooltip("Formation identifier")]
        [SerializeField] private string _formationId = "default_formation";
        
        #endregion

        #region Spacing
        
        [Header("Spacing")]
        [Tooltip("Horizontal spacing between slots")]
        [SerializeField] private float _horizontalSpacing = 3f;
        
        [Tooltip("Vertical spacing between slots")]
        [SerializeField] private float _verticalSpacing = 2f;
        
        [Tooltip("Depth spacing between rows")]
        [SerializeField] private float _depthSpacing = 3f;
        
        [Tooltip("Radius for circle formation")]
        [SerializeField] private float _circleRadius = 10f;
        
        #endregion

        #region V Formation
        
        [Header("V Formation Settings")]
        [Tooltip("Angle of V formation arms (degrees)")]
        [Range(15f, 90f)]
        [SerializeField] private float _vAngle = 45f;
        
        [Tooltip("Offset behind leader per row")]
        [SerializeField] private float _vDepthOffset = 2f;
        
        #endregion

        #region Grid Formation
        
        [Header("Grid Formation Settings")]
        [Tooltip("Maximum units per row")]
        [SerializeField] private int _gridColumns = 5;
        
        [Tooltip("Maximum rows")]
        [SerializeField] private int _gridRows = 4;
        
        #endregion

        #region Diamond Formation
        
        [Header("Diamond Formation Settings")]
        [Tooltip("Diamond width at widest point")]
        [SerializeField] private float _diamondWidth = 10f;
        
        [Tooltip("Diamond depth from front to back")]
        [SerializeField] private float _diamondDepth = 15f;
        
        #endregion

        #region Custom Formation
        
        [Header("Custom Formation Settings")]
        [Tooltip("Custom slot positions relative to formation center")]
        [SerializeField] private Vector3[] _customSlotPositions;
        
        #endregion

        #region Dynamic Adjustment
        
        [Header("Dynamic Adjustment")]
        [Tooltip("Speed of slot position interpolation")]
        [SerializeField] private float _repositionSpeed = 5f;
        
        [Tooltip("Time to wait before filling empty slots")]
        [SerializeField] private float _slotReassignmentDelay = 0.5f;
        
        [Tooltip("Allow formation to rotate towards target")]
        [SerializeField] private bool _rotateTowardsTarget = true;
        
        [Tooltip("Formation rotation speed")]
        [SerializeField] private float _formationRotationSpeed = 90f;
        
        [Tooltip("Can formation shrink when units are lost")]
        [SerializeField] private bool _dynamicResize = true;
        
        [Tooltip("Minimum formation scale factor")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _minScaleFactor = 0.7f;
        
        #endregion

        #region Leader Settings
        
        [Header("Leader Settings")]
        [Tooltip("Does this formation have a designated leader slot")]
        [SerializeField] private bool _hasLeader = true;
        
        [Tooltip("Leader slot index (0 = front center)")]
        [SerializeField] private int _leaderSlotIndex;
        
        [Tooltip("Speed bonus for leader")]
        [SerializeField] private float _leaderSpeedBonus = 1.2f;
        
        #endregion

        #region Properties
        
        public FormationType Type => _formationType;
        public string FormationId => _formationId;
        
        public float HorizontalSpacing => _horizontalSpacing;
        public float VerticalSpacing => _verticalSpacing;
        public float DepthSpacing => _depthSpacing;
        public float CircleRadius => _circleRadius;
        
        public float VAngle => _vAngle;
        public float VDepthOffset => _vDepthOffset;
        
        public int GridColumns => _gridColumns;
        public int GridRows => _gridRows;
        
        public float DiamondWidth => _diamondWidth;
        public float DiamondDepth => _diamondDepth;
        
        public Vector3[] CustomSlotPositions => _customSlotPositions;
        
        public float RepositionSpeed => _repositionSpeed;
        public float SlotReassignmentDelay => _slotReassignmentDelay;
        public bool RotateTowardsTarget => _rotateTowardsTarget;
        public float FormationRotationSpeed => _formationRotationSpeed;
        public bool DynamicResize => _dynamicResize;
        public float MinScaleFactor => _minScaleFactor;
        
        public bool HasLeader => _hasLeader;
        public int LeaderSlotIndex => _leaderSlotIndex;
        public float LeaderSpeedBonus => _leaderSpeedBonus;
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Gets the maximum number of slots this formation can support.
        /// </summary>
        public int GetMaxSlotCount()
        {
            return _formationType switch
            {
                FormationType.Grid => _gridColumns * _gridRows,
                FormationType.Custom => _customSlotPositions?.Length ?? 0,
                FormationType.Circle => 20,
                FormationType.Diamond => 9,
                FormationType.Line => 10,
                FormationType.V => 11,
                _ => 10
            };
        }
        
        #endregion
    }
}
