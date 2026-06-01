using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Clears Unity's internal scene-dirty flag so Untitled / modified scenes can close
/// without SaveCurrentModifiedScenesIfUserWantsTo blocking automation (Coplay MCP).
/// Uses EditorSceneManager.ClearSceneDirtiness (internal).
/// </summary>
public static class EditorSceneDiscardUtility
{
    static MethodInfo _clearSceneDirtiness;

    public static void ClearDirtiness(Scene scene)
    {
        if (!scene.IsValid())
            return;

        var method = GetClearSceneDirtinessMethod();
        if (method == null)
            return;

        method.Invoke(null, new object[] { scene });
    }

    public static void ClearDirtinessOnAllLoadedScenes()
    {
        for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            ClearDirtiness(EditorSceneManager.GetSceneAt(i));
    }

    /// <summary>
    /// Drop Untitled / dirty runner scenes and open a saved scene without a save dialog.
    /// </summary>
    public static void OpenSceneDiscardingUntitled(string scenePath, string fallbackScenePath)
    {
        ClearDirtinessOnAllLoadedScenes();

        var path = ResolveExistingPath(scenePath, fallbackScenePath);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    static string ResolveExistingPath(string scenePath, string fallbackScenePath)
    {
        if (!string.IsNullOrEmpty(scenePath) && System.IO.File.Exists(scenePath))
            return scenePath;

        if (!string.IsNullOrEmpty(fallbackScenePath) && System.IO.File.Exists(fallbackScenePath))
            return fallbackScenePath;

        Debug.LogWarning($"[TestRunner] Scene discard: no scene file at '{scenePath}' or '{fallbackScenePath}'.");
        return scenePath;
    }

    static MethodInfo GetClearSceneDirtinessMethod()
    {
        if (_clearSceneDirtiness != null)
            return _clearSceneDirtiness;

        _clearSceneDirtiness = typeof(EditorSceneManager).GetMethod(
            "ClearSceneDirtiness",
            BindingFlags.Static | BindingFlags.NonPublic);

        if (_clearSceneDirtiness == null)
            Debug.LogWarning("[TestRunner] ClearSceneDirtiness reflection failed — scene save dialog may appear.");

        return _clearSceneDirtiness;
    }
}
