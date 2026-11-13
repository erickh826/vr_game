using System;
using BrickBloom.Presentation;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace BrickBloom.XR
{
    public enum BrickReleaseKind
    {
        Snapped,
        Rejected,
        TrackingLost,
        LowConfidence,
        Cancelled
    }

    // Meta Interaction SDK adapter for one BrickPiece. Translates SDK selection events into piece releases and
    // returns the piece to its supply pose whenever the grab ends for a reason other than the user letting go.
    [DisallowMultipleComponent]
    public sealed class BrickGrabAdapter : MonoBehaviour
    {
        [SerializeField] private BrickPiece _piece;
        [SerializeField] private Grabbable _grabbable;
        [SerializeField] private HandGrabInteractable _handGrabInteractable;
        [SerializeField] private Rigidbody _rigidbody;

        private HandGrabInteractor _activeInteractor;
        private Handedness _activeHandedness;
        private GrabTypeFlags _activeGrabType = GrabTypeFlags.None;
        private bool _hasActiveHand;
        // The interactable drops its selecting interactor before the Grabbable raises Unselect/Cancel.
        private bool _releasePending;
        // Placed pieces stop being grabbable; applied on the next Update, outside the SDK's release callbacks.
        private bool _lockPending;
        private bool _subscribed;

        // Raised after the SDK selection has ended and the piece transform is no longer written by the SDK.
        public event Action<BrickPiece, BrickReleaseKind> Released;

        public BrickPiece Piece => _piece;
        public bool IsHeld => _hasActiveHand && !_releasePending;

        public void Configure(BrickPiece piece, Grabbable grabbable, HandGrabInteractable handGrabInteractable, Rigidbody body)
        {
            _piece = piece;
            _grabbable = grabbable;
            _handGrabInteractable = handGrabInteractable;
            _rigidbody = body;
        }

        private void OnEnable()
        {
            if (_piece == null || _grabbable == null || _handGrabInteractable == null || _rigidbody == null)
            {
                Debug.LogError("[BrickBloom Piece] BrickGrabAdapter is missing a serialized reference; disabling.", this);
                enabled = false;
                return;
            }

            _handGrabInteractable.WhenSelectingInteractorViewAdded += HandleSelectingInteractorAdded;
            _handGrabInteractable.WhenSelectingInteractorViewRemoved += HandleSelectingInteractorRemoved;
            _handGrabInteractable.WhenStateChanged += HandleInteractableStateChanged;
            _grabbable.WhenPointerEventRaised += HandlePointerEventRaised;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            _handGrabInteractable.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorAdded;
            _handGrabInteractable.WhenSelectingInteractorViewRemoved -= HandleSelectingInteractorRemoved;
            _handGrabInteractable.WhenStateChanged -= HandleInteractableStateChanged;
            _grabbable.WhenPointerEventRaised -= HandlePointerEventRaised;
            if (IsHeld)
            {
                ClearActiveHand();
                Finish(BrickReleaseKind.Cancelled, "DisabledWhileHeld");
            }
        }

        private void Update()
        {
            if (!_lockPending) return;
            _lockPending = false;
            _handGrabInteractable.enabled = false;
            var grabInteractable = _handGrabInteractable.GetComponent<GrabInteractable>();
            if (grabInteractable != null) grabInteractable.enabled = false;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) CancelIfHeld("ApplicationPaused");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelIfHeld("SystemFocusLost");
        }

        // Also the entry point for reset: cancels an active grab without counting it as a user release.
        public void CancelIfHeld(string reason)
        {
            if (!IsHeld) return;
            var interactor = _activeInteractor;
            // Clear first so the Unselect raised by ForceRelease is not treated as a user release.
            ClearActiveHand();
            if (interactor != null) interactor.ForceRelease();
            Finish(BrickReleaseKind.Cancelled, reason);
        }

        private void HandleSelectingInteractorAdded(IInteractorView interactorView)
        {
            _activeInteractor = interactorView as HandGrabInteractor;
            if (_activeInteractor == null || _activeInteractor.Hand == null) return;
            _activeHandedness = _activeInteractor.Hand.Handedness;
            _activeGrabType = _activeInteractor.HandGrabTarget != null
                ? _activeInteractor.HandGrabTarget.Anchor
                : GrabTypeFlags.None;
            _hasActiveHand = true;
            _releasePending = false;
            _piece.SetHeld(true);
        }

        private void HandleSelectingInteractorRemoved(IInteractorView interactorView)
        {
            if (ReferenceEquals(_activeInteractor, interactorView)) _releasePending = true;
        }

        private void HandleInteractableStateChanged(InteractableStateChangeArgs args)
        {
            if (!Debug.isDebugBuild) return;
            if (args.NewState == InteractableState.Hover && args.PreviousState == InteractableState.Normal)
            {
                Debug.Log($"[BrickBloom Piece] Hover | piece={_piece.PieceId}");
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    EnforceKinematicAndScale();
                    if (_activeInteractor != null && _activeInteractor.HandGrabTarget != null &&
                        _activeInteractor.HandGrabTarget.Anchor != GrabTypeFlags.None)
                    {
                        _activeGrabType = _activeInteractor.HandGrabTarget.Anchor;
                    }

                    Debug.Log($"[BrickBloom Piece] Select | piece={_piece.PieceId} | hand={(_hasActiveHand ? _activeHandedness.ToString() : "Unknown")} | grabType={_activeGrabType}");
                    break;

                case PointerEventType.Unselect:
                    HandleUnselect();
                    break;

                case PointerEventType.Cancel:
                    if (_hasActiveHand)
                    {
                        ClearActiveHand();
                        Finish(BrickReleaseKind.Cancelled, "PointerCancel");
                    }

                    break;
            }
        }

        private void HandleUnselect()
        {
            EnforceKinematicAndScale();
            // A forced release clears the active hand before the SDK raises Unselect; it was already finished.
            if (!_hasActiveHand) return;

            IHand hand = _activeInteractor != null ? _activeInteractor.Hand : null;
            bool trackingLost = hand != null && (!hand.IsTrackedDataValid || !hand.IsConnected);
            bool confident = hand == null || (hand.IsHighConfidence &&
                hand.GetFingerIsHighConfidence(HandFinger.Index) && hand.GetFingerIsHighConfidence(HandFinger.Thumb));
            ClearActiveHand();

            if (trackingLost)
            {
                Finish(BrickReleaseKind.TrackingLost, "TrackingLostDuringGrab");
            }
            else if (!confident)
            {
                // The grab ended because finger poses became unreliable, not because the user let go.
                Finish(BrickReleaseKind.LowConfidence, "LowConfidenceAtRelease");
            }
            else if (_piece.Workbench != null && _piece.Workbench.TryPlaceReleased(_piece))
            {
                // The SDK selection has ended, so the snap pose just applied is the only transform writer.
                _lockPending = true;
                Finish(BrickReleaseKind.Snapped, "UserRelease");
            }
            else
            {
                Finish(BrickReleaseKind.Rejected, "UserReleaseWithoutTarget");
            }
        }

        private void Finish(BrickReleaseKind kind, string reason)
        {
            EnforceKinematicAndScale();
            Vector3 releasedAt = transform.localPosition;
            // Every ending except a committed snap puts the piece back in the supply area.
            if (kind != BrickReleaseKind.Snapped) _piece.ReturnToTray();

            Debug.Log($"[BrickBloom Piece] Release | piece={_piece.PieceId} | kind={kind} | reason={reason} | finalLocalPos=({releasedAt.x:F3},{releasedAt.y:F3},{releasedAt.z:F3})");
            Released?.Invoke(_piece, kind);
        }

        private void ClearActiveHand()
        {
            _activeInteractor = null;
            _activeGrabType = GrabTypeFlags.None;
            _hasActiveHand = false;
            _releasePending = false;
            _piece.SetHeld(false);
        }

        private void EnforceKinematicAndScale()
        {
            if (transform.localScale != Vector3.one) transform.localScale = Vector3.one;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
        }
    }
}
