using NUnit.Framework;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §3.3 + Epic 6 — PlayerAction factory contracts. The UI (CombatScreenUI)
    // drives the controller exclusively through these factories, so the struct each
    // one builds is load-bearing: a wrong Kind silently no-ops in ExecuteAction.
    public class PlayerActionTests
    {
        [Test]
        // Per §3.3.1 — manual swap targets a bench slot to promote to Lead.
        public void ManualSwap_BuildsManualSwapAction_WithBenchSlot()
        {
            PlayerAction a = PlayerAction.ManualSwap(2);
            Assert.That(a.Kind, Is.EqualTo(PlayerActionKind.ManualSwap));
            Assert.That(a.SwapToBenchSlot, Is.EqualTo(2));
        }

        [Test]
        public void PlaySkill_SetsKindIndexAndTarget()
        {
            PlayerAction a = PlayerAction.PlaySkill(handIndex: 1, enemySlot: 2);
            Assert.That(a.Kind, Is.EqualTo(PlayerActionKind.PlaySkill));
            Assert.That(a.CardIndex, Is.EqualTo(1));
            Assert.That(a.TargetEnemySlot, Is.EqualTo(2));
        }

        [Test]
        public void PlayConsumable_SetsKindAndIndex()
        {
            PlayerAction a = PlayerAction.PlayConsumable(3);
            Assert.That(a.Kind, Is.EqualTo(PlayerActionKind.PlayConsumable));
            Assert.That(a.CardIndex, Is.EqualTo(3));
        }

        [Test]
        public void End_SetsEndTurnKind()
        {
            Assert.That(PlayerAction.End().Kind, Is.EqualTo(PlayerActionKind.EndTurn));
        }
    }
}
