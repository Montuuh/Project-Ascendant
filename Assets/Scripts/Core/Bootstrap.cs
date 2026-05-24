using System.Collections;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.14 + Epic 1 Task 1.4.3 — Boot scene entry point.
    // Registers all canonical services, then transitions to MainMenu.
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _firstScene = "MainMenu";

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);

            Services.Clear();

            // Per §9.14 — canonical service registrations.
            // TODO: Task 2.5 — register GameRNG instance once implemented.
            // TODO: Task 2.7 — register SaveSystem instance once implemented.
            // TODO: Epic 3  — load and register RunStateSO via Addressables.
            // TODO: Epic 3  — load and register MetaProgressionSO via Addressables.

            EventBus.Initialise();

            yield return SceneLoader.LoadAsync(_firstScene);
        }
    }
}
