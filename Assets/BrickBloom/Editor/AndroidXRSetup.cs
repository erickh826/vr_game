using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Meta.XR;
using Unity.XR.CoreUtils.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace BrickBloom.Editor
{
    // Editor-only setup for the pinned Unity/SDK versions. Never runs on import.
    public static class AndroidXRSetup
    {
        private const BuildTargetGroup Group = BuildTargetGroup.Android;
        private const string ProfilePath = "Assets/BrickBloom/BuildProfiles/QuestDevelopment.asset";
        private const string ReportPath = "docs/validation/android-xr.json";
        private const string PendingValidationKey = "BrickBloom.AndroidXR.PendingValidation";
        private static string metaReportPath;
        private const BindingFlags InternalInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public static void ApplyAndValidate()
        {
            try
            {
                RequireAndroid();
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.brickbloom.mr.dev");
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
                PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

                var store = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                    "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                if (store == null) throw new InvalidOperationException("Existing XR settings asset missing.");
                if (!store.HasSettingsForBuildTarget(Group)) store.CreateDefaultSettingsForBuildTarget(Group);
                if (!store.HasManagerSettingsForBuildTarget(Group)) store.CreateDefaultManagerSettingsForBuildTarget(Group);
                var settings = store.SettingsForBuildTarget(Group);
                if (settings.Manager.activeLoaders.Any(l => !(l is OpenXRLoader)))
                    throw new InvalidOperationException("Unexpected existing Android provider; review before changing it.");
                settings.InitManagerOnStart = true;
                settings.Manager.automaticLoading = true;
                settings.Manager.automaticRunning = true;
                if (!XRPackageMetadataStore.AssignLoader(settings.Manager, typeof(OpenXRLoader).FullName, Group))
                    throw new InvalidOperationException("Could not assign Android OpenXR loader.");
                EditorUtility.SetDirty(settings.Manager);
                EditorUtility.SetDirty(settings);
                EditorUtility.SetDirty(store);

                var config = OVRProjectConfig.CachedProjectConfig;
                config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
                config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
                config.systemLoadingScreenBackground = OVRProjectConfig.SystemLoadingScreenBackground.ContextualPassthrough;
                if (!config.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest3))
                    config.targetDeviceTypes.Add(OVRProjectConfig.DeviceType.Quest3);
                OVRProjectConfig.CommitProjectConfig(config);

                var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(Group);
                Enable<MetaXRFeature>(xr);
                Enable<HandTracking>(xr);
                Enable<MetaHandTrackingAim>(xr);
                // Meta's required OVRInput compatibility profile; gameplay remains HandsOnly.
                Enable<OculusTouchControllerProfile>(xr);
                EditorUtility.SetDirty(xr);
                SessionState.SetBool(PendingValidationKey, true);
                CreateDevelopmentProfile();
                AssetDatabase.SaveAssets();
                QueueValidation();
            }
            catch (Exception ex) { Fail(ex); }
        }

        public static void ValidateOnly()
        {
            try { RequireAndroid(); QueueValidation(); }
            catch (Exception ex) { Fail(ex); }
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterProfileReload()
        {
            if (SessionState.GetBool(PendingValidationKey, false))
                EditorApplication.delayCall += QueueValidation;
        }

        private static void RequireAndroid()
        {
            if (Application.unityVersion != "6000.0.85f1")
                throw new InvalidOperationException("Review version-specific Editor APIs before using another Unity version.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Launch Unity with -buildTarget Android; do not switch during this method.");
        }

        private static void Enable<T>(OpenXRSettings settings) where T : OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature == null) throw new InvalidOperationException("Missing installed feature: " + typeof(T).FullName);
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        private static void CreateDevelopmentProfile()
        {
            // Unity 6000.0 has no public profile-creation API. Verified against UnityCsReference
            // 6000.0 BuildProfileCreate.cs; fail closed if the exact overload is unavailable.
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                var create = typeof(BuildProfile).GetMethod("CreateInstance", BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] { typeof(BuildTarget), typeof(StandaloneBuildSubtarget) }, null);
                if (create == null) throw new MissingMethodException("BuildProfile.CreateInstance signature changed.");
                profile = (BuildProfile)create.Invoke(null, new object[] { BuildTarget.Android, StandaloneBuildSubtarget.Player });
                if (!AssetDatabase.IsValidFolder("Assets/BrickBloom/BuildProfiles"))
                    AssetDatabase.CreateFolder("Assets/BrickBloom", "BuildProfiles");
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var targetProperty = typeof(BuildProfile).GetProperty("buildTarget", InternalInstance);
            if (targetProperty == null || (BuildTarget)targetProperty.GetValue(profile) != BuildTarget.Android)
                throw new InvalidOperationException("Existing profile is not Android.");
            var platform = typeof(BuildProfile).GetProperty("platformBuildProfile", InternalInstance)?.GetValue(profile);
            if (platform == null) throw new InvalidOperationException("Android profile platform settings were not initialized.");
            var development = platform.GetType().GetProperty("development", InternalInstance);
            if (development == null) throw new MissingMemberException("Profile development property changed.");
            development.SetValue(platform, true);
            EditorUtility.SetDirty(profile);
            BuildProfile.SetActiveBuildProfile(profile);
            EditorUserBuildSettings.development = true;
        }

        private static void QueueValidation()
        {
            // Let package/task registration and feature changes settle before reporting.
            var packages = UnityEditor.PackageManager.Client.List(true, true);
            double deadline = EditorApplication.timeSinceStartup + 120;
            double earliest = EditorApplication.timeSinceStartup + 5;
            void Tick()
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Tick;
                    Fail(new TimeoutException("Package registration did not settle."));
                    return;
                }
                if (!packages.IsCompleted || EditorApplication.timeSinceStartup < earliest || EditorApplication.isCompiling)
                    return;
                EditorApplication.update -= Tick;
                try
                {
                    if (packages.Status != UnityEditor.PackageManager.StatusCode.Success)
                        throw new InvalidOperationException(packages.Error?.message);
                    WriteValidation();
                }
                catch (Exception ex) { Fail(ex); }
            }
            EditorApplication.update += Tick;
        }

        private static void WriteValidation()
        {
            Directory.CreateDirectory("docs/validation");
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-reportFile");
            if (index < 0 || index + 1 >= args.Length)
                throw new ArgumentException("Pass -reportFile docs/validation/android-meta.json.");
            string metaPath = args[index + 1];
            // Core 207's public CLI skips heavy tasks in its two-minute cooldown.
            // Use the verified internal overload to evaluate them too, without fixing/ignoring rules.
            metaReportPath = metaPath;
            var setup = typeof(OVRProjectSetupCLI).Assembly.GetType("OVRProjectSetup", true);
            var update = setup.GetMethod("UpdateTasks", BindingFlags.Static | BindingFlags.NonPublic);
            var parameters = update?.GetParameters();
            if (parameters == null || parameters.Length != 6 || parameters[5].Name != "bypassCooldown")
                throw new MissingMethodException("Meta UpdateTasks signature changed.");
            var callback = Delegate.CreateDelegate(parameters[4].ParameterType,
                typeof(AndroidXRSetup).GetMethod(nameof(SaveMetaReport), BindingFlags.Static | BindingFlags.NonPublic));
            update.Invoke(null, new object[] { Group, null, Enum.ToObject(parameters[2].ParameterType, 0), true, callback, true });
            var meta = JsonUtility.FromJson<MetaReport>(File.ReadAllText(metaPath));
            if (meta.buildTargetGroup != "Android" || meta.tasksStatus == null || meta.tasksStatus.Length == 0)
                throw new InvalidOperationException("Meta returned an empty or wrong-platform report.");

            var native = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(native, Group);
            // Core Utils 2.6.0 exposes BuildValidationRule but keeps collection internal.
            // Exact signature verified in installed source; no FixAll or ignored-rule mutation.
            var collect = typeof(BuildValidator).GetMethod("GetCurrentValidationIssues", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(HashSet<BuildValidationRule>), typeof(BuildTargetGroup) }, null);
            if (collect == null) throw new MissingMethodException("BuildValidator collection API changed.");
            var failures = new HashSet<BuildValidationRule>();
            collect.Invoke(null, new object[] { failures, Group });
            var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(Group);
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(Group);
            var config = OVRProjectConfig.CachedProjectConfig;
            var report = new ValidationReport
            {
                generatedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                activeTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                applicationId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                minimumApi = (int)PlayerSettings.Android.minSdkVersion, targetApi = (int)PlayerSettings.Android.targetSdkVersion,
                graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Select(x => x.ToString()).ToArray(),
                initializeOnStartup = general != null && general.InitManagerOnStart,
                loaders = general?.Manager?.activeLoaders.Select(x => x.GetType().FullName).ToArray() ?? Array.Empty<string>(),
                enabledFeatures = xr.GetFeatures<OpenXRFeature>().Where(x => x.enabled).Select(x => x.GetType().FullName).ToArray(),
                handTracking = config.handTrackingSupport.ToString(), passthrough = config.insightPassthroughSupport.ToString(),
                targetsQuest3 = config.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest3),
                profilePath = AssetDatabase.GetAssetPath(BuildProfile.GetActiveBuildProfile()),
                developmentBuild = EditorUserBuildSettings.development,
                metaTasksEvaluated = meta.tasksStatus.Length,
                metaRequiredIssues = meta.tasksStatus.Count(x => !x.isDone && x.level == "Required"),
                openXrIssues = native.Select(x => new Issue { message = x.message, required = x.error }).ToArray(),
                buildValidationIssues = failures.OrderBy(x => x.GetDisplayString()).Select(x =>
                    new Issue { message = x.GetDisplayString(), required = x.Error }).ToArray()
            };
            report.gatePassed = report.metaRequiredIssues == 0 && !report.openXrIssues.Any(x => x.required) &&
                !report.buildValidationIssues.Any(x => x.required) && report.initializeOnStartup &&
                report.loaders.SequenceEqual(new[] { typeof(OpenXRLoader).FullName }) &&
                report.applicationId == "com.brickbloom.mr.dev" && report.minimumApi == 32 && report.targetApi == 34 &&
                report.graphicsApis.SequenceEqual(new[] { "Vulkan" }) && report.handTracking == "HandsOnly" &&
                report.passthrough == "Required" && report.targetsQuest3 && report.profilePath == ProfilePath && report.developmentBuild &&
                xr.GetFeature<MetaXRFeature>().enabled && xr.GetFeature<HandTracking>().enabled && xr.GetFeature<MetaHandTrackingAim>().enabled;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
            AssetDatabase.SaveAssets();
            Debug.Log("BrickBloom Android configuration gate: " + report.gatePassed);
            SessionState.SetBool(PendingValidationKey, false);
            EditorApplication.Exit(report.gatePassed ? 0 : 2);
        }

        private static void Fail(Exception ex)
        {
            SessionState.SetBool(PendingValidationKey, false);
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
        private static void SaveMetaReport(object processor)
        {
            var summary = processor.GetType().GetProperty("Summary").GetValue(processor);
            summary.GetType().GetMethod("GenerateReport").Invoke(summary,
                new object[] { Path.GetDirectoryName(metaReportPath), Path.GetFileName(metaReportPath) });
        }
        [Serializable] private sealed class MetaReport { public string buildTargetGroup; public MetaTask[] tasksStatus; }
        [Serializable] private sealed class MetaTask { public string level; public bool isDone; }
        [Serializable] private sealed class Issue { public string message; public bool required; }
        [Serializable] private sealed class ValidationReport
        {
            public string generatedUtc, unityVersion, activeTarget, applicationId, handTracking, passthrough, profilePath;
            public int minimumApi, targetApi, metaTasksEvaluated, metaRequiredIssues;
            public bool initializeOnStartup, targetsQuest3, developmentBuild, gatePassed;
            public string[] graphicsApis, loaders, enabledFeatures;
            public Issue[] openXrIssues, buildValidationIssues;
        }
    }
}
