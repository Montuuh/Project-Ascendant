using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 5 Task 5.1.3 — MoveCardInstance owns (Move, Owner, IsMasteryMove,
    // IsExhausted). Reset() must clear every field so the instance is safe to
    // re-issue from the factory when post-VS pooling lands (§9.17).
    public class MoveCardInstanceTests
    {
        [Test]
        public void Factory_OwnerOverload_SetsAllFields()
        {
            MoveSO move = ScriptableObject.CreateInstance<MoveSO>();
            move.MoveId = "ember";
            PokemonInstance owner = new();
            MoveCardInstanceFactory factory = new();

            MoveCardInstance card = factory.Create(move, owner, isMasteryMove: true);

            Assert.That(card.Move, Is.SameAs(move));
            Assert.That(card.Owner, Is.SameAs(owner));
            Assert.That(card.IsMasteryMove, Is.True);
            Assert.That(card.IsExhausted, Is.False);

            Object.DestroyImmediate(move);
        }

        [Test]
        public void Factory_DefaultOwner_IsFalseMasteryFlag()
        {
            MoveSO move = ScriptableObject.CreateInstance<MoveSO>();
            PokemonInstance owner = new();
            MoveCardInstance card = new MoveCardInstanceFactory().Create(move, owner);
            Assert.That(card.IsMasteryMove, Is.False);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void Reset_ClearsAllFields()
        {
            MoveSO move = ScriptableObject.CreateInstance<MoveSO>();
            PokemonInstance owner = new();
            MoveCardInstance card = new()
            {
                Move = move,
                Owner = owner,
                IsMasteryMove = true,
                IsExhausted = true,
            };
            card.Reset();
            Assert.That(card.Move, Is.Null);
            Assert.That(card.Owner, Is.Null);
            Assert.That(card.IsMasteryMove, Is.False);
            Assert.That(card.IsExhausted, Is.False);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void FactoryRelease_NullInstance_NoThrow()
        {
            Assert.DoesNotThrow(() => new MoveCardInstanceFactory().Release(null));
        }
    }
}
