using System;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §8.7 + Task 2.8.4 — ScriptableHook subclass unit tests.
    public class ScriptableHookTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        // ── Hook subclass behaviours ─────────────────────────────────────────────

        [Test]
        public void ModifyDamageHook_OnFire_AppliesMultiplierAndBonus()
        {
            // Per §8.7 — Soft Sand/type-boost pattern: multiplier × bonus applied to context.
            ModifyDamageHook hook = ScriptableObject.CreateInstance<ModifyDamageHook>();
            hook.Multiplier = 1.15f;
            hook.FlatBonus  = 5;

            EventContext ctx = new() { DamageMultiplier = 1f };
            hook.OnFire(ctx);

            Assert.That(ctx.DamageMultiplier, Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(ctx.FlatDamageBonus, Is.EqualTo(5));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        [Test]
        public void ModifyDamageHook_OnFire_StacksMultipleInvocations()
        {
            // Stacking relics must compound: 1.15 × 1.15 = 1.3225.
            ModifyDamageHook hook = ScriptableObject.CreateInstance<ModifyDamageHook>();
            hook.Multiplier = 1.15f;

            EventContext ctx = new() { DamageMultiplier = 1f };
            hook.OnFire(ctx);
            hook.OnFire(ctx);

            Assert.That(ctx.DamageMultiplier, Is.EqualTo(1.15f * 1.15f).Within(0.0001f));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        [Test]
        public void DrawCardHook_OnFire_IncrementsCardsToDrawBonus()
        {
            // Per §8.7 — Quick Draw pattern: +N cards to draw this turn.
            DrawCardHook hook = ScriptableObject.CreateInstance<DrawCardHook>();
            hook.DrawCount = 2;

            EventContext ctx = new();
            hook.OnFire(ctx);

            Assert.That(ctx.CardsToDrawBonus, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        [Test]
        public void HealHook_OnFire_IncrementsHealAmount()
        {
            // Per §8.7 — Leftovers/Berry Pouch pattern.
            HealHook hook = ScriptableObject.CreateInstance<HealHook>();
            hook.Amount = 25;

            EventContext ctx = new();
            hook.OnFire(ctx);

            Assert.That(ctx.HealAmount, Is.EqualTo(25));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        [Test]
        public void ApplyStatusHook_OnFire_SetsStatusToApply()
        {
            // Per §8.7 — Wide Lens / status-rider ability pattern.
            ApplyStatusHook hook = ScriptableObject.CreateInstance<ApplyStatusHook>();
            hook.Status = StatusCondition.Poison;

            EventContext ctx = new();
            hook.OnFire(ctx);

            Assert.That(ctx.StatusToApply, Is.EqualTo(StatusCondition.Poison));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        [Test]
        public void GrantAPHook_OnFire_IncrementsAPGranted()
        {
            // Per §8.7 — Cycle Cell / Move Echo pattern: +1 AP on trigger.
            GrantAPHook hook = ScriptableObject.CreateInstance<GrantAPHook>();
            hook.Amount = 1;

            EventContext ctx = new();
            hook.OnFire(ctx);

            Assert.That(ctx.APGranted, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        [Test]
        public void BuffStatHook_OnFire_SetsStatAndStage()
        {
            // Per §8.7 — Defense Curl Charm / Adrenal Surge pattern.
            BuffStatHook hook = ScriptableObject.CreateInstance<BuffStatHook>();
            hook.Stat   = Stat.Attack;
            hook.Stages = 2;

            EventContext ctx = new();
            hook.OnFire(ctx);

            Assert.That(ctx.StatTarget,      Is.EqualTo(Stat.Attack));
            Assert.That(ctx.StatStageChange, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(hook);
        }

        // ── HookSubscriber wiring ────────────────────────────────────────────────

        [Test]
        public void HookSubscriber_Subscribe_FiresHookOnEventBusPublish()
        {
            // Per §8.7 — wiring pattern: hook fires when subscribed EventBus event is published.
            HealHook hook = ScriptableObject.CreateInstance<HealHook>();
            hook.Amount = 30;

            EventContext capturedCtx = new();
            Action<TurnContext> handler = HookSubscriber.Subscribe<TurnContext>(
                hook,
                _ => capturedCtx);

            try
            {
                EventBus.Publish(new TurnContext());
            }
            finally
            {
                EventBus.Unsubscribe<TurnContext>(handler);
            }

            Assert.That(capturedCtx.HealAmount, Is.EqualTo(30));
            UnityEngine.Object.DestroyImmediate(hook);
        }
    }
}
