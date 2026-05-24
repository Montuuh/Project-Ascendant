using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.6 — draws additional skill cards from the deck when the move is played.
    // Wired into DeckManager.DrawPhase in Epic 5.
    [CreateAssetMenu(fileName = "New Draw Cards Effect", menuName = "Project Ascendant/Move Effects/Draw Cards Effect")]
    public class DrawCardsEffectSO : MoveEffectSO
    {
        [Tooltip("Number of extra skill cards to draw this turn.")]
        public int CardsToDrawBonus = 1;
    }
}
