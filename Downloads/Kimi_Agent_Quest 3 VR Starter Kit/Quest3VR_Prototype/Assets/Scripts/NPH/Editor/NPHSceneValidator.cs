using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Quest3VR.NPH.Editor
{
    /// <summary>
    /// Scene validation utility for NPH VR setup.
    /// Checks for required components and configuration issues.
    /// </summary>
    public static class NPHSceneValidator
    {
        [MenuItem("Tools/NPH/Validate Scene")]
        public static bool ValidateScene()
        {
            Debug.Log("╔══════════════════════════════════════════════════╗");
            Debug.Log("║     NPH VR Scene Validation                      ║");
            Debug.Log("╚══════════════════════════════════════════════════╝");

            bool allPassed = true;
            int warningCount = 0;
            int errorCount = 0;

            // 1. Check for XR Rig
            var xrRig = Object.FindObjectOfType<Quest3VR.Prototype.VRRigSetup>();
            if (xrRig == null)
            {
                Debug.LogError("[Validation] ❌ No VRRigSetup found. VR rig not configured.");
                errorCount++;
                allPassed = false;
            }
            else
            {
                Debug.Log("[Validation] ✓ VRRigSetup found");
            }

            // 2. Check for NPH System
            var nphController = Object.FindObjectOfType<NPHSceneController>();
            if (nphController == null)
            {
                Debug.LogError("[Validation] ❌ No NPHSceneController found. Run 'Build NPH Scene' first.");
                errorCount++;
                allPassed = false;
            }
            else
            {
                Debug.Log("[Validation] ✓ NPHSceneController found");

                // Validate NPHSceneController references
                var so = new SerializedObject(nphController);
                
                var apiClient = so.FindProperty("apiClient").objectReferenceValue as NPHApiClient;
                if (apiClient == null)
                {
                    Debug.LogError("[Validation] ❌ NPHSceneController.apiClient is not assigned");
                    errorCount++;
                    allPassed = false;
                }
                else
                {
                    Debug.Log("[Validation] ✓ NPHSceneController.apiClient assigned");
                }

                var sliceViewer = so.FindProperty("sliceViewer").objectReferenceValue as CTSliceViewer;
                if (sliceViewer == null)
                {
                    Debug.LogError("[Validation] ❌ NPHSceneController.sliceViewer is not assigned");
                    errorCount++;
                    allPassed = false;
                }
                else
                {
                    Debug.Log("[Validation] ✓ NPHSceneController.sliceViewer assigned");
                }

                var scorePanel = so.FindProperty("scorePanel").objectReferenceValue as NPHScorePanel;
                if (scorePanel == null)
                {
                    Debug.LogWarning("[Validation] ⚠ NPHSceneController.scorePanel is not assigned");
                    warningCount++;
                }
                else
                {
                    Debug.Log("[Validation] ✓ NPHSceneController.scorePanel assigned");
                }
            }

            // 3. Check for CT Slices
            var sampleTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Textures/SampleCT" });
            if (sampleTextures.Length == 0)
            {
                Debug.LogWarning("[Validation] ⚠ No CT textures found in Assets/Textures/SampleCT/");
                Debug.LogWarning("[Validation]   Run 'Tools > NPH > Pipeline: Import Textures' to generate demos");
                warningCount++;
            }
            else
            {
                Debug.Log($"[Validation] ✓ Found {sampleTextures.Length} CT texture(s)");
            }

            // 4. Check for API Client
            var apiClients = Object.FindObjectsOfType<NPHApiClient>();
            if (apiClients.Length == 0)
            {
                Debug.LogError("[Validation] ❌ No NPHApiClient found");
                errorCount++;
                allPassed = false;
            }
            else if (apiClients.Length > 1)
            {
                Debug.LogWarning($"[Validation] ⚠ Multiple NPHApiClient instances found ({apiClients.Length}). Only one singleton should exist.");
                warningCount++;
            }
            else
            {
                Debug.Log("[Validation] ✓ NPHApiClient found");
                
                var apiSo = new SerializedObject(apiClients[0]);
                var serverUrl = apiSo.FindProperty("serverUrl").stringValue;
                if (string.IsNullOrEmpty(serverUrl))
                {
                    Debug.LogWarning("[Validation] ⚠ NPHApiClient.serverUrl is empty");
                    warningCount++;
                }
                else
                {
                    Debug.Log($"[Validation] ✓ NPHApiClient.serverUrl = {serverUrl}");
                }
            }

            // 5. Check for VRHapticsManager
            var haptics = Object.FindObjectOfType<Quest3VR.Prototype.VRHapticsManager>();
            if (haptics == null)
            {
                Debug.LogWarning("[Validation] ⚠ No VRHapticsManager found. Haptics will not work.");
                warningCount++;
            }
            else
            {
                Debug.Log("[Validation] ✓ VRHapticsManager found");
            }

            // 6. Check build settings
            CheckBuildSettings(ref warningCount, ref errorCount);

            // Summary
            Debug.Log("═══════════════════════════════════════════════════");
            if (allPassed && warningCount == 0)
            {
                Debug.Log("[Validation] ✅ All checks passed! Scene is ready for build.");
            }
            else if (allPassed)
            {
                Debug.Log($"[Validation] ⚠ Passed with {warningCount} warning(s)");
            }
            else
            {
                Debug.LogError($"[Validation] ❌ Failed with {errorCount} error(s) and {warningCount} warning(s)");
            }
            Debug.Log("═══════════════════════════════════════════════════");

            return allPassed;
        }

        private static void CheckBuildSettings(ref int warnings, ref int errors)
        {
            #if UNITY_EDITOR
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (buildTarget != BuildTarget.Android)
            {
                Debug.LogWarning("[Validation] ⚠ Build target is not Android. Run 'Configure Quest 3 Build' to fix.");
                warnings++;
            }
            else
            {
                Debug.Log("[Validation] ✓ Build target is Android");
            }

            var scriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            if (scriptingBackend != ScriptingImplementation.IL2CPP)
            {
                Debug.LogWarning("[Validation] ⚠ Scripting backend is not IL2CPP");
                warnings++;
            }
            else
            {
                Debug.Log("[Validation] ✓ Scripting backend is IL2CPP");
            }

            var architectures = PlayerSettings.Android.targetArchitectures;
            if ((architectures & AndroidArchitecture.ARM64) == 0)
            {
                Debug.LogWarning("[Validation] ⚠ ARM64 architecture not enabled");
                warnings++;
            }
            else
            {
                Debug.Log("[Validation] ✓ ARM64 architecture enabled");
            }
            #endif
        }

        [MenuItem("Tools/NPH/Quick Fix Scene")]
        public static void QuickFixScene()
        {
            Debug.Log("[QuickFix] Running automatic scene fixes...");

            // Ensure XR Rig is in the scene
            var xrRig = Object.FindObjectOfType<Quest3VR.Prototype.VRRigSetup>();
            if (xrRig == null)
            {
                // Try to instantiate from prefab
                string[] guids = AssetDatabase.FindAssets("t:Prefab XR_Rig_Quest3");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        Object.Instantiate(prefab);
                        Debug.Log("[QuickFix] ✅ Instantiated XR_Rig_Quest3 prefab");
                    }
                }
                else
                {
                    Debug.LogError("[QuickFix] ❌ XR_Rig_Quest3 prefab not found");
                }
            }

            // Ensure NPH System exists
            var nphSystem = GameObject.Find("NPH_System");
            if (nphSystem == null)
            {
                NPHSceneBuilder.BuildScene();
                Debug.Log("[QuickFix] ✅ Built NPH scene hierarchy");
            }

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[QuickFix] Scene fixes applied. Save the scene (Ctrl+S) to persist changes.");
        }
    }
}
