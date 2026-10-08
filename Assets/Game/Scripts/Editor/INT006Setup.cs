using System;
using System.IO;
using Ngecor.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ngecor.Editor
{
    public static class INT006Setup
    {
        private const string ShaderPath = "Assets/Game/Materials/Interaction/OutlineEdge.shader";
        private const string MaterialDir = "Assets/Game/Materials/Interaction";
        private const string MaterialPath = "Assets/Game/Materials/Interaction/M_OutlineEdge.mat";
        private const string PrefabDir = "Assets/Game/Prefabs/UI";
        private const string HudPrefabPath = "Assets/Game/Prefabs/UI/InteractionHUD.prefab";
        private const string PlayerPrefabPath = "Assets/Game/Prefabs/Player/Player.prefab";
        private const string StatusPath = "Temp/int006_setup_result.txt";

        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            EditorApplication.delayCall += RunSetup;
        }

        [MenuItem("Ngecor/INT-006 Setup Assets")]
        public static void RunSetup()
        {
            try
            {
                // 1. Ensure folder exists
                if (!AssetDatabase.IsValidFolder(MaterialDir))
                {
                    AssetDatabase.CreateFolder("Assets/Game/Materials", "Interaction");
                }

                // 2. Create M_OutlineEdge.mat if missing or update shader
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                if (shader == null)
                    shader = Shader.Find("Ngecor/OutlineEdge");

                var mat = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(MaterialPath);
                if (mat == null)
                {
                    mat = new UnityEngine.Material(shader);
                    mat.SetColor("_OutlineColor", new Color(1f, 1f, 1f, 0.9f));
                    mat.SetFloat("_OutlineWidth", 0.02f);
                    AssetDatabase.CreateAsset(mat, MaterialPath);
                }
                else if (shader != null)
                {
                    mat.shader = shader;
                    mat.SetColor("_OutlineColor", new Color(1f, 1f, 1f, 0.9f));
                    mat.SetFloat("_OutlineWidth", 0.02f);
                    EditorUtility.SetDirty(mat);
                }

                // 3. Ensure UI Prefab dir exists
                if (!AssetDatabase.IsValidFolder(PrefabDir))
                {
                    AssetDatabase.CreateFolder("Assets/Game/Prefabs", "UI");
                }

                // 4. Create InteractionHUD.prefab
                var hudGo = new GameObject("InteractionHUD", typeof(RectTransform));
                var canvas = hudGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = hudGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                hudGo.AddComponent<GraphicRaycaster>();

                var crosshairGo = new GameObject("Crosshair", typeof(RectTransform));
                crosshairGo.transform.SetParent(hudGo.transform, false);
                var crosshairRect = crosshairGo.GetComponent<RectTransform>();
                crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
                crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
                crosshairRect.pivot = new Vector2(0.5f, 0.5f);
                crosshairRect.anchoredPosition = Vector2.zero;
                crosshairRect.sizeDelta = new Vector2(16f, 16f);

                // Horizontal Bar
                var hBarGo = new GameObject("HorizontalBar", typeof(RectTransform));
                hBarGo.transform.SetParent(crosshairGo.transform, false);
                var hRect = hBarGo.GetComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0.5f, 0.5f);
                hRect.anchorMax = new Vector2(0.5f, 0.5f);
                hRect.pivot = new Vector2(0.5f, 0.5f);
                hRect.anchoredPosition = Vector2.zero;
                hRect.sizeDelta = new Vector2(12f, 2f);
                var hImg = hBarGo.AddComponent<Image>();
                hImg.color = new Color(1f, 1f, 1f, 0.9f);
                hImg.raycastTarget = false;

                // Vertical Bar
                var vBarGo = new GameObject("VerticalBar", typeof(RectTransform));
                vBarGo.transform.SetParent(crosshairGo.transform, false);
                var vRect = vBarGo.GetComponent<RectTransform>();
                vRect.anchorMin = new Vector2(0.5f, 0.5f);
                vRect.anchorMax = new Vector2(0.5f, 0.5f);
                vRect.pivot = new Vector2(0.5f, 0.5f);
                vRect.anchoredPosition = Vector2.zero;
                vRect.sizeDelta = new Vector2(2f, 12f);
                var vImg = vBarGo.AddComponent<Image>();
                vImg.color = new Color(1f, 1f, 1f, 0.9f);
                vImg.raycastTarget = false;

                // Prompt Text
                var promptGo = new GameObject("PromptText", typeof(RectTransform));
                promptGo.transform.SetParent(hudGo.transform, false);
                var pRect = promptGo.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0.5f, 0.5f);
                pRect.anchorMax = new Vector2(0.5f, 0.5f);
                pRect.pivot = new Vector2(0.5f, 1f);
                pRect.anchoredPosition = new Vector2(0f, -35f);
                pRect.sizeDelta = new Vector2(400f, 32f);
                var pText = promptGo.AddComponent<Text>();
                pText.alignment = TextAnchor.UpperCenter;
                pText.fontSize = 18;
                pText.color = Color.white;
                pText.raycastTarget = false;
                pText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                             ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                pText.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(hudGo, HudPrefabPath);
                UnityEngine.Object.DestroyImmediate(hudGo);

                // 5. Update Player.prefab
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (playerPrefab != null)
                {
                    var playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

                    // Update InteractionDetector
                    var detector = playerRoot.GetComponent<InteractionDetector>();
                    if (detector != null)
                    {
                        var detectorSo = new SerializedObject(detector);
                        var prop = detectorSo.FindProperty("_showDebugFeedback");
                        if (prop != null) prop.boolValue = false;
                        detectorSo.ApplyModifiedPropertiesWithoutUndo();
                    }

                    // Update PlayerGrab
                    var grab = playerRoot.GetComponent<PlayerGrab>();
                    if (grab != null)
                    {
                        var grabSo = new SerializedObject(grab);
                        var prop = grabSo.FindProperty("_showDebugFeedback");
                        if (prop != null) prop.boolValue = false;
                        grabSo.ApplyModifiedPropertiesWithoutUndo();
                    }

                    // Ensure InteractionHighlighter component
                    var highlighter = playerRoot.GetComponent<InteractionHighlighter>();
                    if (highlighter == null)
                        highlighter = playerRoot.AddComponent<InteractionHighlighter>();
                    var highlighterSo = new SerializedObject(highlighter);
                    var matProp = highlighterSo.FindProperty("_outlineMaterial");
                    if (matProp != null)
                        matProp.objectReferenceValue = mat;
                    var widthProp = highlighterSo.FindProperty("_outlineWidth");
                    if (widthProp != null)
                        widthProp.floatValue = 0.02f;
                    highlighterSo.ApplyModifiedPropertiesWithoutUndo();

                    // Ensure InteractionPromptUI component
                    var promptUi = playerRoot.GetComponent<InteractionPromptUI>();
                    if (promptUi == null)
                        promptUi = playerRoot.AddComponent<InteractionPromptUI>();

                    PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
                    PrefabUtility.UnloadPrefabContents(playerRoot);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                File.WriteAllText(StatusPath, "SUCCESS: INT-006 setup complete at " + DateTime.UtcNow.ToString("o"));
                Debug.Log("[INT-006] Setup completed successfully!");
            }
            catch (Exception ex)
            {
                File.WriteAllText(StatusPath, "ERROR: " + ex.ToString());
                Debug.LogError("[INT-006] Setup failed: " + ex);
            }
        }
    }
}
