using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.9.2 + Epic 9 Task 9.7 — numeric tuning for the 4 VS Mystery Events. Item references
    // (potion, relic pool, etc.) are supplied to the controller as pools; only balance numbers live
    // here (all ints — no float literals, PA0001). Win odds use MysteryRNG, not a stored probability.
    [CreateAssetMenu(fileName = "MysteryConfig", menuName = "Project Ascendant/Config/Mystery Config")]
    public class MysteryConfigSO : ScriptableObject
    {
        [Header("Berry Bush — §7.9.2")]
        [Tooltip("Percent of EffectiveMaxHP restored to all Box Pokémon (eat-now choice).")]
        public int BerryBushHealPercent = 30;
        [Tooltip("Number of Potions granted (take-berries choice).")]
        public int BerryBushPotionCount = 3;

        [Header("Wandering Tutor — §7.9.2")]
        [Tooltip("Poké Dollars granted when declining the tutor.")]
        public int WanderingTutorDeclineDollars = 100;

        [Header("Slot Booth — §7.9.2 (Gamble)")]
        [Tooltip("Poké Dollars wagered (must be affordable to play).")]
        public int SlotBoothWager = 100;
        [Tooltip("Poké Dollars paid out on a win (gross).")]
        public int SlotBoothWinAmount = 250;
    }
}
