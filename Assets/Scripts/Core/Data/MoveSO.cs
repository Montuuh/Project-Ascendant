using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.2 — Definition SO for a move card. Immutable at runtime.
    // Positional modifiers (SF/SB) only valid on Melee moves — enforced by inspector (Task 3.4.2).
    // APCost 0-4 per §5.3.6; 4-AP only on final-form signature/ultimate moves.
    [CreateAssetMenu(fileName = "New Move", menuName = "Project Ascendant/Moves/Move")]
    public class MoveSO : ScriptableObject
    {
        [Header("Identity")]
        public string MoveId;
        public string DisplayName;

        [Header("Combat Properties")]
        public PokemonType Type;
        public MoveRole Role;
        public MoveRange Range;

        // Per §3.3.1 — Step-Forward and Step-Backward only valid on Melee moves.
        public PositionalModifier Modifier;

        public int BasePower;

        [Range(0, 4)]
        public int APCost;

        // Per §9.3.2.2 — 0.75 for Ranged, 1.0 for Melee. Set via inspector; not hardcoded.
        public float RangeModifierMultiplier = 1f;

        public bool AlwaysCrit;

        [Header("Effects")]
        // Per §9.3.2.2 — secondary effects (riders, debuffs, heals, draw).
        // Primary damage is handled by BasePower + damage formula §4.1.1.
        public List<MoveEffectSO> Effects;

        [Header("Presentation")]
        public Sprite CardArt;

        [TextArea(1, 3)]
        public string FlavorText;

        [Tooltip("GDD section that specifies this move. Per §9.15 bidirectional link.")]
        public string GDDReference;
    }
}
