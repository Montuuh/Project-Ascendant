using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.2 — Definition SO for a move. Immutable at runtime.
    // This is a stub for Epic 2 factory infrastructure.
    // TODO: Epic 3 — expand with full schema: Type, Role, Range, Modifier, Effects,
    //       RangeModifierMultiplier, AlwaysCrit, CardArt, FlavorText.
    [CreateAssetMenu(fileName = "New Move", menuName = "ProjectAscendant/Data/Move")]
    public class MoveSO : ScriptableObject
    {
        public string MoveId;
        public string DisplayName;
        public int APCost;
        public int BasePower;
    }
}
