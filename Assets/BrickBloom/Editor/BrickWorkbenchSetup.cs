using System;
using System.IO;
using System.Linq;
using BrickBloom.Presentation;
using BrickBloom.XR;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrickBloom.Editor
{
    // Explicit batch-mode entry point for the offline brick workbench (phase 3, step 1).
    // Creates materials and piece prefabs when missing and adds the workbench to the existing XRFoundation scene.
    public static class BrickWorkbenchSetup
    {
        private const string ModelFolder = "Assets/BrickBloom/Models/Kenney";
        private const string MaterialFolder = "Assets/BrickBloom/Materials";
        private const string PrefabFolder = "Assets/BrickBloom/Prefabs/Bricks";
        private const string BlueprintPath = "Assets/BrickBloom/Data/Blueprints/bridge-001.json";
        private const string WorkbenchName = "BrickWorkbench";
        // Kenney Brick Kit: a 2-stud side measures 0.159 model units for a nominal 0.16 (0.08 per stud).
        private const float ModelShortSideRatio = 0.159f / 0.16f;
        private const float GrabPadding = 0.01f;

        private static float Pitch => (float)BrickDefinition.StudPitch;
        private static float LayerHeight => (float)BrickDefinition.LayerHeight;

        public static void Configure()
        {
            try
            {
                XRFoundationSetup.RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                if (GameObject.Find(WorkbenchName) != null)
                    throw new InvalidOperationException("BrickWorkbench already exists. Review/edit it instead of regenerating.");

                EnsureFolder(MaterialFolder);
                EnsureFolder(PrefabFolder);

                var red = BrickMaterial("Brick_Red", new Color(0.85f, 0.12f, 0.1f));
                var white = BrickMaterial("Brick_White", new Color(0.93f, 0.93f, 0.9f));
                var blue = BrickMaterial("Brick_Blue", new Color(0.1f, 0.35f, 0.9f));
                var yellow = BrickMaterial("Brick_Yellow", new Color(0.98f, 0.8f, 0.1f));
                var boardMaterial = BrickMaterial("Board_Gray", new Color(0.55f, 0.57f, 0.6f));
                var highlightMaterial = HighlightMaterial();

                var twoByTwo = PiecePrefab("BrickPiece_2x2", "bevel-hq-brick-2x2.fbx", 2, white, highlightMaterial);
                var twoByFour = PiecePrefab("BrickPiece_2x4", "bevel-hq-brick-2x4.fbx", 4, white, highlightMaterial);

                var blueprint = AssetDatabase.LoadAssetAtPath<TextAsset>(BlueprintPath);
                if (blueprint == null) throw new InvalidOperationException("Blueprint TextAsset missing: " + BlueprintPath);
                if (!BlueprintLoader.TryLoad(blueprint.text, out var parsed, out var error))
                    throw new InvalidOperationException(error);

                var rigs = UnityEngine.Object.FindObjectsByType<OVRCameraRig>(FindObjectsSortMode.None);
                if (rigs.Length != 1) throw new InvalidOperationException("Expected exactly 1 OVRCameraRig.");

                // Default pose matches the old cube workbench; WorkbenchPlacer moves it in front of the user at runtime.
                var workbench = new GameObject(WorkbenchName);
                workbench.transform.SetPositionAndRotation(new Vector3(0f, 0.78f, 0.55f), Quaternion.identity);

                float boardWidth = parsed.boardWidth * Pitch, boardDepth = parsed.boardDepth * Pitch;
                var origin = new GameObject("BoardOrigin");
                origin.transform.SetParent(workbench.transform, false);
                origin.transform.localPosition = new Vector3(-boardWidth / 2f, 0f, -boardDepth / 2f);

                BuildBoardVisual(workbench.transform, parsed.boardWidth, parsed.boardDepth, boardMaterial);

                var session = workbench.AddComponent<BrickWorkbench>();
                session.Configure(blueprint, origin.transform, twoByTwo, twoByFour, red, white, blue, yellow);
                EditorUtility.SetDirty(session);

                var placer = workbench.AddComponent<WorkbenchPlacer>();
                placer.Configure(rigs[0].centerEyeAnchor, workbench.transform);
                EditorUtility.SetDirty(placer);

                if (GameObject.Find("KeyLight") == null)
                {
                    var lightObject = new GameObject("KeyLight");
                    lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    var keyLight = lightObject.AddComponent<Light>();
                    keyLight.type = LightType.Directional;
                    keyLight.intensity = 1f;
                    keyLight.shadows = LightShadows.None;
                }

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

                // Keep the Gate B cube for reference, but out of the way of the workbench.
                var cube = UnityEngine.Object.FindObjectsByType<GateBCubeDiagnostics>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (cube.Length == 1 && cube[0].transform.parent != null)
                    cube[0].transform.parent.gameObject.SetActive(false);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, XRFoundationSetup.ScenePath);
                AssetDatabase.SaveAssets();
                XRFoundationSetup.CheckScene();
                Debug.Log("BrickBloom brick workbench configured (hardware verification pending).");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static Material BrickMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) throw new InvalidOperationException("URP Simple Lit shader missing.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.5f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Phase 3 step 2: adds the ghost target renderer to the existing workbench. Snapping itself is code only.
        public static void ConfigureSnapping()
        {
            try
            {
                XRFoundationSetup.RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                var workbenches = UnityEngine.Object.FindObjectsByType<BrickWorkbench>(FindObjectsSortMode.None);
                if (workbenches.Length != 1) throw new InvalidOperationException("Expected exactly 1 BrickWorkbench.");
                var workbench = workbenches[0];
                if (workbench.GetComponent<BrickGhostRenderer>() != null)
                    throw new InvalidOperationException("BrickGhostRenderer already exists. Review/edit it instead of regenerating.");

                string prefabPath = $"{PrefabFolder}/BrickGhost.prefab";
                if (File.Exists(prefabPath)) throw new InvalidOperationException(prefabPath + " already exists.");
                var ghostMaterial = TransparentUnlitMaterial("BrickGhost", new Color(1f, 1f, 1f, 0.35f));
                var ghostObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghostObject.name = "BrickGhost";
                UnityEngine.Object.DestroyImmediate(ghostObject.GetComponent<Collider>());
                var ghostRenderer = ghostObject.GetComponent<MeshRenderer>();
                ghostRenderer.sharedMaterial = ghostMaterial;
                ghostRenderer.shadowCastingMode = ShadowCastingMode.Off;
                var ghostPrefab = PrefabUtility.SaveAsPrefabAsset(ghostObject, prefabPath);
                UnityEngine.Object.DestroyImmediate(ghostObject);
                if (ghostPrefab == null) throw new InvalidOperationException("Failed to save " + prefabPath);

                var ghosts = workbench.gameObject.AddComponent<BrickGhostRenderer>();
                ghosts.Configure(workbench, ghostPrefab.GetComponent<Renderer>());
                EditorUtility.SetDirty(ghosts);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, XRFoundationSetup.ScenePath);
                AssetDatabase.SaveAssets();
                XRFoundationSetup.CheckScene();
                Debug.Log("BrickBloom snapping ghosts configured (hardware verification pending).");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        // Phase 3 step 3: hold-to-reset button, progress pips and completion feedback on the existing workbench.
        public static void ConfigureSessionControls()
        {
            try
            {
                XRFoundationSetup.RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                var workbenches = UnityEngine.Object.FindObjectsByType<BrickWorkbench>(FindObjectsSortMode.None);
                if (workbenches.Length != 1) throw new InvalidOperationException("Expected exactly 1 BrickWorkbench.");
                var workbench = workbenches[0];
                if (workbench.GetComponent<BuildProgressDisplay>() != null ||
                    UnityEngine.Object.FindObjectsByType<ResetDwellButton>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                    throw new InvalidOperationException("Session controls already exist. Review/edit them instead of regenerating.");

                string pipPath = $"{PrefabFolder}/ProgressPip.prefab";
                if (File.Exists(pipPath)) throw new InvalidOperationException(pipPath + " already exists.");
                if (!BlueprintLoader.TryLoad(AssetDatabase.LoadAssetAtPath<TextAsset>(BlueprintPath).text, out var parsed, out var error))
                    throw new InvalidOperationException(error);

                var pipObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pipObject.name = "ProgressPip";
                UnityEngine.Object.DestroyImmediate(pipObject.GetComponent<Collider>());
                var pipRenderer = pipObject.GetComponent<MeshRenderer>();
                pipRenderer.sharedMaterial = OpaqueUnlitMaterial("ProgressPip", new Color(0.22f, 0.22f, 0.25f));
                pipRenderer.shadowCastingMode = ShadowCastingMode.Off;
                var pipPrefab = PrefabUtility.SaveAsPrefabAsset(pipObject, pipPath);
                UnityEngine.Object.DestroyImmediate(pipObject);
                if (pipPrefab == null) throw new InvalidOperationException("Failed to save " + pipPath);

                var progress = workbench.gameObject.AddComponent<BuildProgressDisplay>();
                progress.Configure(workbench, pipPrefab.GetComponent<Renderer>());
                EditorUtility.SetDirty(progress);

                var hands = UnityEngine.Object.FindObjectsByType<HandGrabInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .OrderBy(h => h.name, StringComparer.Ordinal).ToArray();
                if (hands.Length < 2) throw new InvalidOperationException("Expected at least 2 HandGrabInteractors in the interaction rig.");

                // Left of the board on the near side; the supply area is on the right.
                float halfWidth = parsed.boardWidth * Pitch / 2f, halfDepth = parsed.boardDepth * Pitch / 2f;
                var button = new GameObject("ResetButton");
                button.transform.SetParent(workbench.transform, false);
                button.transform.localPosition = new Vector3(-halfWidth - 0.06f, 0f, -halfDepth + 0.03f);

                var baseRenderer = ButtonPart("Base", button.transform, new Vector3(0f, 0.005f, 0f), new Vector3(0.06f, 0.01f, 0.06f));
                baseRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Board_Gray.mat");
                if (baseRenderer.sharedMaterial == null) throw new InvalidOperationException("Board_Gray material missing.");
                var capRenderer = ButtonPart("Cap", button.transform, new Vector3(0f, 0.016f, 0f), new Vector3(0.045f, 0.012f, 0.045f));
                capRenderer.sharedMaterial = OpaqueUnlitMaterial("ResetButton", new Color(1f, 0.45f, 0.1f));

                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null && font.material != null)
                {
                    // Flat on the table, between the button and the user.
                    var label = new GameObject("Label");
                    label.transform.SetParent(button.transform, false);
                    label.transform.localPosition = new Vector3(0f, 0.001f, -0.045f);
                    label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    label.transform.localScale = Vector3.one * 0.002f;
                    var text = label.AddComponent<TextMesh>();
                    text.text = "RESET";
                    text.font = font;
                    text.fontSize = 64;
                    text.characterSize = 1f;
                    text.anchor = TextAnchor.MiddleCenter;
                    text.alignment = TextAlignment.Center;
                    text.color = Color.white;
                    label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
                else
                {
                    Debug.LogWarning("BrickBloom: built-in font unavailable; reset button has no label.");
                }

                var reset = button.AddComponent<ResetDwellButton>();
                reset.Configure(workbench, hands, capRenderer.transform, capRenderer);
                EditorUtility.SetDirty(reset);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, XRFoundationSetup.ScenePath);
                AssetDatabase.SaveAssets();
                XRFoundationSetup.CheckScene();
                Debug.Log($"BrickBloom session controls configured with {hands.Length} hand interactors (hardware verification pending).");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        // Raises the existing reset button so the fingertip does not have to reach down to table level.
        public static void RaiseResetButton()
        {
            try
            {
                XRFoundationSetup.RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                var buttons = UnityEngine.Object.FindObjectsByType<ResetDwellButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (buttons.Length != 1) throw new InvalidOperationException("Expected exactly 1 ResetDwellButton.");
                var button = buttons[0];
                var basePart = button.transform.Find("Base");
                var cap = button.transform.Find("Cap");
                if (basePart == null || cap == null) throw new InvalidOperationException("ResetButton Base or Cap missing.");

                const float baseHeight = 0.04f, capHeight = 0.012f, dwellSeconds = 0.8f;
                basePart.localPosition = new Vector3(0f, baseHeight / 2f, 0f);
                basePart.localScale = new Vector3(0.06f, baseHeight, 0.06f);
                cap.localPosition = new Vector3(0f, baseHeight + capHeight / 2f, 0f);

                var serialized = new SerializedObject(button);
                var touchSize = serialized.FindProperty("_touchSize");
                if (touchSize == null) throw new InvalidOperationException("ResetDwellButton._touchSize not found.");
                // From the base up to 4 cm above the cap top.
                touchSize.vector3Value = new Vector3(0.07f, baseHeight + capHeight + 0.04f, 0.07f);
                var dwell = serialized.FindProperty("_dwellSeconds");
                if (dwell == null) throw new InvalidOperationException("ResetDwellButton._dwellSeconds not found.");
                dwell.floatValue = dwellSeconds;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(button);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, XRFoundationSetup.ScenePath);
                XRFoundationSetup.CheckScene();
                Debug.Log("BrickBloom reset button raised (hardware verification pending).");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static MeshRenderer ButtonPart(string name, Transform parent, Vector3 localPosition, Vector3 size)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            var partRenderer = part.GetComponent<MeshRenderer>();
            partRenderer.shadowCastingMode = ShadowCastingMode.Off;
            return partRenderer;
        }

        private static Material OpaqueUnlitMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader missing.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material HighlightMaterial() =>
            TransparentUnlitMaterial("GrabHighlight", new Color(1f, 0.85f, 0.1f, 0.45f));

        private static Material TransparentUnlitMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit shader missing.");
            var material = new Material(shader) { name = name };
            // Alpha-blended surface. Destination alpha stays opaque over bricks so passthrough does not bleed through them.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Root is the body centre at unit scale. studsAlongZ is the long side at turns 0 (schema: 2x4 is 2 along X, 4 along Z).
        private static BrickPiece PiecePrefab(string name, string modelFile, int studsAlongZ, Material bodyMaterial, Material highlightMaterial)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            if (File.Exists(path)) throw new InvalidOperationException(path + " already exists. Review/edit it instead of regenerating.");

            var root = new GameObject(name);
            var bodySize = new Vector3(2 * Pitch, LayerHeight, studsAlongZ * Pitch);

            var visual = InstantiateModel(modelFile, root.transform, "Visual");
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var modelRenderer in renderers) modelRenderer.sharedMaterial = bodyMaterial;
            // Fit the model so its bottom sits on the body bottom and its footprint is centred on the root.
            FitModel(visual.transform, renderers, studsAlongZ, -LayerHeight / 2f, alignTop: false);

            var collider = root.AddComponent<BoxCollider>();
            collider.size = bodySize + Vector3.one * GrabPadding;
            collider.center = Vector3.zero;
            collider.isTrigger = false;

            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;

            Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddGrabInteraction(root);
            var handGrabInteractable = root.GetComponentInChildren<HandGrabInteractable>(true);
            var grabbable = root.GetComponent<Grabbable>();
            if (handGrabInteractable == null || grabbable == null)
                throw new InvalidOperationException("Grab interaction components were not created for " + name);

            var freeTransformer = root.GetComponent<GrabFreeTransformer>();
            if (freeTransformer == null) freeTransformer = root.AddComponent<GrabFreeTransformer>();

            grabbable.InjectOptionalTargetTransform(root.transform);
            grabbable.InjectOptionalRigidbody(body);
            grabbable.InjectOptionalKinematicWhileSelected(true);
            grabbable.InjectOptionalThrowWhenUnselected(false);
            grabbable.ForceKinematicDisabled = false;
            grabbable.MaxGrabPoints = 1;
            grabbable.InjectOptionalOneGrabTransformer(freeTransformer);
            grabbable.InjectOptionalTwoGrabTransformer(null);

            handGrabInteractable.InjectRigidbody(body);
            handGrabInteractable.InjectOptionalPointableElement(grabbable);
            handGrabInteractable.InjectSupportedGrabTypes(GrabTypeFlags.Pinch | GrabTypeFlags.Palm);
            handGrabInteractable.MaxSelectingInteractors = 1;
            handGrabInteractable.ResetGrabOnGrabsUpdated = true;

            var grabInteractable = handGrabInteractable.GetComponent<GrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.InjectRigidbody(body);
                grabInteractable.InjectOptionalPointableElement(grabbable);
                grabInteractable.MaxSelectingInteractors = 1;
                grabInteractable.ResetGrabOnGrabsUpdated = true;
            }

            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "GrabHighlight";
            UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());
            shell.transform.SetParent(root.transform, false);
            shell.transform.localScale = collider.size;
            var shellRenderer = shell.GetComponent<MeshRenderer>();
            shellRenderer.sharedMaterial = highlightMaterial;
            shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shellRenderer.enabled = false;

            var feedback = root.AddComponent<GrabStateColorFeedback>();
            feedback.Configure(handGrabInteractable, shellRenderer);
            feedback.ConfigureHighlightShell(new Color(1f, 0.85f, 0.1f, 0.45f), new Color(0.2f, 0.9f, 0.3f, 0.3f));

            var piece = root.AddComponent<BrickPiece>();
            piece.Configure(renderers, body);

            var adapter = root.AddComponent<BrickGrabAdapter>();
            adapter.Configure(piece, grabbable, handGrabInteractable, body);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null) throw new InvalidOperationException("Failed to save " + path);
            return prefab.GetComponent<BrickPiece>();
        }

        // Two 4x8 stud plates side by side; the plate top surface (studs excluded) is the board's Y = 0 plane.
        private static void BuildBoardVisual(Transform workbench, int boardWidth, int boardDepth, Material material)
        {
            if (boardWidth != 8 || boardDepth != 8)
                throw new InvalidOperationException("Board visual currently supports the 8x8 MVP board only.");
            var visualRoot = new GameObject("BoardVisual");
            visualRoot.transform.SetParent(workbench, false);
            for (int i = 0; i < 2; i++)
            {
                var half = new GameObject("Plate_" + i);
                half.transform.SetParent(visualRoot.transform, false);
                half.transform.localPosition = new Vector3((i == 0 ? -1f : 1f) * 2f * Pitch, 0f, 0f);
                var plate = InstantiateModel("bevel-hq-plate-4x8.fbx", half.transform, "Visual");
                var renderers = plate.GetComponentsInChildren<Renderer>(true);
                foreach (var plateRenderer in renderers)
                {
                    plateRenderer.sharedMaterial = material;
                    plateRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                // 4 studs along X, 8 along Z; stud tops end one stud height above the Y = 0 plane.
                FitModel(plate.transform, renderers, 8, 0f, alignTop: true, shortSideStuds: 4);
            }
        }

        private static GameObject InstantiateModel(string modelFile, Transform parent, string name)
        {
            string path = $"{ModelFolder}/{modelFile}";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new InvalidOperationException("Model missing or not imported: " + path);
            // Plain copy, not a prefab instance, so piece prefabs do not depend on FBX import structure.
            var instance = UnityEngine.Object.Instantiate(model, parent);
            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            foreach (var modelCollider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(modelCollider);
            if (instance.GetComponentsInChildren<Renderer>(true).Length == 0)
                throw new InvalidOperationException("Model has no renderer: " + path);
            return instance;
        }

        // Measures the imported mesh instead of trusting FBX unit settings. The parent must be at identity pose.
        // alignTop=false: model bottom at anchorY. alignTop=true: body top (stud height removed) at anchorY.
        private static void FitModel(Transform model, Renderer[] renderers, int longSideStuds, float anchorY,
            bool alignTop, int shortSideStuds = 2)
        {
            Transform parent = model.parent;
            Vector3 parentPosition = parent.position;
            Quaternion parentRotation = parent.rotation;
            Transform grandParent = parent.parent;
            // Temporarily measure at the world origin so renderer bounds are parent-local.
            parent.SetParent(null, false);
            parent.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            try
            {
                Bounds bounds = Encapsulate(renderers);
                // Blueprint schema wants the long side along Z; rotate if the model's long side is along X.
                if (longSideStuds != shortSideStuds && bounds.size.x > bounds.size.z)
                {
                    model.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    bounds = Encapsulate(renderers);
                }

                float shortSide = Mathf.Min(bounds.size.x, bounds.size.z);
                if (shortSide <= 0f) throw new InvalidOperationException("Model has an empty footprint.");
                float scale = shortSideStuds * Pitch * ModelShortSideRatio / shortSide;
                model.localScale = Vector3.one * scale;
                bounds = Encapsulate(renderers);

                float expectedLong = longSideStuds * Pitch;
                float actualLong = Mathf.Max(bounds.size.x, bounds.size.z);
                if (Mathf.Abs(actualLong - expectedLong) > 0.004f)
                    throw new InvalidOperationException($"Model long side {actualLong:F4} m does not match {expectedLong:F4} m.");

                float y = alignTop
                    ? anchorY - (bounds.max.y - StudHeight(scale, shortSide))
                    : anchorY - bounds.min.y;
                model.localPosition += new Vector3(-bounds.center.x, y, -bounds.center.z);
                Bounds fitted = Encapsulate(renderers);
                Debug.Log($"BrickBloom fitted model {model.parent.name}/{model.name}: scale={scale:F4} size=({fitted.size.x:F4},{fitted.size.y:F4},{fitted.size.z:F4}) minY={fitted.min.y:F4} maxY={fitted.max.y:F4}");
            }
            finally
            {
                parent.SetParent(grandParent, false);
                parent.SetPositionAndRotation(parentPosition, parentRotation);
            }
        }

        // Kenney studs are 0.018 model units tall for a 0.159 two-stud side.
        private static float StudHeight(float scale, float unscaledShortSide) =>
            0.018f / 0.159f * 2f * Pitch * ModelShortSideRatio;

        private static Bounds Encapsulate(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var item in renderers.Skip(1)) bounds.Encapsulate(item.bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
