using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene hygiene for Coplay / TestRunnerApi EditMode runs.
/// Unity's EditModeLauncher prompts to save when a dirty Untitled scene closes — we must
/// replace that scene before the launcher restores editor setup (see OneTimeTearDown in
/// EditModeTestEnvironment). This class handles pre-run capture and post-run restore.
/// </summary>
public static class TestRunSceneGuard
{
    const string FallbackScenePath = "Assets/Scenes/Boot.unity";
    const string EditorPrefsRestoreKey = "ProjectAscendant.TestRun.RestorePrimary";

    static SceneSetup[] _capturedSetup;
    static bool _runActive;

    [InitializeOnLoadMethod]
    static void RegisterHooks()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        EditorSceneManager.sceneClosing += OnSceneClosing;
    }

    static void OnSceneClosing(Scene scene, bool removingScene)
    {
        if (!_runActive)
            return;

        EditorSceneDiscardUtility.ClearDirtiness(scene);
    }

    static void OnBeforeAssemblyReload()
    {
        if (!_runActive)
            return;

        try
        {
            EditorSceneDiscardUtility.OpenSceneDiscardingUntitled(
                ReadRestorePrimaryPath(),
                FallbackScenePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[TestRunner] Scene guard pre-reload: {ex.Message}");
        }
    }

    /// <summary>Call immediately before TestRunnerApi.Execute().</summary>
    public static void Prepare()
    {
        _runActive = true;
        _capturedSetup = EditorSceneManager.GetSceneManagerSetup();

        var primary = GetPrimaryScenePath(_capturedSetup);
        EditorPrefs.SetString(EditorPrefsRestoreKey, primary);

        // EditModeLauncher calls SaveCurrentModifiedScenesIfUserWantsTo() when scenes are dirty.
        ClearDirtyEditorStateBeforeLaunch(primary);

        Debug.Log($"[TestRunner] Scene guard: prepared (restore target: {primary}).");
    }

    public static void Cleanup()
    {
        try
        {
            RestoreCapturedSetupWithoutDialog();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[TestRunner] Scene guard cleanup: {ex.Message}");
        }
        finally
        {
            _capturedSetup = null;
            _runActive = false;
            EditorPrefs.DeleteKey(EditorPrefsRestoreKey);
        }

        Debug.Log("[TestRunner] Scene guard: restored editor scenes after test run.");
    }

    /// <summary>Used by EditModeTestEnvironment.OneTimeTearDown (runs before EditModeLauncher restore).</summary>
    public static string ReadRestorePrimaryPath()
    {
        return EditorPrefs.GetString(EditorPrefsRestoreKey, FallbackScenePath);
    }

    /// <summary>Clear Untitled runner dirtiness before EditModeLauncher closes scenes.</summary>
    public static void DiscardUntitledBeforeLauncherRestore()
    {
        EditorSceneDiscardUtility.OpenSceneDiscardingUntitled(
            ReadRestorePrimaryPath(),
            FallbackScenePath);
    }

    static void ClearDirtyEditorStateBeforeLaunch(string primaryPath)
    {
        EditorSceneDiscardUtility.ClearDirtinessOnAllLoadedScenes();

        if (AnyUntitledScene() || AnyDirtyScene())
            EditorSceneDiscardUtility.OpenSceneDiscardingUntitled(primaryPath, FallbackScenePath);

        AssetDatabase.SaveAssets();
    }

    static bool AnyDirtyScene()
    {
        for (var i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (EditorSceneManager.GetSceneAt(i).isDirty)
                return true;
        }

        return false;
    }

    static void RestoreCapturedSetupWithoutDialog()
    {
        EditorSceneDiscardUtility.ClearDirtinessOnAllLoadedScenes();

        var setups = _capturedSetup?
            .Where(s => s.isLoaded && !string.IsNullOrEmpty(s.path))
            .ToArray();

        if (setups == null || setups.Length == 0)
        {
            EditorSceneDiscardUtility.OpenSceneDiscardingUntitled(FallbackScenePath, FallbackScenePath);
            return;
        }

        var primary = setups.FirstOrDefault(s => s.isActive) ?? setups[0];
        EditorSceneDiscardUtility.OpenSceneDiscardingUntitled(primary.path, FallbackScenePath);

        foreach (var setup in setups)
        {
            if (setup.path == primary.path)
                continue;
            EditorSceneManager.OpenScene(setup.path, OpenSceneMode.Additive);
        }

        var activeScene = EditorSceneManager.GetSceneByPath(primary.path);
        if (activeScene.IsValid() && activeScene.isLoaded)
            EditorSceneManager.SetActiveScene(activeScene);
    }

    static bool AnyUntitledScene()
    {
        for (var i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            if (string.IsNullOrEmpty(EditorSceneManager.GetSceneAt(i).path))
                return true;
        }

        return false;
    }

    static string GetPrimaryScenePath(SceneSetup[] setup)
    {
        var primary = setup?
            .FirstOrDefault(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path));
        if (!string.IsNullOrEmpty(primary.path))
            return primary.path;

        var first = setup?.FirstOrDefault(s => s.isLoaded && !string.IsNullOrEmpty(s.path));
        if (!string.IsNullOrEmpty(first.path))
            return first.path;

        var active = EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.path))
            return active.path;

        return FallbackScenePath;
    }
}
