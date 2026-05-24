namespace ProjectAscendant.Core
{
    // Per §9.5.1 — root node of the full HSM tree.
    // Owns all top-level state instances; boots into MainMenu.
    public sealed class GameRootState : GameStateNode
    {
        public readonly MainMenuState  MainMenu;
        public readonly HubState       Hub;
        public readonly RunState       Run;
        public readonly GameOverState  GameOver;

        public GameRootState()
        {
            // Creation order matters: states that reference siblings access them
            // only inside HandleEvent (after construction), so forward-reference is safe.
            MainMenu = new MainMenuState(this);
            Hub      = new HubState(this);
            Run      = new RunState(this);
            GameOver = new GameOverState(this);
        }

        public override void OnEnter() { EnterChild(MainMenu); }
    }
}
