using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §3.3 + Epic 4 Task 4.1.5 — the discrete action a player can take
    // during Action Phase. Tests script these; the eventual UI surfaces them
    // as button presses / card drops. Either way the controller is driven by
    // the IPlayerAgent contract below.
    public enum PlayerActionKind
    {
        PlaySkill,        // CardIndex into SkillHand; TargetEnemySlot
        PlayConsumable,   // CardIndex into ConsumableHand
        ManualSwap,       // SwapToBenchSlot — bench index (1 or 2) to promote
        EndTurn,
    }

    public struct PlayerAction
    {
        public PlayerActionKind Kind;
        public int CardIndex;           // hand index for PlaySkill/PlayConsumable
        public int TargetEnemySlot;     // 0..n for damage moves (skill cards)
        public int SwapToBenchSlot;     // for ManualSwap (Epic 6 — stubbed in 4.1)

        public static PlayerAction End() => new() { Kind = PlayerActionKind.EndTurn };
        public static PlayerAction PlaySkill(int handIndex, int enemySlot = 0) => new()
        {
            Kind = PlayerActionKind.PlaySkill,
            CardIndex = handIndex,
            TargetEnemySlot = enemySlot,
        };
    }

    // Per Epic 4 Task 4.1.5 — injection point for player decisions. Tests
    // implement scripted agents; the production game wires a UI-driven
    // implementation. The agent NEVER reads private controller state; it
    // gets CombatState snapshots passed in.
    public interface IPlayerAgent
    {
        PlayerAction DecideAction(CombatController.CombatState state);

        // Per §4.8.1 — when Lead faints with a non-empty bench, the player
        // picks a replacement. Returns the index into PlayerTeam (0..2) of
        // the new Lead. Implementations must return one of `candidates` —
        // controller verifies.
        int PickLeadReplacement(CombatController.CombatState state,
                                System.Collections.Generic.IReadOnlyList<PokemonInstance> candidates);
    }
}
