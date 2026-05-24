using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — signals extra card draws via EventContext; DeckManager applies on its next draw step.
    // Example use: Quick Draw relic (+1 card first turn), Reactor Core (max hand size +1).
    [CreateAssetMenu(menuName = "ProjectAscendant/Hooks/DrawCard")]
    public sealed class DrawCardHook : ScriptableHook
    {
        [SerializeField] private int _drawCount = 1;

        public int DrawCount { get => _drawCount; set => _drawCount = value; }

        public override void OnFire(EventContext context)
        {
            context.CardsToDrawBonus += _drawCount;
        }
    }
}
