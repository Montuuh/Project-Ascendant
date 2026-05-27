using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §5.5.4 + Epic 6 Task 6.6 — Lead Aura runtime.
    //
    // A Lead Aura is a passive damage buff that the LEAD Pokémon broadcasts
    // to its BENCH allies whose moves match the aura's type. Sources:
    //   • AbilitySO with Category == Aura, GrantsLeadAura == true, and a
    //     declared LeadAuraType.
    //   • HeldItemSO with GrantsLeadAura == true (Type Plates per §5.5.4).
    //
    // Stacking: each authored source contributes +LeadAuraMatchingTypeBonus
    // (default 0.05). Multiple sources granting the SAME type stack
    // additively (e.g. Charcoal item + Blaze ability on Charizard ⇒ +0.10
    // on Fire moves). Sources granting DIFFERENT types each apply to their
    // own type independently.
    //
    // Exclusions:
    //   • The Lead's own moves do NOT receive the aura (the buff is for
    //     bench-attackers only — playing as Lead is its own reward).
    //   • A fainted Lead grants no aura (§2.4.1: 0 HP = fainted).
    //   • Enemy attackers are never buffed by the player's Lead Aura.
    //     The caller passes the attacker's own team; if the attacker is
    //     not a member of that team OR is the Lead itself, the multiplier
    //     resolves to 1.0.
    //
    // Stateless pull-based design: the multiplier is recomputed from live
    // state each time it's applied. No cache, no event subscription —
    // simpler than a recalc-on-lead-change cache and equivalent in cost
    // for the per-strike read budget. The UI buff icon (post-VS, Epic 13)
    // can poll GetActiveAuraTypes(lead) to render the chip set.
    public static class LeadAuraResolver
    {
        // Per Task 6.6.2 — damage multiplier the controller multiplies into
        // the post-formula damage value, sitting alongside fieldMul and
        // freezeFireMul in CombatController.ResolveDamage.
        //
        // Returns 1.0 when:
        //   • Any argument is null.
        //   • lead is fainted.
        //   • attacker is the Lead.
        //   • attacker is not a member of attackerTeam (e.g. enemy attacker
        //     with the player's Lead passed in by mistake).
        //   • No aura source on the Lead matches move.Type.
        public static float GetDamageMultiplier(
            PokemonInstance attacker,
            MoveSO move,
            PokemonInstance lead,
            IReadOnlyList<PokemonInstance> attackerTeam,
            BattleConfigSO config)
        {
            if (attacker == null || move == null || lead == null || config == null)
                return 1f;
            if (lead.CurrentHP <= 0) return 1f;            // §2.4.1 — fainted = no aura
            if (ReferenceEquals(attacker, lead)) return 1f; // Lead's own moves excluded
            if (!IsMemberOf(attacker, attackerTeam)) return 1f;

            int matchingSources = CountMatchingAuraSources(lead, move.Type);
            if (matchingSources == 0) return 1f;
            return 1f + matchingSources * config.LeadAuraMatchingTypeBonus;
        }

        // Per Task 6.6.1 — UI / debug accessor. Returns the deduplicated set
        // of types the Lead is currently broadcasting an aura for, with the
        // count of contributing sources per type. Empty when Lead has no
        // aura sources, is fainted, or is null.
        public static Dictionary<PokemonType, int> GetActiveAuraTypes(PokemonInstance lead)
        {
            Dictionary<PokemonType, int> result = new();
            if (lead == null || lead.CurrentHP <= 0) return result;

            if (lead.Ability != null && lead.Ability.GrantsLeadAura)
                Add(result, lead.Ability.LeadAuraType);
            if (lead.HeldItem != null && lead.HeldItem.GrantsLeadAura)
                Add(result, lead.HeldItem.LeadAuraType);

            return result;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static int CountMatchingAuraSources(PokemonInstance lead, PokemonType moveType)
        {
            int n = 0;
            if (lead.Ability != null && lead.Ability.GrantsLeadAura
                && lead.Ability.LeadAuraType == moveType) n++;
            if (lead.HeldItem != null && lead.HeldItem.GrantsLeadAura
                && lead.HeldItem.LeadAuraType == moveType) n++;
            return n;
        }

        private static bool IsMemberOf(PokemonInstance p, IReadOnlyList<PokemonInstance> team)
        {
            if (team == null) return false;
            for (int i = 0; i < team.Count; i++)
                if (ReferenceEquals(team[i], p)) return true;
            return false;
        }

        private static void Add(Dictionary<PokemonType, int> map, PokemonType t)
        {
            if (map.TryGetValue(t, out int cur)) map[t] = cur + 1;
            else map[t] = 1;
        }
    }
}
