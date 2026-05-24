using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ConfigureProjectSettings
{
    public static void Execute()
    {
        // Per Epic 1 Task 1.1.2 — Player Settings
        PlayerSettings.companyName = "Montuuh";
        PlayerSettings.productName = "Project Ascendant";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = true;

        // Per Epic 1 Task 1.1.3 — Scripting backend
        // Editor uses Mono (default), Standalone uses IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);

        // C# 10 language version
        PlayerSettings.SetAdditionalCompilerArgumentsForGroup(BuildTargetGroup.Standalone, new string[] { });

        // Per Epic 1 Task 1.1.6 — Quality Settings
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectAscendant] Player Settings configured successfully.");
    }
}
