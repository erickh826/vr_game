using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BrickBloom.Editor
{
    // Batch-mode release build for upload to a Meta release channel: non-development, release package name,
    // signed with the locally kept keystore. Player settings are changed in memory only and restored afterwards,
    // so development builds and ProjectSettings stay as they are.
    // Optional "-brickbloomVersionCode <n>": every upload to the same channel needs a higher number.
    public static class ReleaseBuild
    {
        private const string ReleasePackage = "com.brickbloom.mr";
        private const string ProductName = "BrickBloom MR";
        private const string CompanyName = "BrickBloom";
        private const string KeystorePath = "Keystore/brickbloom-release.keystore";
        private const string PasswordPath = "Keystore/keystore-password.txt";
        private const string KeyAlias = "brickbloom";
        private const string ApkPath = "Builds/Quest/BrickBloomMR-release.apk";

        public static void BuildReleaseApk()
        {
            string oldPackage = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            string oldProduct = PlayerSettings.productName, oldCompany = PlayerSettings.companyName;
            int oldCode = PlayerSettings.Android.bundleVersionCode;
            bool oldCustom = PlayerSettings.Android.useCustomKeystore;
            string oldKeystore = PlayerSettings.Android.keystoreName, oldAlias = PlayerSettings.Android.keyaliasName;
            int exitCode = 1;
            try
            {
                XRFoundationSetup.RequireEnvironment();
                if (!File.Exists(KeystorePath) || !File.Exists(PasswordPath))
                    throw new InvalidOperationException($"Release keystore or password file missing ({KeystorePath}, {PasswordPath}).");
                string password = File.ReadAllText(PasswordPath).Trim();
                if (password.Length == 0) throw new InvalidOperationException("Keystore password file is empty.");
                int versionCode = VersionCode();

                EditorSceneManager.OpenScene(XRFoundationSetup.ScenePath, OpenSceneMode.Single);
                XRFoundationSetup.CheckScene();

                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ReleasePackage);
                PlayerSettings.productName = ProductName;
                PlayerSettings.companyName = CompanyName;
                PlayerSettings.Android.bundleVersionCode = versionCode;
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(KeystorePath);
                PlayerSettings.Android.keystorePass = password;
                PlayerSettings.Android.keyaliasName = KeyAlias;
                PlayerSettings.Android.keyaliasPass = password;

                Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { XRFoundationSetup.ScenePath }, target = BuildTarget.Android,
                    locationPathName = ApkPath, options = BuildOptions.None
                });
                Debug.Log($"BrickBloom release APK build: {report.summary.result} | package={ReleasePackage} | " +
                    $"version={PlayerSettings.bundleVersion} | versionCode={versionCode} | errors={report.summary.totalErrors}");
                exitCode = report.summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception ex) { Debug.LogException(ex); }
            finally
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, oldPackage);
                PlayerSettings.productName = oldProduct;
                PlayerSettings.companyName = oldCompany;
                PlayerSettings.Android.bundleVersionCode = oldCode;
                PlayerSettings.Android.useCustomKeystore = oldCustom;
                PlayerSettings.Android.keystoreName = oldKeystore;
                PlayerSettings.Android.keyaliasName = oldAlias;
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
            }

            EditorApplication.Exit(exitCode);
        }

        private static int VersionCode()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-brickbloomVersionCode");
            if (index < 0) return 1;
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out int code) || code < 1)
                throw new InvalidOperationException("Invalid -brickbloomVersionCode value.");
            return code;
        }
    }
}
