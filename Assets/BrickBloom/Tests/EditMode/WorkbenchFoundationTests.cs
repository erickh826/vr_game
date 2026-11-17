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

        [TestCase(0.0, 0)]
        [TestCase(44.0, 0)]
        [TestCase(46.0, 1)]
        [TestCase(90.0, 1)]
        [TestCase(134.9, 1)]
        [TestCase(180.0, 2)]
        [TestCase(-180.0, 2)]
        [TestCase(-90.0, 3)]
        [TestCase(270.0, 3)]
        [TestCase(316.0, 0)]
        [TestCase(359.0, 0)]
        [TestCase(-1.0, 0)]
        [TestCase(810.0, 1)]
        public void NearestTurnsRoundsYawToAQuarterTurn(double yaw, int expected)
        {
            Assert.That(BrickRules.NearestTurns(yaw), Is.EqualTo(expected));
            Assert.That(BrickRules.ValidTurns(BrickRules.NearestTurns(yaw)), Is.True);
        }

        [Test] public void NearestTurnsRejectsNonFiniteYaw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BrickRules.NearestTurns(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BrickRules.NearestTurns(double.PositiveInfinity));
        }

        [Test] public void NearestTurnsAgreesWithBoardPoseRotation()
        {
            for (int turns = 0; turns < 4; turns++)
            {
                Vector3 forward = BoardPose.ToLocalRotation(turns) * Vector3.forward;
                double yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                Assert.That(BrickRules.NearestTurns(yaw), Is.EqualTo(turns));
            }
        }

        [Test] public void DwellTimerFiresOncePerContinuousTouch()
        {
            var timer = new DwellTimer(1.0);
            Assert.That(timer.Tick(true, 0.4), Is.False);
            Assert.That(timer.Progress, Is.EqualTo(0.4).Within(1e-9));
            Assert.That(timer.Tick(true, 0.4), Is.False);
            Assert.That(timer.Tick(true, 0.4), Is.True);
            Assert.That(timer.IsLatched, Is.True);
            Assert.That(timer.Progress, Is.EqualTo(1.0));
            // Still touching: no repeat.
            for (int i = 0; i < 10; i++) Assert.That(timer.Tick(true, 1.0), Is.False);
            // Lift, then a fresh full dwell is needed.
            Assert.That(timer.Tick(false, 0.1), Is.False);
            Assert.That(timer.IsLatched, Is.False);
            Assert.That(timer.Progress, Is.Zero);
            Assert.That(timer.Tick(true, 0.9), Is.False);
            Assert.That(timer.Tick(true, 0.1), Is.True);
        }

        [Test] public void DwellTimerRestartsWhenTheTouchIsInterruptedAndIgnoresBadDeltas()
        {
            var timer = new DwellTimer(1.0);
            Assert.That(timer.Tick(true, 0.9), Is.False);
            Assert.That(timer.Tick(false, 0.0), Is.False);
            Assert.That(timer.Tick(true, 0.9), Is.False);
            Assert.That(timer.Tick(true, double.NaN), Is.False);
            Assert.That(timer.Tick(true, -5.0), Is.False);
            Assert.That(timer.Tick(true, double.PositiveInfinity), Is.False);
            Assert.That(timer.Progress, Is.EqualTo(0.9).Within(1e-9));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DwellTimer(0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DwellTimer(double.NaN));
        }

        [Test] public void PieceResetLeavesTheBoardAndReturnsToItsSupplyPose()
        {
            var go = new GameObject("piece-under-test");
            try
            {
                var piece = go.AddComponent<BrickPiece>();
                var tray = new Vector3(0.3f, 0.0145f, 0.05f);
                piece.Initialize(null, "p01", BrickKinds.TwoByFour, BrickColors.Red, null, tray, Quaternion.identity);
                int resetEvents = 0;
                bool placedWhenNotified = false;
                piece.ResetRequested += () => { resetEvents++; placedWhenNotified = piece.IsPlaced; };

                piece.PlaceAt(new Vector3(0.05f, 0.0435f, 0.07f), BoardPose.ToLocalRotation(1));
                Assert.That(piece.IsPlaced, Is.True);
                piece.ReturnToTray();
                Assert.That(piece.transform.localPosition.x, Is.EqualTo(0.05f).Within(1e-6f), "a placed piece must not return to the tray");

                piece.ResetToTray();
                Assert.That(resetEvents, Is.EqualTo(1));
                Assert.That(placedWhenNotified, Is.True, "subscribers release XR ownership before the piece state changes");
                Assert.That(piece.IsPlaced, Is.False);
                Assert.That(piece.IsHeld, Is.False);
                Assert.That(Vector3.Distance(piece.transform.localPosition, tray), Is.LessThan(1e-6f));
                Assert.That(Quaternion.Angle(piece.transform.localRotation, Quaternion.identity), Is.LessThan(1e-3f));

                // An unplaced, held piece also ends up in the tray and not held.
                piece.SetHeld(true);
                piece.transform.localPosition = Vector3.one;
                piece.ResetToTray();
                Assert.That(piece.IsHeld, Is.False);
                Assert.That(Vector3.Distance(piece.transform.localPosition, tray), Is.LessThan(1e-6f));
                Assert.That(resetEvents, Is.EqualTo(2));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
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
