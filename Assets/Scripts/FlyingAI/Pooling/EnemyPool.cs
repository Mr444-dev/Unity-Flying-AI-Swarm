using UnityEngine;
using FlyingAI.Core;
using FlyingAI.Pooling;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;
using Cysharp.Threading.Tasks;

namespace FlyingAI.Pooling
{
    /// <summary>
    /// Object pool specialized for flying enemies with UniTask async spawning.
    /// </summary>
    public class EnemyPool : ObjectPoolBase<FlyingEnemy>
    {
        #region Singleton
        
        private static EnemyPool _instance;
        public static EnemyPool Instance => _instance;
        
        #endregion

        #region Serialized Fields
        
        [Header("Enemy Settings")]
        [SerializeField] private FlyingEnemyConfig _defaultConfig;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            base.Awake();
        }
        
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        
        #endregion

        #region Overrides
        
        protected override void OnBeforeSpawn(FlyingEnemy obj)
        {
            obj.ResetForPool();
        }
        
        protected override void OnDespawn(FlyingEnemy obj)
        {
            obj.ResetForPool();
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Spawns an enemy with specific configuration.
        /// </summary>
        public FlyingEnemy SpawnWithConfig(Vector3 position, Quaternion rotation, FlyingEnemyConfig config)
        {
            FlyingEnemy enemy = Spawn(position, rotation);
            if (enemy != null && config != null)
            {
                enemy.Initialize(config);
            }
            return enemy;
        }
        
        /// <summary>
        /// Spawns multiple enemies asynchronously.
        /// </summary>
        public async UniTask<FlyingEnemy[]> SpawnMultipleAsync(Vector3[] positions, Quaternion[] rotations, 
            FlyingEnemyConfig config, float delayBetweenSpawns = 0.1f)
        {
            FlyingEnemy[] enemies = new FlyingEnemy[positions.Length];
            
            for (int i = 0; i < positions.Length; i++)
            {
                enemies[i] = SpawnWithConfig(positions[i], rotations[i], config);
                
                if (delayBetweenSpawns > 0 && i < positions.Length - 1)
                {
                    await UniTask.Delay((int)(delayBetweenSpawns * 1000));
                }
            }
            
            DebugLogger.Log($"Spawned {positions.Length} enemies async", LogCategory.Spawning);
            return enemies;
        }
        
        #endregion
    }
}
