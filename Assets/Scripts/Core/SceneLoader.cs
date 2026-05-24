using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAscendant.Core
{
    // Per Epic 1 Task 1.4.4 — Async scene loading via Addressables.
    // TODO: Switch to Addressables scene load once Addressables catalog is set up (§9.2).
    // Current stub uses SceneManager for compilation; swap in Phase B.
    public static class SceneLoader
    {
        public static IEnumerator LoadAsync(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Scene not found in build settings: {sceneName}");
                yield break;
            }

            while (!op.isDone)
                yield return null;
        }
    }
}
