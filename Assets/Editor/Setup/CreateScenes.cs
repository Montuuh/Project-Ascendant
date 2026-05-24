using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public class CreateScenes
{
    public static void Execute()
    {
        string[] sceneNames = { "Boot", "MainMenu", "Hub", "Combat", "Map" };
        string scenesPath = "Assets/Scenes";

        if (!Directory.Exists(scenesPath))
            Directory.CreateDirectory(scenesPath);

        var buildScenes = new EditorBuildSettingsScene[sceneNames.Length];

        for (int i = 0; i < sceneNames.Length; i++)
        {
            string path = $"{scenesPath}/{sceneNames[i]}.unity";

            if (!File.Exists(path))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log($"[ProjectAscendant] Created scene: {path}");
            }
            else
            {
                Debug.Log($"[ProjectAscendant] Scene already exists: {path}");
            }

            buildScenes[i] = new EditorBuildSettingsScene(path, true);
        }

        EditorBuildSettings.scenes = buildScenes;
        AssetDatabase.Refresh();
        Debug.Log("[ProjectAscendant] Build Settings updated with all scenes (Boot index 0).");
    }
}
