using System;
using System.IO;
using System.Linq;
using BrickBloom.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace BrickBloom.Tests
{
    public sealed class WorkbenchFoundationTests
    {
        private static string ExampleJson() => File.ReadAllText(
            Path.Combine(Application.dataPath, "BrickBloom/Data/Blueprints/bridge-001.json"));

        [Test] public void LoaderAcceptsTheBridgeFixture()
        {
            Assert.That(BlueprintLoader.TryLoad(ExampleJson(), out var blueprint, out var error), Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(blueprint.id, Is.EqualTo("bridge-001"));
            Assert.That(blueprint.bricks.Length, Is.EqualTo(6));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void LoaderRejectsEmptyInput(string json)
        {
            Assert.That(BlueprintLoader.TryLoad(json, out var blueprint, out var error), Is.False);
            Assert.That(blueprint, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase("{ this is not json")]
        [TestCase("[1,2,3]")]
        [TestCase("null")]
        [TestCase("{}")]
        public void LoaderRejectsMalformedOrIncompleteJsonWithoutThrowing(string json)
        {
            Assert.That(BlueprintLoader.TryLoad(json, out var blueprint, out var error), Is.False);
            Assert.That(blueprint, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [Test] public void LoaderRejectsABlueprintThatFailsValidation()
        {
            string overlapping = ExampleJson().Replace("\"x\": 4,\n      \"y\": 0", "\"x\": 2,\n      \"y\": 0")
                .Replace("\"x\": 4,\r\n      \"y\": 0", "\"x\": 2,\r\n      \"y\": 0");
            Assert.That(overlapping, Is.Not.EqualTo(ExampleJson()), "fixture edit did not apply");
            Assert.That(BlueprintLoader.TryLoad(overlapping, out var blueprint, out var error), Is.False);
            Assert.That(blueprint, Is.Null);
            Assert.That(error, Does.Contain("overlap"));
        }

        [Test] public void SupplyPositionsAreOutsideTheBoardAndDoNotOverlap()
        {
            const int boardWidth = 8, count = 12;
            double boardEdge = boardWidth * BrickDefinition.StudPitch;
            // Largest supply footprint: a 2x4 at turns 0 (2 studs along X, 4 along Z).
            double halfX = BrickDefinition.StudPitch, halfZ = 2 * BrickDefinition.StudPitch;
            var centers = Enumerable.Range(0, count).Select(i => SupplyLayout.Center(i, boardWidth)).ToArray();
            foreach (var center in centers)
            {
                Assert.That(center.IsFinite, Is.True);
                Assert.That(center.X - halfX, Is.GreaterThan(boardEdge));
                Assert.That(center.Z - halfZ, Is.GreaterThanOrEqualTo(-1e-9));
                Assert.That(center.Y, Is.EqualTo(0.5 * BrickDefinition.LayerHeight).Within(1e-12));
            }

            for (int i = 0; i < count; i++)
                for (int j = i + 1; j < count; j++)
                {
                    bool separated = Math.Abs(centers[i].X - centers[j].X) >= 2 * halfX - 1e-9 ||
                                     Math.Abs(centers[i].Z - centers[j].Z) >= 2 * halfZ - 1e-9;
                    Assert.That(separated, Is.True, $"supply {i} overlaps {j}");
                }
        }

        [Test] public void SupplyLayoutIsDeterministicAndRejectsInvalidArguments()
        {
            var a = SupplyLayout.Center(4, 8);
            var b = SupplyLayout.Center(4, 8);
            Assert.That(new[] { a.X, a.Y, a.Z }, Is.EqualTo(new[] { b.X, b.Y, b.Z }));
            Assert.Throws<ArgumentOutOfRangeException>(() => SupplyLayout.Center(-1, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => SupplyLayout.Center(0, 0));
        }

        [Test] public void SupplyPiecesCannotSnapFromTheirSupplyPose()
        {
            Assert.That(BlueprintLoader.TryLoad(ExampleJson(), out var blueprint, out _), Is.True);
            var board = new BrickBoard(blueprint);
            var resolver = new SnapResolver();
            for (int i = 0; i < blueprint.bricks.Length; i++)
            {
                var slot = blueprint.bricks[i];
                Assert.That(board.TryRegisterPiece("p" + i, slot.kind, slot.color), Is.True);
                Assert.That(resolver.Resolve(board, "p" + i, 0, SupplyLayout.Center(i, blueprint.boardWidth)), Is.Null);
            }
        }

        [Test] public void BoardPoseRoundTripsPositionsAndQuarterTurns()
        {
            var point = SnapResolver.Center(new BrickSlot { kind = BrickKinds.TwoByFour, x = 2, y = 1, z = 4, turns = 1 });
            Vector3 local = BoardPose.ToLocalPosition(point);
            var back = BoardPose.ToBoardPoint(local);
            Assert.That(back.X, Is.EqualTo(point.X).Within(1e-6));
            Assert.That(back.Y, Is.EqualTo(point.Y).Within(1e-6));
            Assert.That(back.Z, Is.EqualTo(point.Z).Within(1e-6));
            // One quarter turn maps the long (local Z) axis onto board X.
            Vector3 turned = BoardPose.ToLocalRotation(1) * Vector3.forward;
            Assert.That(Mathf.Abs(turned.x), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(BoardPose.ToLocalRotation(0), Is.EqualTo(Quaternion.identity));
        }
    }
}
