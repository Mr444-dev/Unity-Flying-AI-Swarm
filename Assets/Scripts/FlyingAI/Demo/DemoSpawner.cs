using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Managers;
using FlyingAI.Formations;
using FlyingAI.Pooling;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;
using FlyingAIEvents = FlyingAI.Events.FlyingAIEvents;

namespace FlyingAI.Demo
{
    /// <summary>
    /// Demo spawner for testing the flying AI system.
    /// Provides debug visualization and manual spawn controls.
    /// </summary>
    public class DemoSpawner : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Spawn Settings")]
        [SerializeField] private FlyingEnemyConfig _enemyConfig;
        [SerializeField] private FormationConfig _formationConfig;
        [SerializeField] private WeaponConfig _weaponConfig;
        [SerializeField] private int _defaultSpawnCount = 10;
        [SerializeField] private float _spawnRadius = 20f;
        
        [Header("Visualization")]
        [SerializeField] private bool _showSpawnRadius = true;
        [SerializeField] private Color _radiusColor = Color.cyan;
        
        #endregion

        #region Private Fields
        
        private List<FlyingEnemyBase> _spawnedEnemies = new List<FlyingEnemyBase>();

        // Access New Input System via reflection so this script compiles even when Unity.InputSystem is not referenced.
        private static readonly System.Type _keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        private static readonly System.Reflection.PropertyInfo _keyboardCurrentProperty = _keyboardType?.GetProperty("current");
        private static readonly System.Reflection.PropertyInfo _sKeyProperty = _keyboardType?.GetProperty("sKey");
        private static readonly System.Reflection.PropertyInfo _fKeyProperty = _keyboardType?.GetProperty("fKey");
        private static readonly System.Reflection.PropertyInfo _kKeyProperty = _keyboardType?.GetProperty("kKey");
        
        #endregion

        #region Unity Lifecycle
        
        private void Update()
        {
            // Keyboard shortcuts for demo. Uses New Input System when available, then falls back to legacy input.
            if (IsKeyPressedThisFrame(_sKeyProperty, KeyCode.S)) SpawnSingleEnemy();
            if (IsKeyPressedThisFrame(_fKeyProperty, KeyCode.F)) SpawnFormation();
            if (IsKeyPressedThisFrame(_kKeyProperty, KeyCode.K)) KillAll();
        }

        private static bool IsKeyPressedThisFrame(System.Reflection.PropertyInfo keyProperty, KeyCode legacyKey)
        {
            if (TryGetNewInputKeyPress(keyProperty, out bool pressedThisFrame))
            {
                return pressedThisFrame;
            }

            return Input.GetKeyDown(legacyKey);
        }

        private static bool TryGetNewInputKeyPress(System.Reflection.PropertyInfo keyProperty, out bool pressedThisFrame)
        {
            pressedThisFrame = false;

            if (_keyboardCurrentProperty == null || keyProperty == null)
            {
                return false;
            }

            object keyboard = _keyboardCurrentProperty.GetValue(null, null);
            if (keyboard == null)
            {
                return true;
            }

            object keyControl = keyProperty.GetValue(keyboard, null);
            if (keyControl == null)
            {
                return true;
            }

            var keyControlType = keyControl.GetType();
            var wasPressedProperty = keyControlType.GetProperty("wasPressedThisFrame");
            if (wasPressedProperty == null)
            {
                return true;
            }

            object value = wasPressedProperty.GetValue(keyControl, null);
            pressedThisFrame = value is bool pressed && pressed;
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!_showSpawnRadius) return;
            
            Gizmos.color = _radiusColor;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }
        
        #endregion

        #region Public API
        
        public void SpawnSingleEnemy()
        {
            if (_enemyConfig == null) return;
            
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * _spawnRadius;
            var enemy = SpawnManager.Instance?.SpawnEnemy(_enemyConfig, spawnPos, transform.rotation);
            
            if (enemy != null)
            {
                _spawnedEnemies.Add(enemy);
                DebugLogger.Log("Spawned single enemy via DemoSpawner", LogCategory.General);
            }
        }

        public void SpawnFormation()
        {
            if (_enemyConfig == null || _formationConfig == null) return;
            
            List<FlyingEnemyBase> enemies = new List<FlyingEnemyBase>();
            for (int i = 0; i < _defaultSpawnCount; i++)
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * 5f;
                var enemy = SpawnManager.Instance?.SpawnEnemy(_enemyConfig, spawnPos, transform.rotation);
                if (enemy != null) enemies.Add(enemy);
            }
            
            if (enemies.Count > 0)
            {
                var formation = FormationManager.Instance?.CreateFormation(_formationConfig, transform.position, transform.forward);
                if (formation != null)
                {
                    FormationManager.Instance.AssignEnemiesToFormation(formation, enemies);
                    _spawnedEnemies.AddRange(enemies);
                    DebugLogger.Log($"Spawned formation with {enemies.Count} units", LogCategory.General);
                }
            }
        }
        
        public void SpawnMultiple()
        {
            for (int i = 0; i < _defaultSpawnCount; i++)
            {
                SpawnSingleEnemy();
            }
        }
        
        public void OrderAttack()
        {
            var target = SwarmController.Instance?.PrimaryTarget;
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null) enemy.SetTarget(target);
            }
            DebugLogger.Log("Ordered all spawned enemies to attack", LogCategory.Combat);
        }
        
        public void OrderRetreat()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null) enemy.SetTarget(null);
            }
            DebugLogger.Log("Ordered all spawned enemies to retreat", LogCategory.Combat);
        }
        
        public void KillAll()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null) enemy.TakeDamage(new DamageInfo(9999f));
            }
            _spawnedEnemies.Clear();
            DebugLogger.Log("Killed all demo-spawned enemies", LogCategory.General);
        }
        
        public void DespawnAll()
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null) SpawnManager.Instance?.DespawnEnemy(enemy);
            }
            _spawnedEnemies.Clear();
            DebugLogger.Log("Despawned all demo-spawned enemies", LogCategory.General);
        }
        
        #endregion
    }
}
