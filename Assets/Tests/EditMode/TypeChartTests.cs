using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.3.3 — full Gen I type chart coverage.
    // Every non-1× matchup is asserted via TestCase rows below. Then: dual-type
    // products (the four §4.1.2 worked examples + extras), the famous Gen I
    // quirks, and the OPEN G5 neutrality of Dark/Steel/Fairy.
    public class TypeChartTests
    {
        // ── Single-type matchups: every Gen I non-1× entry ────────────────────
        //
        // Format: (attacker, defender, expected multiplier).
        // Reference: §4.1.2 — Gen I type matrix preserved verbatim, incl. quirks.

        // Normal
        [TestCase(PokemonType.Normal, PokemonType.Rock,    0.5)]
        [TestCase(PokemonType.Normal, PokemonType.Ghost,   0.0)]
        // Fire
        [TestCase(PokemonType.Fire,   PokemonType.Fire,    0.5)]
        [TestCase(PokemonType.Fire,   PokemonType.Water,   0.5)]
        [TestCase(PokemonType.Fire,   PokemonType.Grass,   2.0)]
        [TestCase(PokemonType.Fire,   PokemonType.Ice,     2.0)]
        [TestCase(PokemonType.Fire,   PokemonType.Bug,     2.0)]
        [TestCase(PokemonType.Fire,   PokemonType.Rock,    0.5)]
        [TestCase(PokemonType.Fire,   PokemonType.Dragon,  0.5)]
        // Water
        [TestCase(PokemonType.Water,  PokemonType.Fire,    2.0)]
        [TestCase(PokemonType.Water,  PokemonType.Water,   0.5)]
        [TestCase(PokemonType.Water,  PokemonType.Grass,   0.5)]
        [TestCase(PokemonType.Water,  PokemonType.Ground,  2.0)]
        [TestCase(PokemonType.Water,  PokemonType.Rock,    2.0)]
        [TestCase(PokemonType.Water,  PokemonType.Dragon,  0.5)]
        // Electric
        [TestCase(PokemonType.Electric, PokemonType.Water,    2.0)]
        [TestCase(PokemonType.Electric, PokemonType.Electric, 0.5)]
        [TestCase(PokemonType.Electric, PokemonType.Grass,    0.5)]
        [TestCase(PokemonType.Electric, PokemonType.Ground,   0.0)]
        [TestCase(PokemonType.Electric, PokemonType.Flying,   2.0)]
        [TestCase(PokemonType.Electric, PokemonType.Dragon,   0.5)]
        // Grass
        [TestCase(PokemonType.Grass, PokemonType.Fire,   0.5)]
        [TestCase(PokemonType.Grass, PokemonType.Water,  2.0)]
        [TestCase(PokemonType.Grass, PokemonType.Grass,  0.5)]
        [TestCase(PokemonType.Grass, PokemonType.Poison, 0.5)]
        [TestCase(PokemonType.Grass, PokemonType.Ground, 2.0)]
        [TestCase(PokemonType.Grass, PokemonType.Flying, 0.5)]
        [TestCase(PokemonType.Grass, PokemonType.Bug,    0.5)]
        [TestCase(PokemonType.Grass, PokemonType.Rock,   2.0)]
        [TestCase(PokemonType.Grass, PokemonType.Dragon, 0.5)]
        // Ice
        [TestCase(PokemonType.Ice, PokemonType.Water,  0.5)]
        [TestCase(PokemonType.Ice, PokemonType.Grass,  2.0)]
        [TestCase(PokemonType.Ice, PokemonType.Ice,    0.5)]
        [TestCase(PokemonType.Ice, PokemonType.Ground, 2.0)]
        [TestCase(PokemonType.Ice, PokemonType.Flying, 2.0)]
        [TestCase(PokemonType.Ice, PokemonType.Dragon, 2.0)]
        // Fighting
        [TestCase(PokemonType.Fighting, PokemonType.Normal,  2.0)]
        [TestCase(PokemonType.Fighting, PokemonType.Ice,     2.0)]
        [TestCase(PokemonType.Fighting, PokemonType.Poison,  0.5)]
        [TestCase(PokemonType.Fighting, PokemonType.Flying,  0.5)]
        [TestCase(PokemonType.Fighting, PokemonType.Psychic, 0.5)]
        [TestCase(PokemonType.Fighting, PokemonType.Bug,     0.5)]
        [TestCase(PokemonType.Fighting, PokemonType.Rock,    2.0)]
        [TestCase(PokemonType.Fighting, PokemonType.Ghost,   0.0)]
        // Poison (Gen I: Poison→Bug = 2×)
        [TestCase(PokemonType.Poison, PokemonType.Grass,  2.0)]
        [TestCase(PokemonType.Poison, PokemonType.Poison, 0.5)]
        [TestCase(PokemonType.Poison, PokemonType.Ground, 0.5)]
        [TestCase(PokemonType.Poison, PokemonType.Bug,    2.0)]
        [TestCase(PokemonType.Poison, PokemonType.Rock,   0.5)]
        [TestCase(PokemonType.Poison, PokemonType.Ghost,  0.5)]
        // Ground
        [TestCase(PokemonType.Ground, PokemonType.Fire,     2.0)]
        [TestCase(PokemonType.Ground, PokemonType.Electric, 2.0)]
        [TestCase(PokemonType.Ground, PokemonType.Grass,    0.5)]
        [TestCase(PokemonType.Ground, PokemonType.Poison,   2.0)]
        [TestCase(PokemonType.Ground, PokemonType.Flying,   0.0)]
        [TestCase(PokemonType.Ground, PokemonType.Bug,      0.5)]
        [TestCase(PokemonType.Ground, PokemonType.Rock,     2.0)]
        // Flying
        [TestCase(PokemonType.Flying, PokemonType.Electric, 0.5)]
        [TestCase(PokemonType.Flying, PokemonType.Grass,    2.0)]
        [TestCase(PokemonType.Flying, PokemonType.Fighting, 2.0)]
        [TestCase(PokemonType.Flying, PokemonType.Bug,      2.0)]
        [TestCase(PokemonType.Flying, PokemonType.Rock,     0.5)]
        // Psychic
        [TestCase(PokemonType.Psychic, PokemonType.Fighting, 2.0)]
        [TestCase(PokemonType.Psychic, PokemonType.Poison,   2.0)]
        [TestCase(PokemonType.Psychic, PokemonType.Psychic,  0.5)]
        // Bug (Gen I: Bug→Poison = 2×)
        [TestCase(PokemonType.Bug, PokemonType.Fire,     0.5)]
        [TestCase(PokemonType.Bug, PokemonType.Grass,    2.0)]
        [TestCase(PokemonType.Bug, PokemonType.Fighting, 0.5)]
        [TestCase(PokemonType.Bug, PokemonType.Poison,   2.0)]
        [TestCase(PokemonType.Bug, PokemonType.Flying,   0.5)]
        [TestCase(PokemonType.Bug, PokemonType.Psychic,  2.0)]
        [TestCase(PokemonType.Bug, PokemonType.Ghost,    0.5)]
        // Rock
        [TestCase(PokemonType.Rock, PokemonType.Fire,     2.0)]
        [TestCase(PokemonType.Rock, PokemonType.Ice,      2.0)]
        [TestCase(PokemonType.Rock, PokemonType.Fighting, 0.5)]
        [TestCase(PokemonType.Rock, PokemonType.Ground,   0.5)]
        [TestCase(PokemonType.Rock, PokemonType.Flying,   2.0)]
        [TestCase(PokemonType.Rock, PokemonType.Bug,      2.0)]
        // Ghost (Gen I: Ghost→Psychic = 0 — famous bug, intentional per spec)
        [TestCase(PokemonType.Ghost, PokemonType.Normal,  0.0)]
        [TestCase(PokemonType.Ghost, PokemonType.Psychic, 0.0)]
        [TestCase(PokemonType.Ghost, PokemonType.Ghost,   2.0)]
        // Dragon
        [TestCase(PokemonType.Dragon, PokemonType.Dragon, 2.0)]
        public void Chart_SingleMatchup_MatchesGenI(PokemonType atk, PokemonType def, double expected)
        {
            // Per §4.1.2 — Gen I matrix preserved verbatim.
            Assert.That(TypeChart.GetSingle(atk, def), Is.EqualTo(expected).Within(0.001));
        }

        // ── Sanity: neutral matchups (1×) ─────────────────────────────────────

        [TestCase(PokemonType.Normal, PokemonType.Normal, 1.0)]
        [TestCase(PokemonType.Water,  PokemonType.Normal, 1.0)]
        [TestCase(PokemonType.Fire,   PokemonType.Normal, 1.0)]
        [TestCase(PokemonType.Normal, PokemonType.Fire,   1.0)]
        public void Chart_NeutralMatchup_Returns1x(PokemonType atk, PokemonType def, double expected)
        {
            // Per §4.1.2 — unaspected matchups resolve at neutral 1.0×.
            Assert.That(TypeChart.GetSingle(atk, def), Is.EqualTo(expected).Within(0.001));
        }

        // ── Dual-type products — the four §4.1.2 worked examples ──────────────

        [Test]
        public void DualType_FireVsGrassPoison_2x()
        {
            // Per §4.1.2 worked example 1 — F→Grass 2× × F→Poison 1× = 2×.
            var defenders = new List<PokemonType> { PokemonType.Grass, PokemonType.Poison };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Fire, defenders),
                Is.EqualTo(2.0).Within(0.001));
        }

        [Test]
        public void DualType_ElectricVsWaterFlying_4x()
        {
            // Per §4.1.2 worked example 2 — E→Water 2× × E→Flying 2× = 4×.
            var defenders = new List<PokemonType> { PokemonType.Water, PokemonType.Flying };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Electric, defenders),
                Is.EqualTo(4.0).Within(0.001));
        }

        [Test]
        public void DualType_GroundVsWaterRock_2x_PerOpenG6Correction()
        {
            // Per OPEN G6 — spec worked example 3 ("0.5×") contains an arithmetic
            // error. Canonical Gen I: Ground→Water 1× × Ground→Rock 2× = 2.0×.
            var defenders = new List<PokemonType> { PokemonType.Water, PokemonType.Rock };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Ground, defenders),
                Is.EqualTo(2.0).Within(0.001));
        }

        [Test]
        public void DualType_NormalVsGhostPoison_Zero_ImmunityWins()
        {
            // Per §4.1.2 worked example 4 — N→Ghost 0× × N→Poison 1× = 0×.
            var defenders = new List<PokemonType> { PokemonType.Ghost, PokemonType.Poison };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Normal, defenders),
                Is.EqualTo(0.0).Within(0.001));
        }

        // ── Dual-type extra cases ─────────────────────────────────────────────

        [Test]
        public void DualType_IceVsGrassDragon_4x()
        {
            // Per §4.1.2 — Ice 2× vs Grass and 2× vs Dragon → 4× total.
            var defenders = new List<PokemonType> { PokemonType.Grass, PokemonType.Dragon };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Ice, defenders),
                Is.EqualTo(4.0).Within(0.001));
        }

        [Test]
        public void DualType_GrassVsFireFlying_QuarterEffective()
        {
            // Per §4.1.2 — Grass 0.5× vs Fire and 0.5× vs Flying → 0.25×.
            var defenders = new List<PokemonType> { PokemonType.Fire, PokemonType.Flying };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Grass, defenders),
                Is.EqualTo(0.25).Within(0.001));
        }

        [Test]
        public void DualType_FightingVsGhostNormal_Zero()
        {
            // Per §4.1.2 — Fighting→Ghost 0× short-circuits the product.
            var defenders = new List<PokemonType> { PokemonType.Ghost, PokemonType.Normal };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Fighting, defenders),
                Is.EqualTo(0.0).Within(0.001));
        }

        // ── Defender-list edge cases ──────────────────────────────────────────

        [Test]
        public void GetMultiplier_NullDefenders_ReturnsNeutral()
        {
            // Per §4.1.2 — null/empty defender list defaults to 1.0× (safe default).
            Assert.That(TypeChart.GetMultiplier(PokemonType.Fire, null),
                Is.EqualTo(1.0).Within(0.001));
        }

        [Test]
        public void GetMultiplier_EmptyDefenders_ReturnsNeutral()
        {
            Assert.That(TypeChart.GetMultiplier(PokemonType.Fire, new List<PokemonType>()),
                Is.EqualTo(1.0).Within(0.001));
        }

        [Test]
        public void GetMultiplier_SingleType_MatchesGetSingle()
        {
            // The 1-element list path must agree with the GetSingle lookup.
            var defs = new List<PokemonType> { PokemonType.Grass };
            Assert.That(TypeChart.GetMultiplier(PokemonType.Fire, defs),
                Is.EqualTo(TypeChart.GetSingle(PokemonType.Fire, PokemonType.Grass)).Within(0.001));
        }

        // ── OPEN G5: Dark/Steel/Fairy default neutrality ──────────────────────

        [TestCase(PokemonType.Dark)]
        [TestCase(PokemonType.Steel)]
        [TestCase(PokemonType.Fairy)]
        public void OpenG5_PostGen1Type_AsAttacker_AllNeutral(PokemonType atk)
        {
            // Per OPEN G5 — Dark/Steel/Fairy rows are kept at 1.0× until the enum
            // is trimmed or the chart is extended to Gen VI.
            foreach (PokemonType def in System.Enum.GetValues(typeof(PokemonType)))
                Assert.That(TypeChart.GetSingle(atk, def), Is.EqualTo(1.0).Within(0.001),
                    $"Expected neutral for {atk}→{def} pending OPEN G5 ratification.");
        }

        [TestCase(PokemonType.Dark)]
        [TestCase(PokemonType.Steel)]
        [TestCase(PokemonType.Fairy)]
        public void OpenG5_PostGen1Type_AsDefender_AllNeutral(PokemonType def)
        {
            // Symmetric — columns also held at 1.0×.
            foreach (PokemonType atk in System.Enum.GetValues(typeof(PokemonType)))
                Assert.That(TypeChart.GetSingle(atk, def), Is.EqualTo(1.0).Within(0.001),
                    $"Expected neutral for {atk}→{def} pending OPEN G5 ratification.");
        }

        // ── Gen I quirks — assert explicitly so a future "fix" surfaces in CI ──

        [Test]
        public void GenI_GhostVsPsychic_IsZero_NotTwo()
        {
            // Per §4.1.2 ("Gen I matrix exactly") — famous Gen I bug preserved.
            // Modern gens corrected this to 2×; we ship 0× per spec.
            Assert.That(TypeChart.GetSingle(PokemonType.Ghost, PokemonType.Psychic),
                Is.EqualTo(0.0).Within(0.001));
        }

        [Test]
        public void GenI_BugVsPoison_IsTwo_NotHalf()
        {
            // Gen I quirk: Bug→Poison super-effective (changed to 0.5× in Gen II).
            Assert.That(TypeChart.GetSingle(PokemonType.Bug, PokemonType.Poison),
                Is.EqualTo(2.0).Within(0.001));
        }

        [Test]
        public void GenI_PoisonVsBug_IsTwo_NotNeutral()
        {
            // Gen I quirk: Poison→Bug super-effective (changed to 1.0× in Gen II).
            Assert.That(TypeChart.GetSingle(PokemonType.Poison, PokemonType.Bug),
                Is.EqualTo(2.0).Within(0.001));
        }
    }
}
