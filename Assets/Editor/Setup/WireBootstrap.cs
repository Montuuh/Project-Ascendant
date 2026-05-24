using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class WireBootstrap
{
    public static void Execute()
    {
        string bootScenePath = "Assets/Scenes/Boot.unity";
        var scene = EditorSceneManager.OpenScene(bootScenePath, OpenSceneMode.Single);

        // Remove default camera/lights if present
        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // Create Bootstrap object
        var bootstrapGO = new GameObject("Bootstrap");
        bootstrapGO.AddComponent<ProjectAscendant.Core.Bootstrap>();

        EditorSceneManager.SaveScene(scene, bootScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[ProjectAscendant] Bootstrap wired into Boot.unity.");
    }
}
