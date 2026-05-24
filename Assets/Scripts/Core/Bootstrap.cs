using System.Collections;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 1 Task 1.4.3 — Boot scene entry point.
    // Initialises core services in order, then transitions to MainMenu.
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _firstScene = "MainMenu";

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);

            ServiceLocator.Initialise();
            EventBus.Initialise();

            yield return SceneLoader.LoadAsync(_firstScene);
        }
    }
}
