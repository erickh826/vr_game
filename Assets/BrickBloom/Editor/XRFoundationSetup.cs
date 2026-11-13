using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BrickBloom.XR;
using Meta.XR;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;

namespace BrickBloom.Editor
{
    // Explicit batch-mode entry points. Existing scenes are never regenerated.
    public static class XRFoundationSetup
    {
        public const string ScenePath = "Assets/BrickBloom/Scenes/XRFoundation.unity";
        public const string ComprehensiveInteractorsPath = "Assets/BrickBloom/Prefabs/XR/ComprehensiveInteractors.prefab";
        private const string ComprehensiveInteractorsRelativePrefabPath = "BrickBloom/Prefabs/XR/ComprehensiveInteractors.prefab";
        private const string PipelinePath = "Assets/BrickBloom/Settings/Quest_RPAsset.asset";
        private const string RendererPath = "Assets/BrickBloom/Settings/Quest_Renderer.asset";
        private const string ProfilePath = "Assets/BrickBloom/BuildProfiles/QuestDevelopment.asset";
        private const string RigPath = "Assets/BrickBloom/Prefabs/XR/QuestCameraRig.prefab";
        private const string ApkPath = "Builds/Quest/BrickBloomMR-XRFoundation.apk";

        public static void CreateAndValidate()
        {
            try
            {
                RequireEnvironment();
                if (File.Exists(ScenePath) || File.Exists(RigPath) || File.Exists(PipelinePath))
                    throw new InvalidOperationException("Foundation assets already exist. Review/edit them instead of regenerating.");
                Folder("Assets/BrickBloom/Settings");
                Folder("Assets/BrickBloom/Prefabs/XR");
                Folder("Assets/BrickBloom/Scenes");
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.renderingMode = RenderingMode.Forward;
                renderer.intermediateTextureMode = IntermediateTextureMode.Auto;
                AssetDatabase.CreateAsset(renderer, RendererPath);
                var pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 4;
                pipeline.renderScale = 1f;
                // Read-only public property; field verified in URP 17.0.4 source.
                var pipelineSerialized = new SerializedObject(pipeline);
                pipelineSerialized.FindProperty("m_MainLightShadowsSupported").boolValue = false;
                pipelineSerialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
                pipelineSerialized.FindProperty("m_SupportsTerrainHoles").boolValue = false;
                pipelineSerialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                int mobile = Array.IndexOf(QualitySettings.names, "Mobile");
                if (mobile < 0) throw new InvalidOperationException("Expected existing Mobile quality tier.");
                int previousQuality = QualitySettings.GetQualityLevel();
                QualitySettings.SetQualityLevel(mobile, false);
                QualitySettings.renderPipeline = pipeline;
                QualitySettings.SetQualityLevel(previousQuality, false);

                var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                xr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                var subsampled = xr.GetFeature<MetaXRSubsampledLayout>();
                if (subsampled == null) throw new InvalidOperationException("Installed subsampled layout feature missing.");
                subsampled.enabled = true;
                EditorUtility.SetDirty(subsampled);
                EditorUtility.SetDirty(xr);
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.skybox = null;
                RenderSettings.fog = false;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
                if (source == null) throw new InvalidOperationException("Installed OVRCameraRig prefab missing.");
                var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(source);
                rigObject.name = "QuestCameraRig";
                var manager = rigObject.GetComponent<OVRManager>();
                manager.trackingOriginType = OVRManager.TrackingOrigin.Stage;
                manager.isInsightPassthroughEnabled = true;
                var layer = rigObject.AddComponent<OVRPassthroughLayer>();
                layer.hidden = false;
                layer.textureOpacity = 1f;
                var rig = rigObject.GetComponent<OVRCameraRig>();
                foreach (var camera in rigObject.GetComponentsInChildren<Camera>(true))
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.clear;
                    camera.allowHDR = false;
                    camera.allowMSAA = true;
                    camera.nearClipPlane = 0.05f;
                    camera.farClipPlane = 20f;
                    var data = camera.GetUniversalAdditionalCameraData();
                    data.renderPostProcessing = false;
                    data.renderShadows = false;
                    camera.enabled = camera.transform == rig.centerEyeAnchor;
                }
                PrefabUtility.SaveAsPrefabAssetAndConnect(rigObject, RigPath, InteractionMode.AutomatedAction);

                var bench = new GameObject("WorkbenchRoot");
                // Serialized stage-relative diagnostic pose, adjustable in Inspector; no runtime writer.
                bench.transform.localPosition = new Vector3(0f, 0.78f, 0.55f);
                var cube = new GameObject("CubeRoot");
                cube.transform.SetParent(bench.transform, false);
                cube.AddComponent<BoxCollider>().size = Vector3.one * 0.08f;
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(cube.transform, false);
                visual.transform.localScale = Vector3.one * 0.08f;
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) throw new InvalidOperationException("URP Unlit shader missing.");
                var material = new Material(shader) { name = "GateA_Blue" };
                material.SetColor("_BaseColor", new Color(0.08f, 0.4f, 1f, 1f));
                AssetDatabase.CreateAsset(material, "Assets/BrickBloom/Materials/GateA_Blue.mat");
                visual.GetComponent<Renderer>().sharedMaterial = material;
                EditorSceneManager.SaveScene(scene, ScenePath);
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
                profile.overrideGlobalScenes = true;
                profile.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                CreateManifestIfMissing();
                CheckScene();
                AndroidXRSetup.ValidateOnly();
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        public static void ConfigureGateBAndValidate()
        {
            try
            {
                RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var rigs = UnityEngine.Object.FindObjectsByType<OVRCameraRig>(FindObjectsSortMode.None);
                if (rigs.Length != 1)
                {
                    throw new InvalidOperationException($"Expected exactly 1 OVRCameraRig in {ScenePath}, found {rigs.Length}.");
                }

                var cameraRig = rigs[0];
                Folder("Assets/BrickBloom/Prefabs/XR");

                var rigRef = UnityEngine.Object.FindFirstObjectByType<OVRCameraRigRef>();
                if (rigRef == null)
                {
                    var wizardType = Type.GetType(
                        "Oculus.Interaction.OVR.Editor.QuickActions.OVRComprehensiveInteractionRigWizard, Oculus.Interaction.OVR.Editor",
                        throwOnError: true);
                    var wizard = ScriptableObject.CreateInstance(wizardType);
                    try
                    {
                        wizardType.GetMethod("InjectCameraRig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .Invoke(wizard, new object[] { cameraRig });
                        wizardType.GetMethod("InjectOptionalGenerateAsEditableCopy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .Invoke(wizard, new object[] { true });
                        wizardType.GetMethod("InjectOptionalPrefabPath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .Invoke(wizard, new object[] { ComprehensiveInteractorsRelativePrefabPath });
                        wizardType.GetMethod("CreateInteractionRig", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .Invoke(wizard, Array.Empty<object>());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(wizard);
                    }

                    rigRef = UnityEngine.Object.FindFirstObjectByType<OVRCameraRigRef>();
                }

                if (rigRef == null)
                {
                    throw new InvalidOperationException("Failed to create or locate OVRCameraRigRef under QuestCameraRig.");
                }

                WireInteractionRig(cameraRig, rigRef);

                var cubeRoot = GameObject.Find("CubeRoot");
                if (cubeRoot == null)
                {
                    throw new InvalidOperationException("CubeRoot missing from XRFoundation scene.");
                }

                ConfigureGrabbableCube(cubeRoot);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                CreateManifestIfMissing();
                CheckScene();
                AndroidXRSetup.ValidateOnly();
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void WireInteractionRig(OVRCameraRig cameraRig, OVRCameraRigRef rigRef)
        {
            var hands = rigRef.GetComponentsInChildren<OVRHand>(true);
            var leftHand = hands.FirstOrDefault(h => h.GetHand() == OVRPlugin.Hand.HandLeft);
            var rightHand = hands.FirstOrDefault(h => h.GetHand() == OVRPlugin.Hand.HandRight);
            if (leftHand == null || rightHand == null)
            {
                throw new InvalidOperationException("Expected left and right OVRHand components under OVRComprehensiveInteractionRig.");
            }

            var rigRefSerialized = new SerializedObject(rigRef);
            rigRefSerialized.FindProperty("_ovrCameraRig").objectReferenceValue = cameraRig;
            rigRefSerialized.FindProperty("_leftHand").objectReferenceValue = leftHand;
            rigRefSerialized.FindProperty("_rightHand").objectReferenceValue = rightHand;
            rigRefSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rigRef);

            var transformer = rigRef.GetComponent<MonoBehaviour>();
            foreach (var mb in rigRef.GetComponents<MonoBehaviour>())
            {
                if (mb is ITrackingToWorldTransformer)
                {
                    transformer = mb;
                    break;
                }
            }

            foreach (var handSource in rigRef.GetComponentsInChildren<FromOVRHandDataSource>(true))
            {
                var hsSerialized = new SerializedObject(handSource);
                hsSerialized.FindProperty("_cameraRigRef").objectReferenceValue = rigRef;
                hsSerialized.FindProperty("_trackingToWorldTransformer").objectReferenceValue = transformer;
                hsSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(handSource);
            }

            foreach (var controllerSource in rigRef.GetComponentsInChildren<FromOVRControllerDataSource>(true))
            {
                var csSerialized = new SerializedObject(controllerSource);
                csSerialized.FindProperty("_cameraRigRef").objectReferenceValue = rigRef;
                csSerialized.FindProperty("_trackingToWorldTransformer").objectReferenceValue = transformer;
                csSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controllerSource);
            }
        }

        private static void ConfigureGrabbableCube(GameObject cubeRoot)
        {
            cubeRoot.transform.localPosition = Vector3.zero;
            cubeRoot.transform.localRotation = Quaternion.identity;
            cubeRoot.transform.localScale = Vector3.one;

            var collider = cubeRoot.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = cubeRoot.AddComponent<BoxCollider>();
            }
            collider.size = Vector3.one * 0.08f;
            collider.center = Vector3.zero;
            collider.isTrigger = false;
            EditorUtility.SetDirty(collider);

            var body = cubeRoot.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = cubeRoot.AddComponent<Rigidbody>();
            }
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            EditorUtility.SetDirty(body);

            var handGrabInteractable = cubeRoot.GetComponentInChildren<HandGrabInteractable>(true);
            if (handGrabInteractable == null)
            {
                Oculus.Interaction.Editor.QuickActions.QuickActionsAPI.AddGrabInteraction(cubeRoot);
                handGrabInteractable = cubeRoot.GetComponentInChildren<HandGrabInteractable>(true);
            }

            if (handGrabInteractable == null)
            {
                throw new InvalidOperationException("Failed to create HandGrabInteractable under CubeRoot.");
            }

            var grabbable = cubeRoot.GetComponent<Grabbable>();
            if (grabbable == null)
            {
                throw new InvalidOperationException("Expected Grabbable on CubeRoot.");
            }

            var freeTransformer = cubeRoot.GetComponent<GrabFreeTransformer>();
            if (freeTransformer == null)
            {
                freeTransformer = cubeRoot.AddComponent<GrabFreeTransformer>();
            }
            EditorUtility.SetDirty(freeTransformer);

            grabbable.InjectOptionalTargetTransform(cubeRoot.transform);
            grabbable.InjectOptionalRigidbody(body);
            grabbable.InjectOptionalKinematicWhileSelected(true);
            grabbable.InjectOptionalThrowWhenUnselected(false);
            grabbable.ForceKinematicDisabled = false;
            grabbable.MaxGrabPoints = 1;
            grabbable.InjectOptionalOneGrabTransformer(freeTransformer);
            grabbable.InjectOptionalTwoGrabTransformer(null);
            EditorUtility.SetDirty(grabbable);

            handGrabInteractable.InjectRigidbody(body);
            handGrabInteractable.InjectOptionalPointableElement(grabbable);
            handGrabInteractable.InjectSupportedGrabTypes(GrabTypeFlags.Pinch | GrabTypeFlags.Palm);
            handGrabInteractable.MaxSelectingInteractors = 1;
            handGrabInteractable.ResetGrabOnGrabsUpdated = true;
            EditorUtility.SetDirty(handGrabInteractable);

            var grabInteractable = handGrabInteractable.GetComponent<GrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.InjectRigidbody(body);
                grabInteractable.InjectOptionalPointableElement(grabbable);
                grabInteractable.MaxSelectingInteractors = 1;
                grabInteractable.ResetGrabOnGrabsUpdated = true;
                EditorUtility.SetDirty(grabInteractable);
            }

            var diagnostics = cubeRoot.GetComponent<GateBCubeDiagnostics>();
            if (diagnostics == null)
            {
                diagnostics = cubeRoot.AddComponent<GateBCubeDiagnostics>();
            }
            diagnostics.Configure(
                grabbable,
                handGrabInteractable,
                body,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one);
            EditorUtility.SetDirty(diagnostics);

            ConfigureGrabFeedback(cubeRoot, handGrabInteractable);
        }

        private static void ConfigureGrabFeedback(GameObject cubeRoot, HandGrabInteractable handGrabInteractable)
        {
            var visual = cubeRoot.transform.Find("Visual");
            var visualRenderer = visual != null ? visual.GetComponent<Renderer>() : null;
            if (visualRenderer == null)
            {
                throw new InvalidOperationException("CubeRoot/Visual renderer missing; cannot configure grab colour feedback.");
            }

            var feedback = cubeRoot.GetComponent<GrabStateColorFeedback>();
            if (feedback == null)
            {
                feedback = cubeRoot.AddComponent<GrabStateColorFeedback>();
            }
            feedback.Configure(handGrabInteractable, visualRenderer);
            EditorUtility.SetDirty(feedback);
        }

        // Narrow entry point: only adds/configures the grab colour feedback on the existing CubeRoot.
        public static void ConfigureGateBFeedback()
        {
            try
            {
                RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var cubeRoot = GameObject.Find("CubeRoot");
                if (cubeRoot == null)
                {
                    throw new InvalidOperationException("CubeRoot missing from XRFoundation scene.");
                }

                var handGrabInteractable = cubeRoot.GetComponentInChildren<HandGrabInteractable>(true);
                if (handGrabInteractable == null)
                {
                    throw new InvalidOperationException("HandGrabInteractable missing under CubeRoot.");
                }

                ConfigureGrabFeedback(cubeRoot, handGrabInteractable);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                CheckScene();
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        public static void CreateManifestAndValidate()
        {
            try
            {
                RequireEnvironment();
                CreateManifestIfMissing();
                ValidateSceneAndProject();
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void CreateManifestIfMissing()
        {
            // Core 207's required GameActivity rule inspects this source manifest,
            // even though Unity's generated Gradle manifest already uses GameActivity.
            if (!OVRManifestPreprocessor.DoesAndroidManifestExist())
                OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(true);
        }

        public static void ValidateSceneAndProject()
        {
            try
            {
                RequireEnvironment();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                CheckScene();
                AndroidXRSetup.ValidateOnly();
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        public static void BuildApk()
        {
            try
            {
                RequireEnvironment();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                CheckScene();
                Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, target = BuildTarget.Android,
                    locationPathName = ApkPath, options = BuildOptions.Development
                });
                Directory.CreateDirectory("docs/validation");
                var evidenceJson = JsonUtility.ToJson(new BuildEvidence
                {
                    generatedUtc = DateTime.UtcNow.ToString("O"), result = report.summary.result.ToString(),
                    outputPath = ApkPath, scene = ScenePath, errors = report.summary.totalErrors,
                    warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
                    seconds = report.summary.totalTime.TotalSeconds, hardwareVerified = false
                }, true) + "\n";
                File.WriteAllText("docs/validation/gate-b-build.json", evidenceJson);
                Debug.Log("BrickBloom APK build: " + report.summary.result);
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void RequireEnvironment()
        {
            if (!Application.isBatchMode || Application.unityVersion != "6000.0.85f1" ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Run in Unity 6000.0.85f1 batchmode with -buildTarget Android.");
            if (AssetDatabase.GetAssetPath(BuildProfile.GetActiveBuildProfile()) != ProfilePath)
                throw new InvalidOperationException("Activate QuestDevelopment using AndroidXRSetup first.");
        }

        private static void CheckScene()
        {
            var rigs = UnityEngine.Object.FindObjectsByType<OVRCameraRig>(FindObjectsSortMode.None);
            var layers = UnityEngine.Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsSortMode.None);
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
            var pipeline = QualitySettings.GetRenderPipelineAssetAt(Array.IndexOf(QualitySettings.names, "Mobile")) as UniversalRenderPipelineAsset;
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (rigs.Length != 1 || layers.Length != 1 || layers[0].hidden || cameras.Length != 1 ||
                cameras[0].backgroundColor != Color.clear || cameras[0].clearFlags != CameraClearFlags.SolidColor ||
                cameras[0].GetUniversalAdditionalCameraData().renderPostProcessing ||
                !rigs[0].GetComponent<OVRManager>().isInsightPassthroughEnabled ||
                pipeline == null || pipeline.supportsHDR || pipeline.msaaSampleCount != 4 || pipeline.renderScale != 1f ||
                !profile.overrideGlobalScenes || !profile.scenes.Where(s => s.enabled).Select(s => s.path).SequenceEqual(new[] { ScenePath }))
                throw new InvalidOperationException("Gate A scene/render/profile preflight failed.");

            var rigRefs = UnityEngine.Object.FindObjectsByType<OVRCameraRigRef>(FindObjectsSortMode.None);
            var handGrabInteractors = UnityEngine.Object.FindObjectsByType<HandGrabInteractor>(FindObjectsSortMode.None);
            var cubeRoot = GameObject.Find("CubeRoot");
            if (rigRefs.Length != 1 || rigRefs[0].transform.parent != rigs[0].transform ||
                !File.Exists(ComprehensiveInteractorsPath) || handGrabInteractors.Length < 2 || cubeRoot == null)
                throw new InvalidOperationException("Gate B interaction rig or CubeRoot preflight failed.");

            var body = cubeRoot.GetComponent<Rigidbody>();
            var collider = cubeRoot.GetComponent<BoxCollider>();
            var grabbable = cubeRoot.GetComponent<Grabbable>();
            var freeTransformer = cubeRoot.GetComponent<GrabFreeTransformer>();
            var handGrabInteractable = cubeRoot.GetComponentInChildren<HandGrabInteractable>(true);
            var diagnostics = cubeRoot.GetComponent<GateBCubeDiagnostics>();
            if (cubeRoot.transform.localScale != Vector3.one ||
                body == null || body.useGravity || !body.isKinematic ||
                collider == null || collider.isTrigger || collider.size != Vector3.one * 0.08f ||
                grabbable == null || grabbable.MaxGrabPoints != 1 || grabbable.ForceKinematicDisabled ||
                freeTransformer == null || handGrabInteractable == null ||
                handGrabInteractable.MaxSelectingInteractors != 1 ||
                handGrabInteractable.SupportedGrabTypes != (GrabTypeFlags.Pinch | GrabTypeFlags.Palm) ||
                !handGrabInteractable.ResetGrabOnGrabsUpdated ||
                diagnostics == null || cubeRoot.GetComponent<GrabStateColorFeedback>() == null)
                throw new InvalidOperationException("Gate B CubeRoot grab configuration preflight failed.");

            var grabbableSerialized = new SerializedObject(grabbable);
            if (!grabbableSerialized.FindProperty("_kinematicWhileSelected").boolValue ||
                grabbableSerialized.FindProperty("_throwWhenUnselected").boolValue ||
                grabbableSerialized.FindProperty("_oneGrabTransformer").objectReferenceValue != freeTransformer ||
                grabbableSerialized.FindProperty("_twoGrabTransformer").objectReferenceValue != null)
                throw new InvalidOperationException("Gate B Grabbable serialized properties preflight failed.");

            Debug.Log("BrickBloom Gate A + Gate B scene/render/profile/interaction preflight passed (Gate B hardware verification pending).");
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [Serializable] private sealed class BuildEvidence
        {
            public string generatedUtc, result, outputPath, scene;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
            public bool hardwareVerified;
        }
    }
}
