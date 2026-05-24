namespace ProjectAscendant.Core
{
    // Per §9.5 — MonoBehaviour-free HSM driver. Registered with Services at boot.
    // Driven externally via Update(dt) called from Bootstrap's update loop.
    public sealed class GameStateMachine
    {
        private readonly GameRootState _root;

        public GameRootState Root => _root;

        public GameStateMachine()
        {
            _root = new GameRootState();
            _root.OnEnter();
        }

        public void Update(float dt) => _root.OnUpdate(dt);

        // Per §9.5.3 — dispatches an event into the HSM at the root;
        // bubbles down to the current leaf via HandleEvent overrides.
        public void HandleEvent(GameEvent evt) => _root.HandleEvent(evt);
    }
}
