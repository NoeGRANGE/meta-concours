using System.IO;
using System.Threading.Tasks;
using Deskmates.Desk;
using NUnit.Framework;
using UnityEngine;

namespace Deskmates.Tests
{
    /// <summary>Desk plane maths, the fall with bounces and the saved desks (PRD 5.1 and 5.2).</summary>
    public class DeskGeometryTests
    {
        [Test]
        public void PlaneConvertsBetweenWorldAndDeskFrames()
        {
            DeskPlane plane = new DeskPlane(new Vector3(1f, 0.75f, 2f), 90f, new Vector2(0.6f, 0.35f));
            Vector3 world = new Vector3(1.3f, 0.9f, 1.8f);
            Assert.That(Vector3.Distance(world, plane.ToWorld(plane.ToLocal(world))), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(new Vector3(1.2f, 0.75f, 2f), plane.ToWorld(new Vector3(0f, 0f, 0.2f))),
                Is.LessThan(1e-5f), "With a yaw of 90 degrees, +Z of the desk points along world +X.");
        }

        [Test]
        public void ClampingKeepsPointsInTheZoneAndOnTheSurface()
        {
            DeskPlane plane = new DeskPlane(new Vector3(0f, 0.75f, 0f), 0f, new Vector2(0.6f, 0.35f));
            Vector3 clamped = plane.ClampToZone(new Vector3(2f, 1.3f, -1f), 0.05f);
            Assert.AreEqual(new Vector3(0.55f, 0.75f, -0.3f).ToString("F3"), clamped.ToString("F3"));
            Assert.IsTrue(plane.Contains(clamped));
            Assert.IsFalse(plane.Contains(new Vector3(0.7f, 0.75f, 0f)));
        }

        [Test]
        public void ADropFallsBouncesAndSettlesOnTheSurface()
        {
            DropMotion drop = new DropMotion(0.2f);
            int bounces = 0;
            drop.Bounced += _ => bounces++;
            float elapsed = 0f;
            while (!drop.Settled && elapsed < 2f)
            {
                drop.Step(1f / 72f);
                elapsed += 1f / 72f;
                Assert.GreaterOrEqual(drop.Height, 0f, "Never below the surface.");
            }

            Assert.IsTrue(drop.Settled);
            Assert.AreEqual(0f, drop.Height);
            Assert.That(bounces, Is.InRange(2, 4), "A couple of bounces, not one thud nor endless ones.");
            Assert.Less(elapsed, 1f, "It comes to rest in under a second.");
        }

        [Test]
        public void ADropAtOrBelowTheSurfaceIsAlreadySettled()
        {
            Assert.IsTrue(new DropMotion(0f).Settled);
            Assert.IsTrue(new DropMotion(-0.1f).Settled);
        }

        [Test]
        public async Task DesksSurviveASaveAndLoad()
        {
            string path = Path.Combine(Path.GetTempPath(), $"desks-{System.Guid.NewGuid()}.json");
            try
            {
                DeskStore store = new DeskStore(path);
                DeskRecord home = new DeskRecord { anchorId = System.Guid.NewGuid().ToString(), halfSize = new Vector2(0.6f, 0.35f) };
                home.SetPlacement("mail", new Vector3(0.1f, 0f, 0.2f), 180f);
                home.SetPlacement("mail", new Vector3(0.2f, 0f, 0.2f), 170f);
                store.Desks.Add(home);
                store.Desks.Add(new DeskRecord { anchorId = System.Guid.NewGuid().ToString() });
                await store.SaveAsync();

                DeskStore reloaded = new DeskStore(path);
                await reloaded.LoadAsync();
                Assert.AreEqual(2, reloaded.Desks.Count);
                Assert.AreEqual(home.AnchorId, reloaded.Desks[0].AnchorId);
                Assert.AreEqual(1, reloaded.Desks[0].creatures.Count, "Placing a creature again replaces its placement.");
                Assert.AreEqual(170f, reloaded.Desks[0].Find("mail").localYaw);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task ACorruptFileStartsEmpty()
        {
            string path = Path.Combine(Path.GetTempPath(), $"desks-{System.Guid.NewGuid()}.json");
            File.WriteAllText(path, "{ not json");
            try
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
                DeskStore store = new DeskStore(path);
                await store.LoadAsync();
                Assert.IsEmpty(store.Desks);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
