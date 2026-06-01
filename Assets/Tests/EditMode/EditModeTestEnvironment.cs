#if UNITY_EDITOR
using NUnit.Framework;

namespace ProjectAscendant.Tests
{
    /// <summary>
    /// EditModeLauncher closes the runner's dirty Untitled scene after all tests and calls
    /// SaveCurrentModifiedScenesIfUserWantsTo() — Coplay cannot click that dialog.
    ///
    /// OneTimeTearDown runs before launcher restore: clear internal scene dirtiness, then
    /// OpenScene(Single) so Untitled closes without prompting.
    /// </summary>
    [SetUpFixture]
    public class EditModeTestEnvironment
    {
        [OneTimeTearDown]
        public void DiscardUntitledBeforeEditModeLauncherRestore()
        {
            TestRunSceneGuard.DiscardUntitledBeforeLauncherRestore();
        }
    }
}
#endif
