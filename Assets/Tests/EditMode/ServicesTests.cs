using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.14 — Service Locator unit tests.
    public class ServicesTests
    {
        [SetUp]
        public void SetUp()
        {
            Services.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Services.Clear();
        }

        // Per Task 2.1.3
        [Test]
        public void Services_RegisterThenGet_ReturnsInstance()
        {
            var instance = new TestService();
            Services.Register<TestService>(instance);

            TestService result = Services.Get<TestService>();

            Assert.That(result, Is.SameAs(instance));
        }

        // Per Task 2.1.3
        [Test]
        public void Services_Clear_RemovesAll()
        {
            Services.Register<TestService>(new TestService());
            Services.Register<AnotherTestService>(new AnotherTestService());

            Services.Clear();

            Assert.That(Services.Has<TestService>(), Is.False);
            Assert.That(Services.Has<AnotherTestService>(), Is.False);
        }

        [Test]
        public void Services_GetUnregistered_ReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*TestService.*not registered.*"));

            TestService result = Services.Get<TestService>();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Services_RegisterNull_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                Services.Register<TestService>(null));
        }

        [Test]
        public void Services_RegisterTwice_OverwritesPrevious()
        {
            var first = new TestService();
            var second = new TestService();

            Services.Register<TestService>(first);
            Services.Register<TestService>(second);

            Assert.That(Services.Get<TestService>(), Is.SameAs(second));
        }

        [Test]
        public void Services_Has_ReturnsTrueAfterRegister()
        {
            Services.Register<TestService>(new TestService());

            Assert.That(Services.Has<TestService>(), Is.True);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private class TestService { }
        private class AnotherTestService { }
    }
}
