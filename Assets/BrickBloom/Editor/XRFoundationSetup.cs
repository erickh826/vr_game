using System;
using System.IO;
using System.Linq;
using Meta.XR;
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
                File.WriteAllText("docs/validation/gate-a-build.json", JsonUtility.ToJson(new BuildEvidence
                {
                    generatedUtc = DateTime.UtcNow.ToString("O"), result = report.summary.result.ToString(),
                    outputPath = ApkPath, scene = ScenePath, errors = report.summary.totalErrors,
                    warnings = report.summary.totalWarnings, bytes = report.summary.totalSize,
                    seconds = report.summary.totalTime.TotalSeconds, hardwareVerified = false
                }, true) + "\n");
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
            Debug.Log("BrickBloom Gate A scene/render/profile preflight passed (hardware verification pending).");
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
