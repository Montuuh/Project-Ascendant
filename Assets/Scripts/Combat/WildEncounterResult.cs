using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.3.4 + Epic 8 Task 8.1 — result returned by
    // WildEncounterController.ResolveOutcome after a wild combat ends.
    //
    // Outcome enum maps the three terminal states from §7.3.4.1:
    //   • Caught       — the wild Pokémon is in the player's hands;
    //                    BoxUpdated reflects whether it actually entered the
    //                    Box (false iff the player chose Skip at the prompt).
    //   • WildFainted  — the wild's HP hit 0 from combat damage. Recruit lost.
    //                    No Box change.
    //   • PlayerWiped  — Active Team wipe. Run-failure event fires per §3.3.6;
    //                    no Box change here.
    public struct WildEncounterResult
    {
        public enum WildOutcome { Caught, WildFainted, PlayerWiped }

        public WildOutcome Outcome;
        public PokemonInstance CaughtTarget;       // null unless Outcome==Caught
        public bool BoxOverflowPromptShown;        // true iff §2.3.1 fired
        public int ReleasedBoxIndex;               // -1 if no swap happened
        public bool BoxUpdated;                    // true iff a Pokémon entered the Box

        public static WildEncounterResult MakeCaught(PokemonInstance caught,
                                                     bool overflowShown,
                                                     int releasedIdx,
                                                     bool boxUpdated)
            => new()
            {
                Outcome = WildOutcome.Caught,
                CaughtTarget = caught,
                BoxOverflowPromptShown = overflowShown,
                ReleasedBoxIndex = releasedIdx,
                BoxUpdated = boxUpdated,
            };

        public static WildEncounterResult MakeWildFainted()
            => new()
            {
                Outcome = WildOutcome.WildFainted,
                ReleasedBoxIndex = -1,
            };

        public static WildEncounterResult MakePlayerWiped()
            => new()
            {
                Outcome = WildOutcome.PlayerWiped,
                ReleasedBoxIndex = -1,
            };
    }
}
