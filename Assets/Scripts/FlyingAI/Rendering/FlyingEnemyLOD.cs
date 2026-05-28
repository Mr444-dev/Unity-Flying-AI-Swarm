using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Utils;

namespace FlyingAI.Rendering
{
    /// <summary>
    /// LOD level configuration.
    /// </summary>
    [System.Serializable]
    public class LODLevel
    {
        public float Distance;
        public Mesh Mesh;
        public Material Material;
        public bool EnableAnimations;
        public bool EnableParticles;
        public float UpdateRate; // Updates per second
    }

    /// <summary>
    /// LOD system for flying enemies to optimize rendering.
    /// </summary>
    public class FlyingEnemyLOD : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("LOD Configuration")]
        [SerializeField] private LODLevel[] _lodLevels;
        [SerializeField] private float _lodUpdateInterval = 0.5f;
        
        [Header("References")]
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private ParticleSystem[] _particles;
        [SerializeField] private Animator _animator;
        
        [Header("Culling")]
        [SerializeField] private float _cullDistance = 200f;
        [SerializeField] private bool _useFrustumCulling = true;
        
        #endregion

        #region Private Fields
        
        private int _currentLOD = -1;
        private float _lastLODUpdate;
        private Camera _mainCamera;
        private FlyingEnemyBase _enemy;
        private bool _isCulled;
        private float _currentUpdateRate = 60f;
        private float _lastBehaviorUpdate;
        
        #endregion

        #region Properties
        
        public int CurrentLODLevel => _currentLOD;
        public bool IsCulled => _isCulled;
        public float UpdateRate => _currentUpdateRate;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            _enemy = GetComponent<FlyingEnemyBase>();
            _mainCamera = Camera.main;
            
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_animator == null) _animator = GetComponent<Animator>();
        }
        
        private void Update()
        {
            if (Time.time - _lastLODUpdate >= _lodUpdateInterval)
            {
                UpdateLOD();
                _lastLODUpdate = Time.time;
            }
        }
        
        #endregion

        #region LOD Updates
        
        private void UpdateLOD()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }
            
            float distance = Vector3.Distance(transform.position, _mainCamera.transform.position);
            
            // Culling check
            if (distance > _cullDistance)
            {
                SetCulled(true);
                return;
            }
            
            // Frustum culling
            if (_useFrustumCulling)
            {
                Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);
                bool inView = viewportPos.x >= -0.1f && viewportPos.x <= 1.1f &&
                              viewportPos.y >= -0.1f && viewportPos.y <= 1.1f &&
                              viewportPos.z > 0;
                
                if (!inView)
                {
                    SetCulled(true);
                    return;
                }
            }
            
            SetCulled(false);
            
            // Determine LOD level
            int newLOD = 0;
            for (int i = 0; i < _lodLevels.Length; i++)
            {
                if (distance > _lodLevels[i].Distance)
                {
                    newLOD = i;
                }
                else break;
            }
            
            if (newLOD != _currentLOD)
            {
                ApplyLOD(newLOD);
            }
        }
        
        private void ApplyLOD(int lodIndex)
        {
            if (lodIndex < 0 || lodIndex >= _lodLevels.Length) return;
            
            _currentLOD = lodIndex;
            LODLevel level = _lodLevels[lodIndex];
            
            // Apply mesh
            if (_meshFilter != null && level.Mesh != null)
            {
                _meshFilter.sharedMesh = level.Mesh;
            }
            
            // Apply material
            if (_meshRenderer != null && level.Material != null)
            {
                _meshRenderer.sharedMaterial = level.Material;
            }
            
            // Toggle animations
            if (_animator != null)
            {
                _animator.enabled = level.EnableAnimations;
            }
            
            // Toggle particles
            if (_particles != null)
            {
                foreach (var ps in _particles)
                {
                    if (ps != null)
                    {
                        var emission = ps.emission;
                        emission.enabled = level.EnableParticles;
                    }
                }
            }
            
            // Update rate for behavior
            _currentUpdateRate = level.UpdateRate;
            
            DebugLogger.LogVerbose($"LOD changed to level {lodIndex}", LogCategory.General);
        }
        
        private void SetCulled(bool culled)
        {
            if (_isCulled == culled) return;
            
            _isCulled = culled;
            
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = !culled;
            }
            
            if (_particles != null)
            {
                foreach (var ps in _particles)
                {
                    if (ps != null)
                    {
                        if (culled) ps.Pause();
                        else ps.Play();
                    }
                }
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Checks if behavior update should run based on LOD rate.
        /// </summary>
        public bool ShouldUpdateBehavior()
        {
            if (_isCulled) return false;
            
            float interval = 1f / _currentUpdateRate;
            if (Time.time - _lastBehaviorUpdate >= interval)
            {
                _lastBehaviorUpdate = Time.time;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Forces immediate LOD update.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateLOD();
        }
        
        #endregion
    }

    /// <summary>
    /// Global LOD manager for batch processing.
    /// </summary>
    public class LODManager : MonoBehaviour
    {
        #region Singleton
        
        private static LODManager _instance;
        public static LODManager Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Global Settings")]
        [SerializeField] private float _globalLODBias = 1f;
        [SerializeField] private int _maxHighDetailEnemies = 20;
        [SerializeField] private float _updateBatchSize = 50;
        
        #endregion

        #region Private Fields
        
        private List<FlyingEnemyLOD> _lodComponents = new List<FlyingEnemyLOD>();
        private int _currentBatchIndex;
        
        #endregion

        #region Properties
        
        public float GlobalLODBias { get => _globalLODBias; set => _globalLODBias = value; }
        
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
        
        private void LateUpdate()
        {
            // Batch update LOD components
            int endIndex = Mathf.Min(
                _currentBatchIndex + (int)_updateBatchSize,
                _lodComponents.Count
            );
            
            for (int i = _currentBatchIndex; i < endIndex; i++)
            {
                if (_lodComponents[i] != null)
                {
                    _lodComponents[i].ForceUpdate();
                }
            }
            
            _currentBatchIndex = endIndex >= _lodComponents.Count ? 0 : endIndex;
        }
        
        #endregion

        #region Registration
        
        public void Register(FlyingEnemyLOD lod)
        {
            if (!_lodComponents.Contains(lod))
            {
                _lodComponents.Add(lod);
            }
        }
        
        public void Unregister(FlyingEnemyLOD lod)
        {
            _lodComponents.Remove(lod);
        }
        
        #endregion
    }
}
