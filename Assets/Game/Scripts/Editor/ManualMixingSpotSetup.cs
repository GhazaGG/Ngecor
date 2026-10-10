#if UNITY_EDITOR
using System.Collections.Generic;
using Ngecor.Material;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MaterialAsset = UnityEngine.Material;

namespace Ngecor.EditorTools
{
    public static class ManualMixingSpotSetup
    {
        private const string SpotPrefabPath = "Assets/Game/Prefabs/Material/ManualMixingSpot.prefab";
        private const string DrumPrefabPath = "Assets/Game/Prefabs/Material/WaterDrum.prefab";
        private const string WaterMaterialPath = "Assets/Game/Materials/Material/Water.mat";
        private const string ScenePath = "Assets/Game/Scenes/Dev/Dev_Ghaza.unity";
        private const string TestSackName = "Mixing Test Cement Sack";
        private const string DrumName = "Water Drum";

        // Rebuilds the mixing spot, creates the water drum, and cleans Dev_Ghaza so every ingredient
        // has to be carried to the spot.
        [MenuItem("Ngecor/Setup Manual Mixing Spot")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var sand = AssetDatabase.LoadAssetAtPath<MaterialAsset>("Assets/Game/Materials/Material/Sand.mat");
            var cement = AssetDatabase.LoadAssetAtPath<MaterialAsset>("Assets/Game/Materials/Material/Cement.mat");
            var concrete = AssetDatabase.LoadAssetAtPath<MaterialAsset>("Assets/Game/Materials/Material/Concrete.mat");
            if (sand == null || cement == null || concrete == null)
                throw new System.InvalidOperationException("Material assets are missing.");
            var water = LoadOrCreateWaterMaterial(sand);

            BuildSpotPrefab(sand, cement, concrete);
            BuildDrumPrefab(concrete, water);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlaceInScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Manual mixing spot, water drum, and clean Dev_Ghaza setup created.");
        }

        // Dev scene only: CementBag.prefab becomes grabbable in MAT-003 (#46).
        [MenuItem("Ngecor/Make Dev_Ghaza Cement Sacks Grabbable")]
        public static void MakeDevSacksGrabbable()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var added = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var bag in root.GetComponentsInChildren<CementBag>(true))
                    if (!bag.TryGetComponent<Ngecor.Interaction.GrabbableObject>(out _))
                    {
                        bag.gameObject.AddComponent<Ngecor.Interaction.GrabbableObject>();
                        added++;
                    }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Added GrabbableObject to {added} cement sacks in Dev_Ghaza.");
        }

        private static MaterialAsset LoadOrCreateWaterMaterial(MaterialAsset template)
        {
            var existing = AssetDatabase.LoadAssetAtPath<MaterialAsset>(WaterMaterialPath);
            if (existing != null)
                return existing;
            var material = new MaterialAsset(template) { name = "Water" };
            var blue = new Color(0.2f, 0.45f, 0.85f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", blue);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", blue);
            AssetDatabase.CreateAsset(material, WaterMaterialPath);
            return material;
        }

        private static void BuildSpotPrefab(MaterialAsset sand, MaterialAsset cement, MaterialAsset concrete)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "ManualMixingSpot";
            root.transform.localScale = new Vector3(3f, 0.15f, 2.4f);
            root.GetComponent<Renderer>().sharedMaterial = concrete;
            var stock = root.AddComponent<BulkMaterialContainer>();

            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Ingredients";
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            bed.transform.localScale = new Vector3(0.4f, 2f, 0.45f);
            bed.GetComponent<Renderer>().sharedMaterial = sand;
            Object.DestroyImmediate(bed.GetComponent<Collider>());
            Configure(stock, 200, ContainerMode.MultipleTypes, bed.transform,
                MaterialType.Sand, MaterialType.Cement, MaterialType.Water, MaterialType.Concrete);

            var progress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            progress.name = "Progress";
            progress.transform.SetParent(root.transform, false);
            progress.transform.localPosition = new Vector3(0f, 0.65f, 0.36f);
            progress.transform.localScale = new Vector3(0.3f, 0.45f, 0.035f);
            progress.GetComponent<Renderer>().sharedMaterial = cement;
            Object.DestroyImmediate(progress.GetComponent<Collider>());

            AddPourTrigger(root.transform, new Vector3(0f, 3f, 0f), new Vector3(1.2f, 8f, 1.2f));

            var spot = root.AddComponent<ManualMixingSpot>();
            var serialized = new SerializedObject(spot);
            serialized.FindProperty("_bedRenderer").objectReferenceValue = bed.GetComponent<Renderer>();
            serialized.FindProperty("_progressVisual").objectReferenceValue = progress.transform;
            serialized.FindProperty("_progressRenderer").objectReferenceValue = progress.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddCollectRule(root, MaterialType.Concrete, true);

            PrefabUtility.SaveAsPrefabAsset(root, SpotPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildDrumPrefab(MaterialAsset body, MaterialAsset water)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "WaterDrum";
            root.transform.localScale = new Vector3(0.9f, 0.55f, 0.9f);
            root.GetComponent<Renderer>().sharedMaterial = body;
            var stock = root.AddComponent<BulkMaterialContainer>();

            // Slightly wider than the shell so the water level reads as a colored band from outside.
            var level = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            level.name = "WaterLevel";
            level.transform.SetParent(root.transform, false);
            level.transform.localScale = new Vector3(1.03f, 1f, 1.03f);
            level.GetComponent<Renderer>().sharedMaterial = water;
            Object.DestroyImmediate(level.GetComponent<Collider>());
            Configure(stock, 200, ContainerMode.SingleType, level.transform, MaterialType.Water);
            var serialized = new SerializedObject(stock);
            serialized.FindProperty("_singleType").enumValueIndex = (int)MaterialType.Water;
            var contents = serialized.FindProperty("_contents");
            contents.arraySize = 1;
            contents.GetArrayElementAtIndex(0).FindPropertyRelative("_type").enumValueIndex = (int)MaterialType.Water;
            contents.GetArrayElementAtIndex(0).FindPropertyRelative("_units").intValue = 200;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AddPourTrigger(root.transform, new Vector3(0f, 1f, 0f), new Vector3(2f, 4f, 2f));
            AddCollectRule(root, MaterialType.Water, false);

            PrefabUtility.SaveAsPrefabAsset(root, DrumPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void AddPourTrigger(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("PourTrigger");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
            go.AddComponent<DebugPourReceiverTrigger>();
        }

        private static void AddCollectRule(GameObject target, MaterialType type, bool shovelCanCollect)
        {
            var rule = target.AddComponent<CollectOnlyType>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_type").enumValueIndex = (int)type;
            serialized.FindProperty("_shovelCanCollect").boolValue = shovelCanCollect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Configure(BulkMaterialContainer container, int capacity, ContainerMode mode,
            Transform fillVisual, params MaterialType[] accepted)
        {
            var serialized = new SerializedObject(container);
            serialized.FindProperty("_capacity").intValue = capacity;
            serialized.FindProperty("_mode").enumValueIndex = (int)mode;
            var types = serialized.FindProperty("_acceptedTypes");
            types.arraySize = accepted.Length;
            for (var i = 0; i < accepted.Length; i++)
                types.GetArrayElementAtIndex(i).enumValueIndex = (int)accepted[i];
            serialized.FindProperty("_fillVisual").objectReferenceValue = fillVisual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceInScene(Scene scene)
        {
            var spotPosition = new Vector3(2f, 0.075f, 2f);
            var spotRotation = Quaternion.identity;
            var oldSacks = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var oldSpot = root.GetComponent<ManualMixingSpot>();
                if (oldSpot != null)
                {
                    spotPosition = root.transform.position;
                    spotRotation = root.transform.rotation;
                    Object.DestroyImmediate(root);
                }
                else if (root.name == TestSackName || root.name == DrumName)
                    oldSacks.Add(root);
            }
            foreach (var old in oldSacks)
                Object.DestroyImmediate(old);

            var spot = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(SpotPrefabPath), scene);
            spot.transform.SetPositionAndRotation(spotPosition, spotRotation);
            spot.name = "Manual Mixing Spot";

            var drum = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(DrumPrefabPath), scene);
            drum.name = DrumName;
            Physics.SyncTransforms();
            var offsets = new List<Vector3>();
            for (var radius = 3.5f; radius <= 9f; radius += 1.5f)
                for (var angle = 0; angle < 360; angle += 30)
                    offsets.Add(Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius);
            foreach (var offset in offsets)
            {
                var candidate = spotPosition + offset;
                if (!Physics.Raycast(candidate + Vector3.up * 2f, Vector3.down, out var ground, 6f,
                        Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    continue;
                var position = ground.point + Vector3.up * 0.56f;
                drum.transform.position = position;
                Physics.SyncTransforms();
                if (OnlyHitsDrum(position))
                    return;
            }
            Debug.LogWarning("No free floor found near the mixing spot; move '" + DrumName + "' by hand.");
        }

        private static bool OnlyHitsDrum(Vector3 position)
        {
            foreach (var hit in Physics.OverlapBox(position, new Vector3(0.55f, 0.5f, 0.55f),
                         Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                if (hit.GetComponentInParent<BulkMaterialContainer>() == null
                    || hit.GetComponentInParent<CollectOnlyType>() == null
                    || hit.GetComponentInParent<CollectOnlyType>().Type != MaterialType.Water)
                    return false;
            return true;
        }
    }
}
#endif
