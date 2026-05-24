using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §4.4.5 — Definition SO for a Gym Badge. Earned from Gym Leader victories.
    // Badges provide persistent run-wide passive effects via ScriptableHook.
    // Some abilities and items have explicit Badge synergies (e.g. §5.6 Shell Armor + Boulder Badge).
    [CreateAssetMenu(fileName = "New Badge", menuName = "Project Ascendant/Data/Badge")]
    public class BadgeSO : ScriptableObject
    {
        [Header("Identity")]
        public string BadgeId;
        public string DisplayName;

        [Tooltip("Region and Gym number, e.g. 'R1-Gym1'.")]
        public string GymSource;

        public Sprite Icon;

        [Header("Effect")]
        // Per §4.4.5 — persistent run-wide effect wired via HookSubscriber in Epic 4.
        public ScriptableHook GrantedHook;

        [TextArea(1, 3)]
        [Tooltip("Human-readable effect summary shown in the Badge case UI.")]
        public string EffectDescription;

        [Tooltip("GDD section for this badge. Per §9.15.")]
        public string GDDReference;
    }
}
