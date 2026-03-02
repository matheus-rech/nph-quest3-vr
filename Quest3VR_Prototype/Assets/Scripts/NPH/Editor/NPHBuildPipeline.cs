using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace Quest3VR.NPH.Editor
{
    /// <summary>
    /// Full automated build pipeline for NPH VR on Quest 3.
    /// Each method is callable from Unity batch mode via -executeMethod.
    ///
    /// Usage:
    ///   Unity -batchmode -quit -executeMethod Quest3VR.NPH.Editor.NPHBuildPipeline.BuildAll
    ///
    /// Reads configuration from command-line args:
    ///   -apiUrl http://192.168.1.100:8000
    ///   -texturesDir /path/to/ct/pngs
    ///   -outputPath /path/to/build/output.apk
    /// </summary>
    public static class NPHBuildPipeline
    {
        // ── Command-line arg helpers ──────────────────────────────

        private static string GetArg(string name, string fallback = null)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return fallback;
        }

        private static bool HasFlag(string name)
        {
            return Environment.GetCommandLineArgs().Contains(name);
        }

        // ── 1. Import CT textures ────────────────────────────────

        /// <summary>
        /// Import CT slice PNGs/JPGs from an external directory into Assets/Textures/SampleCT/.
        /// If no -texturesDir arg is provided, generates 5 procedural demo textures.
        /// </summary>
        [MenuItem("Tools/NPH/Pipeline: Import Textures")]
        public static void ImportTextures()
        {
            string destDir = Path.Combine(Application.dataPath, "Textures", "SampleCT");
            Directory.CreateDirectory(destDir);

            string srcDir = GetArg("-texturesDir");

            if (!string.IsNullOrEmpty(srcDir) && Directory.Exists(srcDir))
            {
                var files = Directory.GetFiles(srcDir)
                    .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (files.Length == 0)
                {
                    Debug.LogWarning($"[NPHBuildPipeline] No PNG/JPG files found in {srcDir}, generating demo textures.");
                    GenerateDemoTextures(destDir);
                }
                else
                {
                    foreach (var file in files)
                    {
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, overwrite: true);
                    }
                    Debug.Log($"[NPHBuildPipeline] Imported {files.Length} CT textures from {srcDir}");
                }
            }
            else
            {
                Debug.Log("[NPHBuildPipeline] No -texturesDir provided, generating 5 demo CT textures.");
                GenerateDemoTextures(destDir);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Configure imported textures as Sprites with correct settings
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/SampleCT" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.isReadable = true; // Needed for GetPixels/EncodeToPNG at runtime
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 1024;

                    // ASTC compression for Quest 3
                    var androidSettings = importer.GetPlatformTextureSettings("Android");
                    androidSettings.overridden = true;
                    androidSettings.format = TextureImporterFormat.ASTC_6x6;
                    androidSettings.maxTextureSize = 1024;
                    importer.SetPlatformTextureSettings(androidSettings);

                    importer.SaveAndReimport();
                }
            }

            Debug.Log($"[NPHBuildPipeline] Texture import complete — {guids.Length} textures configured.");
        }

        private static void GenerateDemoTextures(string destDir)
        {
            // Generate synthetic brain-CT-like grayscale slices for demo purposes
            int size = 512;
            for (int i = 0; i < 5; i++)
            {
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                float centerX = size * 0.5f;
                float centerY = size * 0.5f;
                float skullRadius = size * 0.42f;
                float ventricleRadius = size * (0.08f + i * 0.015f); // grow slightly per slice

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        float val = 0f; // background black

                        if (dist < skullRadius)
                        {
                            // Skull ring
                            if (dist > skullRadius * 0.92f)
                                val = 0.9f; // bright skull bone
                            else
                                val = 0.35f + 0.05f * Mathf.PerlinNoise(x * 0.02f, y * 0.02f); // brain tissue
                        }

                        // Left ventricle (dark CSF)
                        float lvx = centerX - size * 0.06f;
                        float lvy = centerY + size * 0.02f;
                        float ldist = Mathf.Sqrt((x - lvx) * (x - lvx) + (y - lvy) * (y - lvy));
                        if (ldist < ventricleRadius)
                            val = 0.05f;

                        // Right ventricle
                        float rvx = centerX + size * 0.06f;
                        float rdist = Mathf.Sqrt((x - rvx) * (x - rvx) + (y - lvy) * (y - lvy));
                        if (rdist < ventricleRadius)
                            val = 0.05f;

                        tex.SetPixel(x, y, new Color(val, val, val));
                    }
                }

                tex.Apply();
                byte[] png = tex.EncodeToPNG();
                string filename = $"demo_ct_slice_{i:D3}.png";
                File.WriteAllBytes(Path.Combine(destDir, filename), png);
                UnityEngine.Object.DestroyImmediate(tex);
            }

            Debug.Log("[NPHBuildPipeline] Generated 5 demo CT textures.");
        }

        // ── 2. Build scene + wire references ─────────────────────

        /// <summary>
        /// Build NPH scene hierarchy, assign sample textures, configure API URL, save scene.
        /// </summary>
        [MenuItem("Tools/NPH/Pipeline: Build & Configure Scene")]
        public static void BuildAndConfigureScene()
        {
            // Build scene hierarchy
            NPHSceneBuilder.BuildScene();

            // Find sample textures
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/SampleCT" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[NPHBuildPipeline] No sample textures found. Run ImportTextures first.");
            }
            else
            {
                // Wire textures to NPHSceneController.sampleCTSlices
                var controller = UnityEngine.Object.FindObjectOfType<NPHSceneController>();
                if (controller != null)
                {
                    var textures = guids
                        .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(t => t != null)
                        .OrderBy(t => t.name)
                        .ToArray();

                    var so = new SerializedObject(controller);
                    var prop = so.FindProperty("sampleCTSlices");
                    prop.arraySize = textures.Length;
                    for (int i = 0; i < textures.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = textures[i];
                    so.ApplyModifiedPropertiesWithoutUndo();

                    Debug.Log($"[NPHBuildPipeline] Assigned {textures.Length} sample textures to NPHSceneController.");
                }
            }

            // Configure API URL
            string apiUrl = GetArg("-apiUrl", "http://192.168.1.100:8000");
            var apiClient = UnityEngine.Object.FindObjectOfType<NPHApiClient>();
            if (apiClient != null)
            {
                var so = new SerializedObject(apiClient);
                var urlProp = so.FindProperty("serverUrl");
                if (urlProp != null)
                {
                    urlProp.stringValue = apiUrl;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[NPHBuildPipeline] API URL set to: {apiUrl}");
                }
            }

            // Save scene
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[NPHBuildPipeline] Scene saved: {scene.path}");
        }

        // ── 3. Configure Android/Quest 3 build settings ──────────

        /// <summary>
        /// Switch to Android platform and configure Quest 3 build settings.
        /// </summary>
        [MenuItem("Tools/NPH/Pipeline: Configure Quest 3 Build")]
        public static void ConfigureQuest3Build()
        {
            // Switch to Android
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[NPHBuildPipeline] Switching to Android build target...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // IL2CPP scripting backend (required for Quest)
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

            // ARM64 only (Quest 3 is ARM64)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Texture compression: ASTC (Quest 3 native)
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // Graphics API: OpenGLES3 for Oculus XR compatibility
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            // Single Pass Instanced rendering (Quest VR standard)
            PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;

            // Minimum API level: Android 10 (Quest 3 runs Android 12)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // Build system: Gradle
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // Ensure the scene is in build settings
            string scenePath = "Assets/Scenes/VRStarterScene.unity";
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[NPHBuildPipeline] Added {scenePath} to build settings.");
            }

            Debug.Log("[NPHBuildPipeline] Quest 3 build settings configured:");
            Debug.Log("  Backend: IL2CPP, Arch: ARM64, Textures: ASTC");
            Debug.Log("  Graphics: OpenGLES3, Rendering: Single Pass Instanced");
            Debug.Log("  Min SDK: Android 10 (API 29)");
        }

        // ── 4. Build APK ─────────────────────────────────────────

        /// <summary>
        /// Build the Quest 3 APK. Outputs to -outputPath or Builds/NPH_Quest3.apk.
        /// </summary>
        [MenuItem("Tools/NPH/Pipeline: Build APK")]
        public static void BuildAPK()
        {
            string outputPath = GetArg("-outputPath");
            if (string.IsNullOrEmpty(outputPath))
            {
                string buildsDir = Path.Combine(Application.dataPath, "..", "Builds");
                Directory.CreateDirectory(buildsDir);
                outputPath = Path.Combine(buildsDir, "NPH_Quest3.apk");
            }

            // Ensure output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            string scenePath = "Assets/Scenes/VRStarterScene.unity";

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            Debug.Log($"[NPHBuildPipeline] Building APK → {outputPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMB = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[NPHBuildPipeline] BUILD SUCCEEDED — {sizeMB:F1} MB");
                Debug.Log($"[NPHBuildPipeline] APK: {outputPath}");
                Debug.Log($"[NPHBuildPipeline] Time: {summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                Debug.LogError($"[NPHBuildPipeline] BUILD FAILED: {summary.result}");
                Debug.LogError($"[NPHBuildPipeline] Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}");

                // In batch mode, exit with error code
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // ── 5. Full pipeline ─────────────────────────────────────

        /// <summary>
        /// Run the complete pipeline: import textures → build scene → configure Quest 3 → build APK.
        /// This is the main entry point for the shell script.
        /// </summary>
        [MenuItem("Tools/NPH/Pipeline: Build All (Full Pipeline)")]
        public static void BuildAll()
        {
            Debug.Log("╔══════════════════════════════════════════════════╗");
            Debug.Log("║     NPH VR Quest 3 — Full Build Pipeline        ║");
            Debug.Log("╚══════════════════════════════════════════════════╝");

            var startTime = DateTime.Now;

            try
            {
                Debug.Log("\n── Step 1/4: Import Textures ──────────────────");
                ImportTextures();

                Debug.Log("\n── Step 2/4: Build & Configure Scene ──────────");
                BuildAndConfigureScene();

                Debug.Log("\n── Step 3/4: Configure Quest 3 Build ──────────");
                ConfigureQuest3Build();

                if (!HasFlag("-skipBuild"))
                {
                    Debug.Log("\n── Step 4/4: Build APK ────────────────────────");
                    BuildAPK();
                }
                else
                {
                    Debug.Log("\n── Step 4/4: SKIPPED (-skipBuild flag) ────────");
                }

                var elapsed = DateTime.Now - startTime;
                Debug.Log($"\n[NPHBuildPipeline] Full pipeline completed in {elapsed.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NPHBuildPipeline] Pipeline failed: {ex.Message}\n{ex.StackTrace}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // ── Scene-only pipeline (no APK build) ───────────────────

        /// <summary>
        /// Import textures + build scene only (no APK build).
        /// Useful for setting up the project before manual iteration in the Editor.
        /// </summary>
        [MenuItem("Tools/NPH/Pipeline: Setup Only (No Build)")]
        public static void SetupOnly()
        {
            Debug.Log("[NPHBuildPipeline] Running setup-only pipeline...");
            ImportTextures();
            BuildAndConfigureScene();
            Debug.Log("[NPHBuildPipeline] Setup complete. Open scene and iterate in Editor.");
        }
    }
}
