using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BrickBloom.Tests
{
    public sealed class FoundationTests
    {
        private static Blueprint Example() => JsonUtility.FromJson<Blueprint>(File.ReadAllText(
            Path.Combine(Application.dataPath, "BrickBloom/Data/Blueprints/bridge-001.json")));
        private static BrickSlot Slot(string id, int x = 0, int y = 0) =>
            new BrickSlot { id = id, kind = "2x2", color = "red", x = x, y = y };
        private static Blueprint Plan(params BrickSlot[] slots) => new Blueprint
            { schemaVersion = 1, id = "test", boardWidth = 8, boardDepth = 8, bricks = slots };

        [Test] public void ExampleBuildsBottomUpAndResets()
        {
            var data = Example();
            Assert.That(BlueprintValidator.Validate(data), Is.Empty);
            var board = new BrickBoard(data);
            Assert.That(board.TotalCount, Is.EqualTo(6));
            Assert.That(board.TryPlace("b03", "2x4", "white", 1), Is.False);
            foreach (var slot in data.bricks)
                Assert.That(new SnapResolver().TrySnap(board, slot.kind, slot.color, slot.turns,
                    SnapResolver.Center(slot), out _), Is.True, slot.id);
            Assert.That(board.IsComplete, Is.True);
            board.Reset();
            Assert.That(board.PlacedCount, Is.Zero);
            Assert.That(board.IsComplete, Is.False);
        }
        [Test] public void InputOrderDoesNotDetermineBuildability()
        {
            var data = Example(); Array.Reverse(data.bricks);
            Assert.That(BlueprintValidator.Validate(data), Is.Empty);
        }
        [TestCase("kind")][TestCase("color")][TestCase("rotation")][TestCase("bounds")]
        [TestCase("negative")][TestCase("duplicate")][TestCase("overlap")][TestCase("unsupported")]
        [TestCase("schema")][TestCase("null")][TestCase("empty")][TestCase("overflow")]
        [TestCase("blankId")][TestCase("dimensions")]
        public void InvalidBlueprintsAreRejected(string fault)
        {
            var data = Example();
            switch (fault)
            {
                case "kind": data.bricks[0].kind = "3x3"; break;
                case "color": data.bricks[0].color = "green"; break;
                case "rotation": data.bricks[0].turns = 4; break;
                case "bounds": data.bricks[0].x = 7; break;
                case "negative": data.bricks[0].y = -1; break;
                case "duplicate": data.bricks[1].id = data.bricks[0].id; break;
                case "overlap": data.bricks[1].x = 2; break;
                case "unsupported": data.bricks[0].y = 4; break;
                case "schema": data.schemaVersion = 2; break;
                case "null": data.bricks[0] = null; break;
                case "empty": data.bricks = Array.Empty<BrickSlot>(); break;
                case "overflow": data.bricks[0].x = int.MaxValue; break;
                case "blankId": data.bricks[0].id = " "; break;
                case "dimensions": data.boardWidth = 0; break;
            }
            Assert.That(BlueprintValidator.Validate(data), Is.Not.Empty);
            Assert.Throws<ArgumentException>(() => new BrickBoard(data));
        }
        [Test] public void MissingBlueprintIsRejected() => Assert.That(BlueprintValidator.Validate(null), Is.Not.Empty);
        [Test] public void PartialSupportIsRejected()
        {
            var upper = Slot("upper", 0, 1); upper.kind = "2x4";
            Assert.That(BlueprintValidator.Validate(Plan(Slot("lower"), upper)), Is.Not.Empty);
        }
        [Test] public void BridgeRequiresBothSupports()
        {
            var board = new BrickBoard(Example());
            Assert.That(board.TryPlace("b01", "2x4", "red", 0), Is.True);
            Assert.That(board.CanPlace("b03", "2x4", "white", 1), Is.False);
            Assert.That(board.TryPlace("b02", "2x4", "blue", 0), Is.True);
            Assert.That(board.CanPlace("b03", "2x4", "white", 3), Is.True);
        }
        [Test] public void RotationEquivalenceIsGeometric()
        {
            for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++)
            {
                Assert.That(BrickRules.EquivalentRotation("2x2", a, b), Is.True);
                Assert.That(BrickRules.EquivalentRotation("2x4", a, b), Is.EqualTo(a % 2 == b % 2));
            }
            Assert.That(BrickRules.EquivalentRotation("2x2", -1, 0), Is.False);
            Assert.That(BrickRules.EquivalentRotation("2x4", 0, 4), Is.False);
        }
        [Test] public void WrongIdentityAndOccupiedSlotCannotSnap()
        {
            var board = new BrickBoard(Plan(Slot("a")));
            var resolver = new SnapResolver(); var center = SnapResolver.Center(Slot("a"));
            Assert.That(resolver.Resolve(board, "2x4", "red", 0, center), Is.Null);
            Assert.That(resolver.Resolve(board, "2x2", "blue", 0, center), Is.Null);
            Assert.That(resolver.Resolve(board, "2x2", "red", 4, center), Is.Null);
            Assert.That(resolver.TrySnap(board, "2x2", "red", 3, center, out _), Is.True);
            Assert.That(resolver.TrySnap(board, "2x2", "red", 0, center, out _), Is.False);
        }
        [Test] public void ClosestCandidateWinsAndPreviewDoesNotMutate()
        {
            var board = new BrickBoard(Plan(Slot("first"), Slot("closer", 2)));
            var result = new SnapResolver().Resolve(board, "2x2", "red", 0, new BoardPoint(.05, .0145, .024));
            Assert.That(result.id, Is.EqualTo("closer"));
            Assert.That(board.PlacedCount, Is.Zero);
        }
        [Test] public void ExactTieUsesOrdinalId()
        {
            var first = Slot("z"); first.z = 2;
            var board = new BrickBoard(Plan(first, Slot("a", 2)));
            Assert.That(new SnapResolver(1, 1).Resolve(board, "2x2", "red", 0,
                new BoardPoint(.024, .0145, .024)).id, Is.EqualTo("a"));
        }
        [TestCase(.0349, 0, 0, true)][TestCase(.0351, 0, 0, false)]
        [TestCase(0, .0319, 0, true)][TestCase(0, .0321, 0, false)]
        [TestCase(.03, 0, .03, false)]
        public void TolerancesUseHorizontalRadiusAndIndependentHeight(double x, double y, double z, bool expected)
        {
            var slot = Slot("a"); var center = SnapResolver.Center(slot);
            Assert.That(new SnapResolver().Resolve(new BrickBoard(Plan(slot)), "2x2", "red", 0,
                new BoardPoint(center.X + x, center.Y + y, center.Z + z)) != null, Is.EqualTo(expected));
        }
        [Test] public void InvalidPositionsAndTolerancesAreRejected()
        {
            var board = new BrickBoard(Plan(Slot("a")));
            Assert.That(new SnapResolver().Resolve(board, "2x2", "red", 0, new BoardPoint(double.NaN, 0, 0)), Is.Null);
            Assert.That(new SnapResolver().Resolve(board, "2x2", "red", 0, new BoardPoint(0, double.PositiveInfinity, 0)), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SnapResolver(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SnapResolver(double.NaN));
        }
        [Test] public void BoardSnapshotsPreventExternalMutation()
        {
            var data = Plan(Slot("a")); var board = new BrickBoard(data);
            data.bricks[0].color = "blue"; board.Slots[0].color = "yellow";
            Assert.That(board.TryPlace("a", "2x2", "red", 0), Is.True);
        }
        [Test] public void RotatedFootprintAndCenterUseBoardLocalCoordinates()
        {
            var slot = new BrickSlot { id = "a", kind = "2x4", color = "red", x = 4, z = 6, turns = 1 };
            Assert.That(BlueprintValidator.Validate(Plan(slot)), Is.Empty);
            Assert.That(SnapResolver.Center(slot).X, Is.EqualTo(.144).Within(1e-10));
            slot.turns = 0;
            Assert.That(BlueprintValidator.Validate(Plan(slot)), Is.Not.Empty);
        }
    }
}
