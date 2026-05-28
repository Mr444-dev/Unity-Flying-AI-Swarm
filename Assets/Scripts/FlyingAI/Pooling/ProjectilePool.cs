using UnityEngine;
using FlyingAI.Pooling;

namespace FlyingAI.Combat
{
    /// <summary>
    /// Object pool specialized for projectiles.
    /// </summary>
    public class ProjectilePool : ObjectPoolBase<ProjectileBase>
    {
        #region Singleton
        
        private static ProjectilePool _instance;
        public static ProjectilePool Instance => _instance;
        
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
        
        protected override void OnBeforeSpawn(ProjectileBase obj)
        {
            obj.ResetForPool();
        }
        
        protected override void OnDespawn(ProjectileBase obj)
        {
            obj.ResetForPool();
        }
        
        #endregion
    }
}
