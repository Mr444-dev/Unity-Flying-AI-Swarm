using UnityEngine;
using System;
using System.Collections.Generic;
using FlyingAI.Utils;
using Cysharp.Threading.Tasks;

namespace FlyingAI.Pooling
{
    /// <summary>
    /// Generic object pool base class with pre-warming and auto-expansion.
    /// </summary>
    /// <typeparam name="T">Component type to pool.</typeparam>
    public abstract class ObjectPoolBase<T> : MonoBehaviour where T : Component
    {
        #region Serialized Fields
        
        [Header("Pool Settings")]
        [SerializeField] protected T _prefab;
        [SerializeField] protected int _initialSize = 20;
        [SerializeField] protected int _maxSize = 100;
        [SerializeField] protected bool _autoExpand = true;
        [SerializeField] protected int _expandAmount = 10;
        
        #endregion

        #region Private Fields
        
        protected Queue<T> _available = new Queue<T>();
        protected HashSet<T> _inUse = new HashSet<T>();
        protected Transform _poolContainer;
        
        #endregion

        #region Properties
        
        public int AvailableCount => _available.Count;
        public int InUseCount => _inUse.Count;
        public int TotalCount => AvailableCount + InUseCount;
        
        #endregion

        #region Events
        
        public event Action<T> OnObjectSpawned;
        public event Action<T> OnObjectDespawned;
        
        #endregion

        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            _poolContainer = new GameObject($"{typeof(T).Name}_Pool").transform;
            _poolContainer.SetParent(transform);
        }
        
        protected virtual void Start()
        {
            PrewarmPool();
        }
        
        #endregion

        #region Pool Management
        
        protected virtual void PrewarmPool()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                T obj = CreateNewObject();
                _available.Enqueue(obj);
            }
            
            DebugLogger.Log($"{GetType().Name} prewarmed with {_initialSize} objects", LogCategory.Pooling, this);
        }
        
        protected virtual T CreateNewObject()
        {
            T obj = Instantiate(_prefab, _poolContainer);
            obj.gameObject.SetActive(false);
            OnObjectCreated(obj);
            return obj;
        }
        
        /// <summary>
        /// Called when a new object is created. Override for custom initialization.
        /// </summary>
        protected virtual void OnObjectCreated(T obj) { }
        
        /// <summary>
        /// Called before an object is returned from the pool.
        /// </summary>
        protected virtual void OnBeforeSpawn(T obj) { }
        
        /// <summary>
        /// Called when an object is returned to the pool.
        /// </summary>
        protected virtual void OnDespawn(T obj) { }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        public virtual T Spawn(Vector3 position, Quaternion rotation)
        {
            T obj = null;
            
            while (_available.Count > 0 && obj == null)
            {
                obj = _available.Dequeue();
                if (obj == null) continue;
            }
            
            if (obj == null)
            {
                if (_autoExpand && TotalCount < _maxSize)
                {
                    ExpandPool();
                    obj = _available.Count > 0 ? _available.Dequeue() : CreateNewObject();
                }
                else
                {
                    DebugLogger.LogWarning($"{GetType().Name} pool exhausted!", LogCategory.Pooling, this);
                    return null;
                }
            }
            
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            
            OnBeforeSpawn(obj);
            obj.gameObject.SetActive(true);
            _inUse.Add(obj);
            
            OnObjectSpawned?.Invoke(obj);
            return obj;
        }
        
        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public virtual void Despawn(T obj)
        {
            if (obj == null) return;
            if (!_inUse.Contains(obj)) return;
            
            _inUse.Remove(obj);
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_poolContainer);
            
            OnDespawn(obj);
            _available.Enqueue(obj);
            
            OnObjectDespawned?.Invoke(obj);
        }
        
        /// <summary>
        /// Expands the pool by the expand amount.
        /// </summary>
        public virtual void ExpandPool()
        {
            int toCreate = Mathf.Min(_expandAmount, _maxSize - TotalCount);
            for (int i = 0; i < toCreate; i++)
            {
                T obj = CreateNewObject();
                _available.Enqueue(obj);
            }
            
            DebugLogger.Log($"{GetType().Name} expanded by {toCreate}", LogCategory.Pooling, this);
        }
        
        /// <summary>
        /// Despawns all active objects.
        /// </summary>
        public virtual void DespawnAll()
        {
            foreach (var obj in new List<T>(_inUse))
            {
                Despawn(obj);
            }
        }
        
        #endregion
    }
}
