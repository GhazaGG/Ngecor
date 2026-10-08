#if UNITY_EDITOR
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
        private const string PrefabPath = "Assets/Game/Prefabs/Material/ManualMixingSpot.prefab";
        private const string ScenePath = "Assets/Game/Scenes/Dev/Dev_Ghaza.unity";

        [MenuItem("Ngecor/Setup Manual Mixing Spot")]
        public static void Setup()
        {
            var pile = AssetDatabase.LoadAssetAtPath<GroundMaterialPile>("Assets/Game/Prefabs/Material/GroundPile.prefab");
            var concrete = AssetDatabase.LoadAssetAtPath<MaterialAsset>("Assets/Game/Materials/Material/Concrete.mat");
            var progressMaterial = AssetDatabase.LoadAssetAtPath<MaterialAsset>("Assets/Game/Materials/Material/Cement.mat");
            var ingredientMaterial = AssetDatabase.LoadAssetAtPath<MaterialAsset>("Assets/Game/Materials/Material/Sand.mat");
            if (pile == null || concrete == null || progressMaterial == null || ingredientMaterial == null)
                throw new System.InvalidOperationException("Mixing spot prefab references are missing.");

            var prefabRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabRoot.name = "ManualMixingSpot";
            prefabRoot.transform.localScale = new Vector3(3f, 0.15f, 2.4f);
            prefabRoot.GetComponent<Renderer>().sharedMaterial = concrete;
            var stock = prefabRoot.AddComponent<BulkMaterialContainer>();
            var ingredients = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ingredients.name = "Ingredients";
            ingredients.transform.SetParent(prefabRoot.transform, false);
            ingredients.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            ingredients.transform.localScale = new Vector3(0.4f, 2f, 0.45f);
            ingredients.GetComponent<Renderer>().sharedMaterial = ingredientMaterial;
            Object.DestroyImmediate(ingredients.GetComponent<Collider>());
            Configure(stock, 200, true, ingredients.transform);
            var spot = prefabRoot.AddComponent<ManualMixingSpot>();

            var output = new GameObject("OutputPoint");
            output.transform.SetParent(prefabRoot.transform, false);
            output.transform.localPosition = new Vector3(0.72f, 2f, 0f);

            var progress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            progress.name = "Progress";
            progress.transform.SetParent(prefabRoot.transform, false);
            progress.transform.localPosition = new Vector3(0f, 0.65f, 0.36f);
            progress.transform.localScale = new Vector3(0.3f, 0.45f, 0.035f);
            progress.GetComponent<Renderer>().sharedMaterial = progressMaterial;
            Object.DestroyImmediate(progress.GetComponent<Collider>());

            var pourTrigger = new GameObject("PourTrigger");
            pourTrigger.transform.SetParent(prefabRoot.transform, false);
            pourTrigger.transform.localPosition = new Vector3(0f, 3f, 0f);
            var trigger = pourTrigger.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.2f, 8f, 1.2f);
            pourTrigger.AddComponent<DebugPourReceiverTrigger>();

            var serializedSpot = new SerializedObject(spot);
            serializedSpot.FindProperty("_outputPilePrefab").objectReferenceValue = pile;
            serializedSpot.FindProperty("_outputPoint").objectReferenceValue = output.transform;
            serializedSpot.FindProperty("_progressVisual").objectReferenceValue = progress.transform;
            serializedSpot.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Object.DestroyImmediate(prefabRoot);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (FindSpot(scene) == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                instance.transform.position = new Vector3(2f, 0.075f, 2f);
                instance.name = "Manual Mixing Spot";
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Manual mixing spot prefab and Dev_Ghaza setup created.");
        }

        private static ManualMixingSpot FindSpot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var spot = root.GetComponent<ManualMixingSpot>();
                if (spot != null)
                    return spot;
            }
            return null;
        }

        private static void Configure(BulkMaterialContainer container, int capacity, bool acceptInputs,
            Transform fillVisual)
        {
            var serialized = new SerializedObject(container);
            serialized.FindProperty("_capacity").intValue = capacity;
            serialized.FindProperty("_mode").enumValueIndex = (int)ContainerMode.MultipleTypes;
            var accepted = serialized.FindProperty("_acceptedTypes");
            accepted.arraySize = acceptInputs ? 2 : 1;
            accepted.GetArrayElementAtIndex(0).enumValueIndex = (int)MaterialType.Sand;
            if (acceptInputs)
                accepted.GetArrayElementAtIndex(1).enumValueIndex = (int)MaterialType.Cement;
            serialized.FindProperty("_fillVisual").objectReferenceValue = fillVisual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
