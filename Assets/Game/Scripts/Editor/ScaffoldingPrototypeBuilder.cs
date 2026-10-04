using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ngecor.Construction;

namespace Ngecor.EditorTools
{
    public static class ScaffoldingPrototypeBuilder
    {
        private const string MaterialsDir = "Assets/Game/Materials/Construction";
        private const string PrefabsDir = "Assets/Game/Prefabs/Construction";
        private const string SettingsDir = "Assets/Game/Settings";
        private const string ScenePath = "Assets/Game/Scenes/Playground.unity";

        private const string PhysicMatPath = SettingsDir + "/Scaffolding_Friction.physicsMaterial";
        private const string SteelMatPath = MaterialsDir + "/Scaffolding_Steel.mat";
        private const string WoodMatPath = MaterialsDir + "/Scaffolding_Wood.mat";
        private const string FramePrefabPath = PrefabsDir + "/ScaffoldingFrame.prefab";
        private const string PlatformPrefabPath = PrefabsDir + "/ScaffoldingPlatform.prefab";
        private const string ModulePrefabPath = PrefabsDir + "/ScaffoldingModule.prefab";
        private const string Issue18ShowcaseName = "Scaffolding_Issue18_Showcase";

        [MenuItem("Ngecor/Build Scaffolding Prototype")]
        public static void BuildAll()
        {
            Debug.Log("[ScaffoldingBuilder] Starting Scaffolding Prototype build...");

            EnsureDirectories();
            PhysicsMaterial physMat = CreateOrLoadPhysicMaterial();
            Material steelMat = CreateOrLoadSteelMaterial();
            Material woodMat = CreateOrLoadWoodMaterial();

            GameObject framePrefab = BuildFramePrefab(steelMat, physMat);
            GameObject platformPrefab = BuildPlatformPrefab(woodMat, physMat);

            PopulatePlaygroundScene(framePrefab, platformPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ScaffoldingBuilder] Build completed successfully!");
        }

        [MenuItem("Ngecor/Build BUILD-001 Scaffolding Module")]
        public static void BuildIssue18Prototype()
        {
            EnsureDirectories();

            PhysicsMaterial physMat = CreateOrLoadPhysicMaterial();
            Material woodMat = CreateOrLoadWoodMaterial();

            // Keep the older modular frame in the scene, but regenerate its visual
            // material as timber so every scaffold shown by the Playground matches
            // the one-material prototype direction.
            BuildFramePrefab(woodMat, physMat);
            GameObject modulePrefab = BuildModulePrefab(woodMat, woodMat, physMat);

            Scene scene = GetOrOpenPlayground();
            if (GameObject.Find(Issue18ShowcaseName) != null)
            {
                Debug.LogWarning("[ScaffoldingBuilder] BUILD-001 showcase already exists; left it and all existing showcase objects untouched.");
            }
            else
            {
                PopulateIssue18Scene(scene, modulePrefab, physMat, woodMat);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ScaffoldingBuilder] BUILD-001 module prefab is ready. Existing modular scaffold assets and showcase were preserved.");
        }

        [MenuItem("Ngecor/Update BUILD-001 Module Prefab")]
        public static void UpdateIssue18ModulePrefab()
        {
            EnsureDirectories();
            PhysicsMaterial physMat = CreateOrLoadPhysicMaterial();
            Material woodMat = CreateOrLoadWoodMaterial();
            BuildModulePrefab(woodMat, woodMat, physMat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject BuildModulePrefab(Material steelMat, Material woodMat, PhysicsMaterial physMat)
        {
            GameObject root = new GameObject("ScaffoldingModule");
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 65f;
            rb.linearDamping = 1.0f;
            rb.angularDamping = 2.0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            GameObject frameFront = BuildModuleFrame(root, "Frame_Front", -0.9f, steelMat, physMat, true);
            GameObject frameBack = BuildModuleFrame(root, "Frame_Back", 0.9f, steelMat, physMat, false);
            GameObject deckLeft = BuildDeckPart(root, "Platform_Left", -0.35f, woodMat, physMat);
            GameObject deckRight = BuildDeckPart(root, "Platform_Right", 0.35f, woodMat, physMat);

            GameObject loadProbe = new GameObject("Player_Load_Probe");
            loadProbe.transform.SetParent(root.transform, false);
            loadProbe.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            loadProbe.transform.localScale = new Vector3(1.35f, 0.9f, 2f);

            ScaffoldingLoadFailure loadFailure = root.AddComponent<ScaffoldingLoadFailure>();
            loadFailure.Configure(
                deckLeft.transform.GetChild(0).GetComponent<BoxCollider>(),
                loadProbe.transform,
                new[] { frameFront.transform, frameBack.transform, deckLeft.transform, deckRight.transform });

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ModulePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildModuleFrame(GameObject root, string frameName, float z, Material steelMat, PhysicsMaterial physMat, bool addLadderRungs)
        {
            GameObject frame = new GameObject(frameName);
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, z);

            // Narrow feet retain a smaller support footprint than the deck.
            CreateBar(frame, "Post_Left", new Vector3(-0.36f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), steelMat, physMat);
            CreateBar(frame, "Post_Right", new Vector3(0.36f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), steelMat, physMat);
            CreateBar(frame, "Foot_Left", new Vector3(-0.36f, 0.04f, 0f), new Vector3(0.26f, 0.08f, 0.30f), steelMat, physMat);
            CreateBar(frame, "Foot_Right", new Vector3(0.36f, 0.04f, 0f), new Vector3(0.26f, 0.08f, 0.30f), steelMat, physMat);
            CreateBar(frame, "Crossbar_Low", new Vector3(0f, 0.2f, 0f), new Vector3(0.72f, 0.12f, 0.12f), steelMat, physMat);
            CreateBar(frame, "Crossbar_Mid", new Vector3(0f, 0.95f, 0f), new Vector3(0.72f, 0.12f, 0.12f), steelMat, physMat);
            CreateBar(frame, "Top_Support_Bar", new Vector3(0f, 1.75f, 0f), new Vector3(0.84f, 0.14f, 0.14f), steelMat, physMat);

            if (addLadderRungs)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        GameObject rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        rung.name = $"SupportLadderRung_{(side < 0 ? "Left" : "Right")}_{i + 1:00}";
                        rung.transform.SetParent(frame.transform, false);
                        rung.transform.localPosition = new Vector3(side * 0.36f, 0.2f + i * 0.25f, -z);
                        rung.transform.localScale = new Vector3(0.14f, 0.07f, 1.8f);
                        rung.GetComponent<MeshRenderer>().sharedMaterial = steelMat;
                        Object.DestroyImmediate(rung.GetComponent<BoxCollider>());
                    }
                }
            }

            return frame;
        }

        private static GameObject BuildDeckPart(GameObject root, string name, float x, Material woodMat, PhysicsMaterial physMat)
        {
            GameObject deck = new GameObject(name);
            deck.transform.SetParent(root.transform, false);
            deck.transform.localPosition = new Vector3(x, 1.85f, 0f);

            CreateBar(deck, "Plank", Vector3.zero, new Vector3(0.7f, 0.06f, 2.1f), woodMat, physMat);
            return deck;
        }

        private static Scene GetOrOpenPlayground()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return scene;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        private static void PopulateIssue18Scene(Scene scene, GameObject modulePrefab, PhysicsMaterial physMat, Material woodMat)
        {
            GameObject showcase = new GameObject(Issue18ShowcaseName);
            SceneManager.MoveGameObjectToScene(showcase, scene);
            showcase.transform.position = new Vector3(-6f, 0f, 0f);

            GameObject lower = (GameObject)PrefabUtility.InstantiatePrefab(modulePrefab, scene);
            lower.name = "Module_Level_1";
            lower.transform.SetParent(showcase.transform, false);
            lower.transform.localPosition = Vector3.zero;

            GameObject upper = (GameObject)PrefabUtility.InstantiatePrefab(modulePrefab, scene);
            upper.name = "Module_Level_2";
            upper.transform.SetParent(showcase.transform, false);
            upper.transform.localPosition = new Vector3(0f, 1.88f, 0f);

            // Static primitive ramp rises 3.76 m over 3.76 m (45 degrees),
            // staying below the CharacterController's 50 degree slope limit.
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "AccessRamp_Static";
            ramp.transform.SetParent(showcase.transform, false);
            ramp.transform.localPosition = new Vector3(0f, 1.88f, -1.88f);
            ramp.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
            ramp.transform.localScale = new Vector3(1.4f, 0.16f, 5.317f);
            ramp.isStatic = true;
            ramp.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            ramp.GetComponent<BoxCollider>().sharedMaterial = physMat;

            CreateTestCargo(showcase.transform, "TestCargo_Standard", new Vector3(2f, 0.225f, 0f), 25f, physMat);
            CreateTestCargo(showcase.transform, "TestCargo_Heavy", new Vector3(3f, 0.225f, 0f), 125f, physMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateTestCargo(Transform parent, string name, Vector3 localPosition, float mass, PhysicsMaterial physMat)
        {
            GameObject cargo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargo.name = name;
            cargo.transform.SetParent(parent, false);
            cargo.transform.localPosition = localPosition;
            cargo.transform.localScale = Vector3.one * 0.45f;
            cargo.GetComponent<BoxCollider>().sharedMaterial = physMat;

            Rigidbody rb = cargo.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Game/Materials"))
                AssetDatabase.CreateFolder("Assets/Game", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsDir))
                AssetDatabase.CreateFolder("Assets/Game/Materials", "Construction");

            if (!AssetDatabase.IsValidFolder("Assets/Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Game", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabsDir))
                AssetDatabase.CreateFolder("Assets/Game/Prefabs", "Construction");
        }

        private static PhysicsMaterial CreateOrLoadPhysicMaterial()
        {
            PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicMatPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial("Scaffolding_Friction")
                {
                    dynamicFriction = 0.85f,
                    staticFriction = 0.90f,
                    bounciness = 0.0f,
                    frictionCombine = PhysicsMaterialCombine.Maximum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                AssetDatabase.CreateAsset(mat, PhysicMatPath);
            }
            return mat;
        }

        private static Material CreateOrLoadSteelMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SteelMatPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader)
                {
                    color = new Color(0.33f, 0.43f, 0.48f, 1f) // Galvanized steel blue-gray
                };
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.6f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

                AssetDatabase.CreateAsset(mat, SteelMatPath);
            }
            return mat;
        }

        private static Material CreateOrLoadWoodMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(WoodMatPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader)
                {
                    color = new Color(0.55f, 0.43f, 0.35f, 1f) // Timber plank
                };
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);

                AssetDatabase.CreateAsset(mat, WoodMatPath);
            }
            return mat;
        }

        private static GameObject BuildFramePrefab(Material steelMat, PhysicsMaterial physMat)
        {
            GameObject root = new GameObject("ScaffoldingFrame");
            root.tag = "Untagged";

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 25f;
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = 1.0f;
            rb.angularDamping = 2.0f;
#else
            rb.drag = 1.0f;
            rb.angularDrag = 2.0f;
#endif
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Posts: 2 vertical posts, height 1.8m, spaced at X = -0.56 and +0.56
            CreateBar(root, "Post_Left", new Vector3(-0.56f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), steelMat, physMat);
            CreateBar(root, "Post_Right", new Vector3(0.56f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), steelMat, physMat);

            // Foot plates for stable ground contact
            CreateBar(root, "Foot_Left", new Vector3(-0.56f, 0.04f, 0f), new Vector3(0.30f, 0.08f, 0.30f), steelMat, physMat);
            CreateBar(root, "Foot_Right", new Vector3(0.56f, 0.04f, 0f), new Vector3(0.30f, 0.08f, 0.30f), steelMat, physMat);

            // Bottom crossbar
            CreateBar(root, "Crossbar_Bottom", new Vector3(0f, 0.2f, 0f), new Vector3(1.12f, 0.12f, 0.12f), steelMat, physMat);

            // Ladder rungs (intermediate climbing/stepping access)
            CreateBar(root, "Rung_1", new Vector3(0f, 0.7f, 0f), new Vector3(1.12f, 0.12f, 0.12f), steelMat, physMat);
            CreateBar(root, "Rung_2", new Vector3(0f, 1.2f, 0f), new Vector3(1.12f, 0.12f, 0.12f), steelMat, physMat);

            // Top support bar (support beam for platforms and stacking)
            CreateBar(root, "Top_Support_Bar", new Vector3(0f, 1.78f, 0f), new Vector3(1.2f, 0.14f, 0.14f), steelMat, physMat);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FramePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildPlatformPrefab(Material woodMat, PhysicsMaterial physMat)
        {
            GameObject root = new GameObject("ScaffoldingPlatform");
            root.tag = "Untagged";

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 15f;
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.0f;
#else
            rb.drag = 0.5f;
            rb.angularDrag = 1.0f;
#endif
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Main walking plank: 2.0m long (Z), 0.6m wide (X), 0.06m thick (Y)
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Plank";
            plank.transform.SetParent(root.transform);
            plank.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            plank.transform.localScale = new Vector3(0.6f, 0.06f, 2.0f);
            plank.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            plank.GetComponent<BoxCollider>().sharedMaterial = physMat;

            // End Lips / Catch Hooks:
            // Underneath at Z = -0.98 and +0.98 to catch on the frame's top crossbars (spaced ~1.8m apart)
            // Inner clearance = ~1.92m, frame outer edge = 1.88m -> ~4cm loose play, allowing imperfect alignment
            CreateBar(root, "Lip_Front", new Vector3(0f, -0.04f, -0.98f), new Vector3(0.58f, 0.08f, 0.04f), woodMat, physMat);
            CreateBar(root, "Lip_Back", new Vector3(0f, -0.04f, 0.98f), new Vector3(0.58f, 0.08f, 0.04f), woodMat, physMat);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlatformPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateBar(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, PhysicsMaterial physMat)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(parent.transform);
            bar.transform.localPosition = pos;
            bar.transform.localScale = scale;
            bar.GetComponent<MeshRenderer>().sharedMaterial = mat;
            bar.GetComponent<BoxCollider>().sharedMaterial = physMat;
        }

        private static void PopulatePlaygroundScene(GameObject framePrefab, GameObject platformPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            // Clean up existing showcase if any
            GameObject existing = GameObject.Find("Scaffolding_Showcase");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject showcaseRoot = new GameObject("Scaffolding_Showcase");
            showcaseRoot.transform.position = Vector3.zero;

            // 1. Standard Assembly (Clean 1.8m spacing)
            GameObject standardGroup = new GameObject("Assembly_Standard");
            standardGroup.transform.SetParent(showcaseRoot.transform);
            standardGroup.transform.position = Vector3.zero;

            // Frame A at (6, 0, 1.1), rotated 90 deg around Y
            GameObject frameA = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, scene);
            frameA.name = "Frame_Std_A";
            frameA.transform.SetParent(standardGroup.transform);
            frameA.transform.position = new Vector3(6f, 0f, 1.1f);
            frameA.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Frame B at (6, 0, 2.9), rotated 90 deg around Y (1.8m span)
            GameObject frameB = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, scene);
            frameB.name = "Frame_Std_B";
            frameB.transform.SetParent(standardGroup.transform);
            frameB.transform.position = new Vector3(6f, 0f, 2.9f);
            frameB.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Platform resting on top bars at Y = 1.84m
            GameObject platformStd = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, scene);
            platformStd.name = "Platform_Std";
            platformStd.transform.SetParent(standardGroup.transform);
            platformStd.transform.position = new Vector3(6f, 1.85f, 2.0f);
            platformStd.transform.rotation = Quaternion.identity;

            // 2. Imperfect Assembly (Slightly skewed, demonstrating AC #5: "tanpa snapping terlalu ketat")
            GameObject imperfectGroup = new GameObject("Assembly_Imperfect");
            imperfectGroup.transform.SetParent(showcaseRoot.transform);
            imperfectGroup.transform.position = Vector3.zero;

            // Frame C slightly rotated and offset
            GameObject frameC = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, scene);
            frameC.name = "Frame_Imp_C";
            frameC.transform.SetParent(imperfectGroup.transform);
            frameC.transform.position = new Vector3(8.5f, 0f, 1.15f);
            frameC.transform.rotation = Quaternion.Euler(0f, 84f, 0f);

            // Frame D slightly rotated the other way
            GameObject frameD = (GameObject)PrefabUtility.InstantiatePrefab(framePrefab, scene);
            frameD.name = "Frame_Imp_D";
            frameD.transform.SetParent(imperfectGroup.transform);
            frameD.transform.position = new Vector3(8.55f, 0f, 2.95f);
            frameD.transform.rotation = Quaternion.Euler(0f, 95f, 0f);

            // Platform resting loosely on skewed frames
            GameObject platformImp = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, scene);
            platformImp.name = "Platform_Imp";
            platformImp.transform.SetParent(imperfectGroup.transform);
            platformImp.transform.position = new Vector3(8.52f, 1.85f, 2.05f);
            platformImp.transform.rotation = Quaternion.Euler(0f, 3.5f, 0f);

            // 3. Test Player Dummy for traversal testing (near standard assembly)
            GameObject playerDummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerDummy.name = "TestPlayerDummy";
            playerDummy.transform.SetParent(showcaseRoot.transform);
            playerDummy.transform.position = new Vector3(6f, 0.9f, 0f);
            playerDummy.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f); // Capsule total height 1.8m

            Rigidbody dummyRb = playerDummy.AddComponent<Rigidbody>();
            dummyRb.mass = 75f;
            dummyRb.interpolation = RigidbodyInterpolation.Interpolate;
            dummyRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            dummyRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            playerDummy.AddComponent<TestScaffoldingWalker>();

            // Distinct player visual color (cyan)
            Material dummyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = new Color(0.1f, 0.7f, 0.8f, 1f)
            };
            playerDummy.GetComponent<MeshRenderer>().material = dummyMat;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ScaffoldingBuilder] Saved updated scene with Scaffolding_Showcase and TestPlayerDummy.");
        }

        public static void VerifyPhysicsSimulation()
        {
            Debug.Log("[ScaffoldingVerifier] Opening scene for simulation test...");
            Scene scene = EditorSceneManager.OpenScene(ScenePath);

            GameObject platformStd = GameObject.Find("Platform_Std");
            GameObject platformImp = GameObject.Find("Platform_Imp");
            GameObject frameA = GameObject.Find("Frame_Std_A");
            GameObject dummy = GameObject.Find("TestPlayerDummy");

            if (platformStd == null || platformImp == null || frameA == null || dummy == null)
            {
                Debug.LogError("[VERIFY_FAIL] Required showcase objects not found in scene!");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[ScaffoldingVerifier] Simulating 5.0 seconds of resting physics...");
            Physics.simulationMode = SimulationMode.Script;
            for (int i = 0; i < 250; i++)
            {
                Physics.Simulate(0.02f);
            }

            Vector3 stdPos = platformStd.transform.position;
            Vector3 impPos = platformImp.transform.position;
            Debug.Log($"[ScaffoldingVerifier] Resting Std Platform Y = {stdPos.y:F3}, Imp Platform Y = {impPos.y:F3}");

            if (stdPos.y < 1.7f || impPos.y < 1.7f)
            {
                Debug.LogError($"[VERIFY_FAIL] Platforms slipped or fell below frame height! Std Y={stdPos.y:F3}, Imp Y={impPos.y:F3}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[ScaffoldingVerifier] Placing test player on top of platform for load test...");
            dummy.transform.position = new Vector3(6f, 2.85f, 2.0f); // Stand on top of Platform_Std
            Rigidbody dummyRb = dummy.GetComponent<Rigidbody>();
            dummyRb.linearVelocity = Vector3.zero;

            Debug.Log("[ScaffoldingVerifier] Simulating 5.0 seconds under player load...");
            for (int i = 0; i < 250; i++)
            {
                Physics.Simulate(0.02f);
            }

            Vector3 loadedPlatformPos = platformStd.transform.position;
            Vector3 dummyPos = dummy.transform.position;
            Debug.Log($"[ScaffoldingVerifier] Under load: Platform Y = {loadedPlatformPos.y:F3}, Dummy Y = {dummyPos.y:F3}");

            if (loadedPlatformPos.y < 1.7f)
            {
                Debug.LogError($"[VERIFY_FAIL] Platform collapsed under player weight! Y={loadedPlatformPos.y:F3}");
                EditorApplication.Exit(1);
                return;
            }

            if (dummyPos.y < 2.0f)
            {
                Debug.LogError($"[VERIFY_FAIL] Player fell through the platform! Dummy Y={dummyPos.y:F3}");
                EditorApplication.Exit(1);
                return;
            }

            // Restore simulation mode
            Physics.simulationMode = SimulationMode.FixedUpdate;

            Debug.Log("[VERIFY_PASS] Scaffolding modular assembly, collider stability, and player load tests PASSED!");
            EditorApplication.Exit(0);
        }
    }
}
