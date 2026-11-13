using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BrickBloom.Editor
{
    // Batch report of locomotion-related objects in the XRFoundation scene, and an explicit entry point to disable them.
    public static class LocomotionAudit
    {
        public static void Report()
        {
            try
            {
                XRFoundationSetup.RequireEnvironment();
                EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var group in all.Where(m => m != null && ((m.GetType().Namespace ?? "").Contains("Locomotion") ||
                             m.GetType().Name.Contains("Locomot") || m.GetType().Name.Contains("Teleport") || m.GetType().Name.Contains("PlayerController")))
                             .GroupBy(m => m.gameObject).OrderBy(g => PathOf(g.Key.transform)))
                {
                    Debug.Log($"LOCO | {PathOf(group.Key.transform)} | activeSelf={group.Key.activeSelf} | activeInHierarchy={group.Key.activeInHierarchy} | {string.Join(",", group.Select(m => m.GetType().Name + (m.enabled ? "" : "(off)")))}");
                }
                foreach (var cc in UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Debug.Log($"LOCO-CC | {PathOf(cc.transform)} | activeInHierarchy={cc.gameObject.activeInHierarchy}");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static readonly string[] SourceTypeNames =
        {
            "TeleportInteractor", "StepLocomotionBroadcaster", "TurnLocomotionBroadcaster", "SlideLocomotionBroadcaster",
            "LocomotionAxisTurnerInteractor", "LocomotionActionsBroadcaster"
        };

        // The MVP is played standing or seated at a real desk: the user never moves the rig artificially.
        // Deactivates every locomotion gesture source and the locomotor that would move the camera rig.
        public static void Disable()
        {
            try
            {
                XRFoundationSetup.RequireEnvironment();
                var scene = EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(m => m != null).ToArray();
                var locomotors = all.Where(m => m.GetType().Name == "FirstPersonLocomotor").ToArray();
                if (locomotors.Length != 1 || locomotors[0].transform.parent == null ||
                    locomotors[0].transform.parent.name != "Locomotor")
                    throw new InvalidOperationException("Expected exactly 1 FirstPersonLocomotor under a 'Locomotor' object.");

                int changed = 0;
                var targets = all.Where(m => SourceTypeNames.Contains(m.GetType().Name)).Select(m => m.gameObject)
                    .Append(locomotors[0].transform.parent.gameObject).Distinct();
                foreach (var target in targets)
                {
                    if (!target.activeSelf) continue;
                    target.SetActive(false);
                    EditorUtility.SetDirty(target);
                    changed++;
                    Debug.Log("LOCO-DISABLED | " + PathOf(target.transform));
                }

                var stillActive = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Where(m => m != null && (SourceTypeNames.Contains(m.GetType().Name) || m.GetType().Name == "FirstPersonLocomotor")).ToArray();
                if (stillActive.Length != 0)
                    throw new InvalidOperationException("Locomotion sources still active: " + string.Join(", ", stillActive.Select(m => PathOf(m.transform))));

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, XRFoundationSetup.ScenePath);
                XRFoundationSetup.CheckScene();
                Debug.Log($"BrickBloom locomotion disabled: {changed} objects deactivated.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
