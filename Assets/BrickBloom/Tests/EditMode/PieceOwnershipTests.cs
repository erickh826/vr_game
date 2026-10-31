using NUnit.Framework;

namespace BrickBloom.Tests
{
    public sealed class PieceOwnershipTests
    {
        private static BrickBoard Board()
        {
            var board = new BrickBoard(new Blueprint
            {
                schemaVersion = 1, id = "ownership", boardWidth = 8, boardDepth = 8,
                bricks = new[]
                {
                    new BrickSlot { id = "a", kind = BrickKinds.TwoByTwo, color = BrickColors.Red },
                    new BrickSlot { id = "b", kind = BrickKinds.TwoByTwo, color = BrickColors.Red, x = 2 }
                }
            });
            Assert.That(board.TryRegisterPiece("physical-1", BrickKinds.TwoByTwo, BrickColors.Red), Is.True);
            Assert.That(board.TryRegisterPiece("physical-2", BrickKinds.TwoByTwo, BrickColors.Red), Is.True);
            return board;
        }

        [Test] public void DuplicateReleaseCannotOccupyAnotherMatchingTarget()
        {
            var board = Board(); var resolver = new SnapResolver();
            var a = SnapResolver.Center(board.Slots[0]);
            var b = SnapResolver.Center(board.Slots[1]);
            Assert.That(resolver.TrySnap(board, "physical-1", 0, a, out var placed), Is.True);
            Assert.That(placed.id, Is.EqualTo("a"));
            Assert.That(resolver.TrySnap(board, "physical-1", 0, a, out var duplicate), Is.False);
            Assert.That(duplicate, Is.Null);
            Assert.That(resolver.TrySnap(board, "physical-1", 0, b, out duplicate), Is.False);
            Assert.That(duplicate, Is.Null);
            Assert.That(board.TryPlace("physical-1", "b", 0), Is.False);
            Assert.That(board.IsOccupied("b"), Is.False);
            Assert.That(board.PlacedCount, Is.EqualTo(1));
            Assert.That(board.TryGetSlotForPiece("physical-1", out var slot), Is.True);
            Assert.That(slot, Is.EqualTo("a"));
            Assert.That(board.TryGetPieceForSlot("a", out var piece), Is.True);
            Assert.That(piece, Is.EqualTo("physical-1"));
        }

        [Test] public void SecondPieceCannotTakeOccupiedSlotButCanStillPlaceElsewhere()
        {
            var board = Board();
            Assert.That(board.TryPlace("physical-1", "a", 0), Is.True);
            Assert.That(board.TryPlace("physical-2", "a", 0), Is.False);
            Assert.That(board.TryGetSlotForPiece("physical-2", out _), Is.False);
            Assert.That(new SnapResolver().TrySnap(board, "physical-2", 0,
                SnapResolver.Center(board.Slots[0]), out _), Is.False);
            Assert.That(board.TryPlace("physical-2", "b", 0), Is.True);
        }

        [TestCase(null)][TestCase("")][TestCase(" ")][TestCase("unregistered")]
        public void InvalidPieceCannotAcquireOwnership(string id)
        {
            var board = Board();
            Assert.That(board.CanPlace(id, "a", 0), Is.False);
            Assert.That(board.TryPlace(id, "a", 0), Is.False);
            Assert.That(new SnapResolver().TrySnap(board, id, 0,
                SnapResolver.Center(board.Slots[0]), out var target), Is.False);
            Assert.That(target, Is.Null);
            Assert.That(board.TryGetSlotForPiece(id, out var slot), Is.False);
            Assert.That(slot, Is.Null);
            Assert.That(board.PlacedCount, Is.Zero);
        }

        [TestCase(null)][TestCase("")][TestCase(" ")][TestCase("unknown-slot")]
        public void InvalidTargetCannotAcquireOwnership(string id)
        {
            var board = Board();
            Assert.That(board.TryPlace("physical-1", id, 0), Is.False);
            Assert.That(board.TryGetPieceForSlot(id, out var owner), Is.False);
            Assert.That(owner, Is.Null);
            Assert.That(board.TryGetSlotForPiece("physical-1", out _), Is.False);
            Assert.That(board.PlacedCount, Is.Zero);
        }

        [Test] public void RegistrationRejectsInvalidOrReusedIdentity()
        {
            var board = Board();
            foreach (var id in new[] { null, "", " " })
                Assert.That(board.TryRegisterPiece(id, BrickKinds.TwoByTwo, BrickColors.Red), Is.False);
            Assert.That(board.TryRegisterPiece("bad-kind", "3x3", BrickColors.Red), Is.False);
            Assert.That(board.TryRegisterPiece("bad-color", BrickKinds.TwoByTwo, "green"), Is.False);
            Assert.That(board.TryRegisterPiece("physical-1", BrickKinds.TwoByFour, BrickColors.Blue), Is.False);
            Assert.That(board.TryGetPiece("physical-1", out var original), Is.True);
            Assert.That(original.Id, Is.EqualTo("physical-1"));
            Assert.That(original.Kind, Is.EqualTo(BrickKinds.TwoByTwo));
            Assert.That(original.Color, Is.EqualTo(BrickColors.Red));
            Assert.That(board.TryGetPiece("bad-kind", out _), Is.False);
            Assert.That(board.TryGetPiece("bad-color", out _), Is.False);
        }

        [Test] public void InvalidMatchingOrRotationLeavesOwnershipEmpty()
        {
            var board = Board();
            board.TryRegisterPiece("wrong-color", BrickKinds.TwoByTwo, BrickColors.Blue);
            board.TryRegisterPiece("wrong-kind", BrickKinds.TwoByFour, BrickColors.Red);
            Assert.That(board.TryPlace("wrong-color", "a", 0), Is.False);
            Assert.That(board.TryPlace("wrong-kind", "a", 0), Is.False);
            Assert.That(board.TryPlace("physical-1", "a", -1), Is.False);
            Assert.That(board.TryPlace("physical-1", "a", 4), Is.False);
            Assert.That(board.TryGetSlotForPiece("physical-1", out _), Is.False);
            Assert.That(board.IsOccupied("a"), Is.False);
        }

        [Test] public void ResetClearsBothDirectionsAndRetainsReusableInventory()
        {
            var board = Board();
            board.TryPlace("physical-1", "a", 0);
            board.TryPlace("physical-2", "b", 0);
            board.Reset(); board.Reset();
            Assert.That(board.IsComplete, Is.False);
            Assert.That(board.PlacedCount, Is.Zero);
            foreach (var slot in board.Slots)
            {
                Assert.That(board.IsOccupied(slot.id), Is.False);
                Assert.That(board.TryGetPieceForSlot(slot.id, out _), Is.False);
            }
            foreach (var id in new[] { "physical-1", "physical-2" })
            {
                Assert.That(board.TryGetSlotForPiece(id, out _), Is.False);
                Assert.That(board.TryGetPiece(id, out _), Is.True);
            }
            Assert.That(board.TryPlace("physical-1", "b", 0), Is.True);
            Assert.That(board.TryPlace("physical-2", "a", 0), Is.True);
        }

        [Test] public void CompletionEmitsOncePerBuildEvenWithReentrantDuplicateRelease()
        {
            var board = Board(); var resolver = new SnapResolver(); int events = 0;
            var last = SnapResolver.Center(board.Slots[1]);
            board.Completed += () =>
            {
                events++;
                Assert.That(board.IsComplete, Is.True);
                Assert.That(board.PlacedCount, Is.EqualTo(2));
                Assert.That(board.TryGetPieceForSlot("b", out var owner), Is.True);
                Assert.That(owner, Is.EqualTo("physical-2"));
                Assert.That(resolver.TrySnap(board, "physical-2", 0, last, out _), Is.False);
            };
            for (int build = 1; build <= 3; build++)
            {
                Assert.That(board.TryPlace("physical-1", "a", 0), Is.True);
                Assert.That(events, Is.EqualTo(build - 1));
                Assert.That(resolver.TrySnap(board, "physical-2", 0, last, out _), Is.True);
                Assert.That(events, Is.EqualTo(build));
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    Assert.That(board.IsComplete, Is.True);
                    Assert.That(resolver.TrySnap(board, "physical-2", 0, last, out _), Is.False);
                    Assert.That(board.TryPlace("physical-1", "a", 0), Is.False);
                }
                Assert.That(events, Is.EqualTo(build));
                board.Reset();
                Assert.That(events, Is.EqualTo(build));
            }
        }

        [Test] public void PreviewAndReturnedTargetsCannotMutateBoardOrClaimOwnership()
        {
            var board = Board(); var resolver = new SnapResolver();
            var original = SnapResolver.Center(board.Slots[0]);
            var preview = resolver.Resolve(board, "physical-1", 0, original);
            preview.id = "b"; preview.x = 7; preview.color = BrickColors.Blue;
            Assert.That(board.PlacedCount, Is.Zero);
            Assert.That(board.TryGetSlotForPiece("physical-1", out _), Is.False);
            Assert.That(resolver.TrySnap(board, "physical-1", 0, original, out var target), Is.True);
            target.id = "b"; target.color = BrickColors.Blue;
            Assert.That(board.Slots[0].id, Is.EqualTo("a"));
            Assert.That(board.Slots[0].color, Is.EqualTo(BrickColors.Red));
            Assert.That(board.TryGetSlotForPiece("physical-1", out var owned), Is.True);
            Assert.That(owned, Is.EqualTo("a"));
        }

        [Test] public void SharedConstantsPreserveSchemaOneWireValues()
        {
            // Deliberate contract assertions; literals belong here and in the JSON fixture.
            Assert.That(new[] { BrickKinds.TwoByTwo, BrickKinds.TwoByFour }, Is.EqualTo(new[] { "2x2", "2x4" }));
            Assert.That(new[] { BrickColors.Red, BrickColors.White, BrickColors.Blue, BrickColors.Yellow },
                Is.EqualTo(new[] { "red", "white", "blue", "yellow" }));
        }
    }
}
