using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.3.2 + Epic 10 Task 10.5 — resolves which evolution branches a Pokémon may choose from at
    // the Branch-Selection screen: the species' level-only branches, PLUS any branch unlocked by an
    // Evolution Item currently in the run inventory. The SourceItem field tells the caller which item
    // (if any) gates the option, so it can be consumed on use (10.5.3). Pure C#, testable.
    //
    // VS: no Evolution Items ship, so ownedItems is always empty → this returns exactly the species'
    // level branches and behaviour is unchanged (10.5.4 architecture-only).
    public static class EvolutionOptions
    {
        public readonly struct Option
        {
            public readonly EvolutionBranchSO Branch;
            public readonly EvolutionItemSO SourceItem; // null = reachable by level alone
            public Option(EvolutionBranchSO branch, EvolutionItemSO source) { Branch = branch; SourceItem = source; }
            public bool IsItemGated => SourceItem != null;
        }

        public static List<Option> For(PokemonInstance mon, IReadOnlyList<EvolutionItemSO> ownedItems)
        {
            List<Option> options = new();
            if (mon == null || mon.Species == null) return options;

            // Level-only branches (§5.3.4).
            if (mon.Species.Branches != null)
            {
                foreach (EvolutionBranchSO b in mon.Species.Branches)
                    if (b != null && !Contains(options, b)) options.Add(new Option(b, null));
            }

            // Item-gated branches (§5.3.2) — only if the matching item is held. The item's EnabledBranch
            // must apply to THIS species (its EvolvedSpecies' base is this mon), guarded by the authoring
            // of the item; here we simply offer any owned item's branch not already present.
            if (ownedItems != null)
            {
                foreach (EvolutionItemSO item in ownedItems)
                {
                    if (item == null || item.EnabledBranch == null) continue;
                    if (Contains(options, item.EnabledBranch)) continue;
                    options.Add(new Option(item.EnabledBranch, item));
                }
            }

            return options;
        }

        private static bool Contains(List<Option> list, EvolutionBranchSO branch)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].Branch == branch) return true;
            return false;
        }

        // Task 10.5.3 — consume the gating item after an item-gated evolution is confirmed.
        public static void ConsumeItem(RunStateSO run, EvolutionItemSO item)
        {
            if (run == null || item == null) return;
            run.OwnedEvolutionItems?.Remove(item);
        }
    }
}
