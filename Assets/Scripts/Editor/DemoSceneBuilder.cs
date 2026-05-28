using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using FlyingAI.Managers;
using FlyingAI.Demo;
using FlyingAI.Pooling;
using FlyingAI.ScriptableObjects;
using FlyingAI.Core;

namespace FlyingAI.Editor
{
    public class DemoSceneBuilder
    {
        [MenuItem("Flying AI/Generate Demo Scene")]
        public static void GenerateDemoScene()
        {
            // 1. Create a new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 2. Set up Managers
            GameObject managersGo = new GameObject("Managers");
            var spawnManager = managersGo.AddComponent<SpawnManager>();
            var formationManager = managersGo.AddComponent<FormationManager>();
            var swarmController = managersGo.AddComponent<SwarmController>();
            var tacticsController = managersGo.AddComponent<SwarmTacticsController>();
            var prioritySystem = managersGo.AddComponent<EnemyPrioritySystem>();
            var waveStatsTracker = managersGo.AddComponent<WaveStatsTracker>();

            // 3. Set up EnemyPool
            GameObject enemyPoolGo = new GameObject("EnemyPool");
            var enemyPool = enemyPoolGo.AddComponent<EnemyPool>();

            // 4. Set up DemoSpawner
            GameObject demoSpawnerGo = new GameObject("DemoSpawner");
            var demoSpawner = demoSpawnerGo.AddComponent<DemoSpawner>();
            demoSpawnerGo.transform.position = new Vector3(0, 5, 0);

            // Find Configs
            var enemyConfig = FindAssetByType<FlyingEnemyConfig>();
            var formationConfig = FindAssetByType<FormationConfig>();
            var weaponConfig = FindAssetByType<WeaponConfig>();
            var waveConfig = FindAssetByType<WaveConfig>();

            // Generate a clean prefab for the Demo to avoid missing script errors
            FlyingEnemy enemyPrefab = GenerateCleanEnemyPrefab();

            // Assign via reflection/SerializedObject to avoid private field issues
            AssignField(spawnManager, "_enemyPool", enemyPool);
            if (waveConfig != null) {
                AssignArrayField(spawnManager, "_waves", new Object[] { waveConfig });
            }

            AssignField(demoSpawner, "_enemyConfig", enemyConfig);
            AssignField(demoSpawner, "_formationConfig", formationConfig);
            AssignField(demoSpawner, "_weaponConfig", weaponConfig);
            AssignField(demoSpawner, "_spawnRadius", 20f);
            
            if (enemyPrefab != null) {
                AssignField(enemyPool, "_prefab", enemyPrefab);
            }
            AssignField(enemyPool, "_initialSize", 20);
            AssignField(enemyPool, "_maxSize", 100);

            // 6. Setup some floor for visual context
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(10, 1, 10);
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            if (floorRenderer != null) {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.2f, 0.2f);
                floorRenderer.sharedMaterial = mat;
            }

            // Adjust camera
            Camera mainCam = Camera.main;
            if (mainCam != null) {
                mainCam.transform.position = new Vector3(0, 15, -25);
                mainCam.transform.rotation = Quaternion.Euler(30, 0, 0);
            }

            // Save Scene if needed, or leave it unsaved so user can test
            EditorUtility.DisplayDialog("Demo Scene Generated", "The Demo Scene has been successfully generated! All managers and configs are wired up.\n\nPress Play, and then use 'S' to spawn a single enemy, 'F' for a formation, and 'K' to kill all.", "Awesome!");
        }

        private static T FindAssetByType<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }

        private static FlyingEnemy GenerateCleanEnemyPrefab()
        {
            string prefabPath = "Assets/DemoEnemyPrefab.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                var comp = existing.GetComponent<FlyingEnemy>();
                if (comp != null) return comp;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "DemoEnemyPrefab";
            var enemy = go.AddComponent<FlyingEnemy>();
            
            // It needs a collider, which PrimitiveType.Sphere already provides
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            GameObject.DestroyImmediate(go);
            
            return prefab.GetComponent<FlyingEnemy>();
        }

        private static void AssignField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is Object objValue)
                    prop.objectReferenceValue = objValue;
                else if (value is float floatValue)
                    prop.floatValue = floatValue;
                else if (value is int intValue)
                    prop.intValue = intValue;
                
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[DemoSceneBuilder] Could not find field {fieldName} on {target.name} ({target.GetType()})");
            }
        }
        
        private static void AssignArrayField(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
                so.ApplyModifiedProperties();
            }
        }
    }
}
