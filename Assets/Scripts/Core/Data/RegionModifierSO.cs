using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.8.3.1 (CL-016) — the 16 launch Region Modifiers. Each Gym TYPE-agnostic modifier is a
    // transient, per-Region accent (§2.1.4.1 / CL-016): exactly one is active per Region, re-chosen at
    // each Region start. Effect is interpreted from Kind (+ Magnitude) by RegionModifierResolver, which
    // each system queries — mirroring RelicResolver. Numbers are systems-designer-tunable placeholders.
    public enum RegionModifierKind
    {
        HandOfPlenty,     // +Magnitude max hand size
        SturdyLead,       // Lead survives one lethal hit at 1 HP per combat
        TypeAffinity,     // a chosen type +Magnitude damage (chosen type stored on RunState)
        TraumaResistance, // Trauma stacks reduce MaxHP by (5 - Magnitude)% per zone instead of 5/10
        SwapFuel,         // Lead heals Magnitude HP per manual swap
        LuckyDraw,        // +Magnitude consumable card on turn 1 of each combat
        StatusMastery,    // player-applied status conditions last +Magnitude turns
        PocketHealer,     // +Magnitude% team heal on each node's first combat victory
        CoinPurse,        // Poké Dollar drops ×(1 + Magnitude)
        GlassCannon,      // +Magnitude damage dealt AND taken (double-edge)
        QuickStudy,       // all Pokémon gain +Magnitude combat XP
        BargainHunter,    // Shop + Dojo prices ×(1 - Magnitude)
        IronSkin,         // −Magnitude damage from Cleave intents
        MassMobilization, // Step-Forward / Step-Backward also draw 1 card
        PokedexWhisper,   // first Unknown intent of each combat is revealed
        FieldSurveyor,    // player chooses the active neutral Battlefield each wild/Region combat
    }

    public enum RegionModifierTier { Strong, Medium, Niche }

    [CreateAssetMenu(fileName = "New Region Modifier", menuName = "Project Ascendant/World/Region Modifier")]
    public class RegionModifierSO : ScriptableObject
    {
        public string ModifierId;
        public string DisplayName;

        [Tooltip("Effect kind — interpreted by RegionModifierResolver. §7.8.3.1 (CL-016).")]
        public RegionModifierKind Kind;

        [Tooltip("Effect magnitude, interpreted per Kind (e.g. HandOfPlenty=1 card, QuickStudy=0.15, " +
                 "GlassCannon=0.20, SwapFuel=5 HP). Boolean-style kinds ignore this.")]
        public float Magnitude;

        public RegionModifierTier Tier;

        [TextArea(1, 3)]
        public string EffectDescription;
    }
}
