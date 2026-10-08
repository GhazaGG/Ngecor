using System;
using System.Reflection;
using Ngecor.Material;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ngecor.Editor
{
    public static class BulkMaterialDemo
    {
        private const string ScenePath = "Assets/Game/Scenes/Dev/Dev_Ghaza.unity";

        [MenuItem("Ngecor/Material Demo/Create Dev Scene")]
        public static void CreateDevScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException($"Dev scene already exists: {ScenePath}");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0f, 6f, -11f), Quaternion.Euler(25f, 0f, 0f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            CreateContainer("Sand Source", new Vector3(-4.5f, 0.5f, 0f), 20,
                ContainerMode.SingleType, MaterialType.Sand,
                new[] { MaterialType.Sand }, new MaterialAmount(MaterialType.Sand, 12), true);
            CreateContainer("Mixed Receiver", new Vector3(-1.5f, 0.5f, 0f), 20,
                ContainerMode.MultipleTypes, MaterialType.Sand,
                new[] { MaterialType.Sand, MaterialType.Cement }, null, false);
            CreateContainer("Cement Source", new Vector3(1.5f, 0.5f, 0f), 10,
                ContainerMode.SingleType, MaterialType.Cement,
                new[] { MaterialType.Cement }, new MaterialAmount(MaterialType.Cement, 6), false);
            CreateContainer("Sand Only", new Vector3(4.5f, 0.5f, 0f), 10,
                ContainerMode.SingleType, MaterialType.Sand,
                new[] { MaterialType.Sand }, null, false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Created {ScenePath}. Enter Play Mode, then use Ngecor > Material Demo menu.");
        }

        [MenuItem("Ngecor/Material Demo/Transfer Sand For One Second")]
        private static void TransferSand()
        {
            Transfer("Sand Source", "Mixed Receiver", MaterialType.Sand);
        }

        [MenuItem("Ngecor/Material Demo/Transfer Cement For One Second")]
        private static void TransferCement()
        {
            Transfer("Cement Source", "Mixed Receiver", MaterialType.Cement);
        }

        [MenuItem("Ngecor/Material Demo/Try Cement Into Sand Only")]
        private static void TryRejectedTransfer()
        {
            Transfer("Cement Source", "Sand Only", MaterialType.Cement);
        }

        [MenuItem("Ngecor/Material Demo/Run Container Checks")]
        public static void RunContainerChecks()
        {
            var type = Type.GetType("Ngecor.Material.Tests.BulkMaterialContainerTests, Ngecor.Material.Tests");
            if (type == null)
                throw new InvalidOperationException("Ngecor.Material.Tests assembly is unavailable.");

            var passed = 0;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var isTest = false;
                foreach (var attribute in method.GetCustomAttributes(false))
                    isTest |= attribute.GetType().Name == "TestAttribute";
                if (!isTest)
                    continue;

                var suite = Activator.CreateInstance(type);
                try
                {
                    method.Invoke(suite, null);
                    Debug.Log($"PASS {method.Name}");
                    passed++;
                }
                catch (TargetInvocationException error)
                {
                    throw error.InnerException ?? error;
                }
                finally
                {
                    type.GetMethod("TearDown").Invoke(suite, null);
                }
            }

            if (passed == 0)
                throw new InvalidOperationException("No container checks were found.");
            Debug.Log($"Passed {passed} MAT-005 container checks.");
        }

        [MenuItem("Ngecor/Material Demo/Verify Dev Scene")]
        public static void VerifyDevScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sand = GameObject.Find("Sand Source").GetComponent<BulkMaterialContainer>();
            var mixed = GameObject.Find("Mixed Receiver").GetComponent<BulkMaterialContainer>();
            var cement = GameObject.Find("Cement Source").GetComponent<BulkMaterialContainer>();
            var sandOnly = GameObject.Find("Sand Only").GetComponent<BulkMaterialContainer>();
            if (sand.GetUnits(MaterialType.Sand) != 12 || cement.GetUnits(MaterialType.Cement) != 6
                || !mixed.Accepts(MaterialType.Sand) || !mixed.Accepts(MaterialType.Cement)
                || sandOnly.Accepts(MaterialType.Cement))
                throw new InvalidOperationException("Dev scene container configuration is incorrect.");

            typeof(BulkMaterialContainer).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(sand, null);
            var before = sand.TotalUnits + mixed.TotalUnits;
            var moved = sand.TransferForSeconds(mixed, MaterialType.Sand, 1f);
            if (moved != 10 || before != sand.TotalUnits + mixed.TotalUnits
                || cement.TransferForSeconds(sandOnly, MaterialType.Cement, 1f) != 0
                || cement.GetUnits(MaterialType.Cement) != 6)
                throw new InvalidOperationException("Dev scene transfer check failed.");

            sand.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var fixedUpdate = typeof(BulkMaterialContainer).GetMethod("FixedUpdate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            for (var i = 0; i < 11; i++)
                fixedUpdate.Invoke(sand, null);
            if (sand.GetUnits(MaterialType.Sand) != 2)
                throw new InvalidOperationException("Dev scene tilt spill check failed.");

            Debug.Log("MAT-005 dev scene checks passed: serialized state, conserved transfer, "
                + "type rejection, and retention without a ground deposit. Changes were not saved.");
        }

        private static void Transfer(string sourceName, string targetName, MaterialType type)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before transferring material.");
                return;
            }

            var source = GameObject.Find(sourceName)?.GetComponent<BulkMaterialContainer>();
            var target = GameObject.Find(targetName)?.GetComponent<BulkMaterialContainer>();
            if (source == null || target == null)
            {
                Debug.LogWarning("Open Dev_Ghaza before using the material demo menu.");
                return;
            }

            var before = source.TotalUnits + target.TotalUnits;
            var moved = source.TransferForSeconds(target, type, 1f);
            Debug.Log($"{type}: moved {moved}; {sourceName}={source.GetUnits(type)}, "
                + $"{targetName}={target.GetUnits(type)}, total {before}->{source.TotalUnits + target.TotalUnits}");
        }

        private static void CreateContainer(string name, Vector3 position, int capacity,
            ContainerMode mode, MaterialType singleType, MaterialType[] accepted,
            MaterialAmount? initial, bool withRigidbody)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            gameObject.AddComponent<BoxCollider>();
            if (withRigidbody)
                gameObject.AddComponent<Rigidbody>().isKinematic = true;

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(gameObject.transform, false);
            fill.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            UnityEngine.Object.DestroyImmediate(fill.GetComponent<Collider>());

            var container = gameObject.AddComponent<BulkMaterialContainer>();
            var serialized = new SerializedObject(container);
            serialized.FindProperty("_capacity").intValue = capacity;
            serialized.FindProperty("_mode").enumValueIndex = (int)mode;
            serialized.FindProperty("_singleType").enumValueIndex = (int)singleType;
            serialized.FindProperty("_fillVisual").objectReferenceValue = fill.transform;
            var types = serialized.FindProperty("_acceptedTypes");
            types.arraySize = accepted.Length;
            for (var i = 0; i < accepted.Length; i++)
                types.GetArrayElementAtIndex(i).enumValueIndex = (int)accepted[i];
            var contents = serialized.FindProperty("_contents");
            contents.arraySize = initial.HasValue ? 1 : 0;
            if (initial.HasValue)
            {
                var entry = contents.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("_type").enumValueIndex = (int)initial.Value.Type;
                entry.FindPropertyRelative("_units").intValue = initial.Value.Units;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
