using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace BrickBloom.XR
{
    [DisallowMultipleComponent]
    public sealed class GateBCubeDiagnostics : MonoBehaviour
    {
        [SerializeField] private Grabbable _grabbable;
        [SerializeField] private HandGrabInteractable _handGrabInteractable;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Vector3 _trayLocalPosition = Vector3.zero;
        [SerializeField] private Quaternion _trayLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 _trayLocalScale = Vector3.one;
        [SerializeField] private float _maxReachDistanceMeters = 0.85f;
        [SerializeField] private float _postReleaseTrackingWatchSeconds = 0.5f;

        private HandGrabInteractor _activeInteractor;
        private Handedness _activeHandedness = Handedness.Left;
        private GrabTypeFlags _activeGrabType = GrabTypeFlags.None;
        private bool _hasActiveHand;
        // Set when the interactable drops its selecting interactor. The Grabbable's Unselect/Cancel
        // pointer event arrives after that, and still needs the hand identity for cycle counting.
        private bool _releasePending;
        // The runtime usually ends the grab before it reports the hand as lost, so the hand is watched briefly after release.
        private IHand _releasedHand;
        private Handedness _releasedHandedness;
        private float _releaseWatchUntil;
        private int _leftCompletedCycles;
        private int _rightCompletedCycles;
        private int _recoveryCount;
        private HandGrabInteractor[] _diagnosticInteractors;
        private float _nextDiagnosticTime;
        private readonly List<Action> _stateUnsubscribers = new List<Action>();

        private void Start()
        {
            _diagnosticInteractors = FindObjectsByType<HandGrabInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[BrickBloom GateB] Startup | interactors={_diagnosticInteractors.Length} | cubeWorld={FormatVector(transform.position)} | interactableActive={_handGrabInteractable != null && _handGrabInteractable.isActiveAndEnabled}");
            if (!Debug.isDebugBuild) return;
            foreach (var interactor in _diagnosticInteractors)
            {
                if (interactor == null) continue;
                var captured = interactor;
                Action<InteractorStateChangeArgs> handler = args =>
                {
                    if (!captured.isActiveAndEnabled) return;
                    Debug.Log($"[BrickBloom GateB] State | hand={captured.Hand?.Handedness.ToString() ?? "null"} | {args.PreviousState}->{args.NewState} | {DescribeReach(captured)} | cube={FormatVector(transform.position)}");
                };
                captured.WhenStateChanged += handler;
                _stateUnsubscribers.Add(() => { if (captured != null) captured.WhenStateChanged -= handler; });
            }
        }

        private void OnDestroy()
        {
            foreach (var unsubscribe in _stateUnsubscribers) unsubscribe();
            _stateUnsubscribers.Clear();
        }

        // Fingertip distances are what near-grab candidate detection depends on; wrist distance alone is ambiguous.
        private string DescribeReach(HandGrabInteractor interactor)
        {
            var hand = interactor.Hand;
            float indexTip = -1f, thumbTip = -1f;
            bool pinching = false;
            if (hand != null && hand.IsTrackedDataValid)
            {
                if (hand.GetJointPose(HandJointId.HandIndexTip, out var index)) indexTip = Vector3.Distance(index.position, transform.position);
                if (hand.GetJointPose(HandJointId.HandThumbTip, out var thumb)) thumbTip = Vector3.Distance(thumb.position, transform.position);
                pinching = hand.GetIndexFingerIsPinching();
            }
            return $"indexTipDistance={indexTip:F3} | thumbTipDistance={thumbTip:F3} | pinching={pinching} | state={interactor.State} | candidate={interactor.HasCandidate} | interactable={interactor.HasInteractable}";
        }

        private void Update()
        {
            UpdatePostReleaseWatch();
            if (!Debug.isDebugBuild || _diagnosticInteractors == null || Time.unscaledTime < _nextDiagnosticTime) return;
            // Sample faster while a tracked hand is near the cube so short grab attempts are captured.
            float interval = 2f;
            foreach (var interactor in _diagnosticInteractors)
            {
                if (interactor == null || !interactor.isActiveAndEnabled || interactor.Hand == null) continue;
                if (interactor.Hand.GetRootPose(out var root) && Vector3.Distance(root.position, transform.position) < 0.45f) interval = 0.5f;
            }
            _nextDiagnosticTime = Time.unscaledTime + interval;
            foreach (var interactor in _diagnosticInteractors)
            {
                if (interactor == null) continue;
                var hand = interactor.Hand;
                var pose = Pose.identity;
                bool poseValid = hand != null && hand.GetRootPose(out pose);
                float distance = poseValid ? Vector3.Distance(pose.position, transform.position) : -1f;
                Debug.Log($"[BrickBloom GateB] Tracking | hand={hand?.Handedness.ToString() ?? "null"} | active={interactor.isActiveAndEnabled} | tracked={hand?.IsTrackedDataValid} | connected={hand?.IsConnected} | poseValid={poseValid} | wristDistance={distance:F3} | wrist={FormatVector(pose.position)} | cube={FormatVector(transform.position)} | grabbing={interactor.IsGrabbing} | {DescribeReach(interactor)}");
            }
        }

        public int LeftCompletedCycles => _leftCompletedCycles;
        public int RightCompletedCycles => _rightCompletedCycles;
        public int RecoveryCount => _recoveryCount;

        public void Configure(
            Grabbable grabbable,
            HandGrabInteractable handGrabInteractable,
            Rigidbody body,
            Vector3 trayLocalPosition,
            Quaternion trayLocalRotation,
            Vector3 trayLocalScale)
        {
            _grabbable = grabbable;
            _handGrabInteractable = handGrabInteractable;
            _rigidbody = body;
            _trayLocalPosition = trayLocalPosition;
            _trayLocalRotation = trayLocalRotation;
            _trayLocalScale = trayLocalScale;
        }

        private void Awake()
        {
            if (_grabbable == null)
            {
                _grabbable = GetComponent<Grabbable>();
            }

            if (_handGrabInteractable == null)
            {
                _handGrabInteractable = GetComponentInChildren<HandGrabInteractable>(true);
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            EnforceKinematicAndScale();
        }

        private void OnEnable()
        {
            if (_handGrabInteractable != null)
            {
                _handGrabInteractable.WhenSelectingInteractorViewAdded += HandleSelectingInteractorAdded;
                _handGrabInteractable.WhenSelectingInteractorViewRemoved += HandleSelectingInteractorRemoved;
            }

            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        private void OnDisable()
        {
            if (_handGrabInteractable != null)
            {
                _handGrabInteractable.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorAdded;
                _handGrabInteractable.WhenSelectingInteractorViewRemoved -= HandleSelectingInteractorRemoved;
            }

            if (_grabbable != null)
            {
                _grabbable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }

            if (IsHeld)
            {
                ResetToTrayPose("OnDisableWhileSelected");
                ClearActiveHand();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"[BrickBloom GateB] OnApplicationPause | paused={pauseStatus} | leftCycles={_leftCompletedCycles} | rightCycles={_rightCompletedCycles}");
            if (pauseStatus && (IsHeld || IsOutOfReach()))
            {
                ForceReleaseActiveInteractor();
                ResetToTrayPose("ApplicationPaused");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"[BrickBloom GateB] OnApplicationFocus | focused={hasFocus} | leftCycles={_leftCompletedCycles} | rightCycles={_rightCompletedCycles}");
            if (!hasFocus && IsHeld)
            {
                ForceReleaseActiveInteractor();
                ResetToTrayPose("SystemFocusLostWhileSelected");
            }
            else if (hasFocus && IsOutOfReach())
            {
                ResetToTrayPose("SystemFocusRegainedOutOfReach");
            }
        }

        private void HandleSelectingInteractorAdded(IInteractorView interactorView)
        {
            _activeInteractor = interactorView as HandGrabInteractor;
            if (_activeInteractor != null && _activeInteractor.Hand != null)
            {
                _activeHandedness = _activeInteractor.Hand.Handedness;
                _activeGrabType = _activeInteractor.HandGrabTarget != null
                    ? _activeInteractor.HandGrabTarget.Anchor
                    : GrabTypeFlags.None;
                _hasActiveHand = true;
                _releasePending = false;
            }
        }

        private void HandleSelectingInteractorRemoved(IInteractorView interactorView)
        {
            if (ReferenceEquals(_activeInteractor, interactorView))
            {
                _releasePending = true;
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    _releasedHand = null;
                    EnforceKinematicAndScale();
                    if (_activeInteractor != null && _activeInteractor.HandGrabTarget != null &&
                        _activeInteractor.HandGrabTarget.Anchor != GrabTypeFlags.None)
                    {
                        _activeGrabType = _activeInteractor.HandGrabTarget.Anchor;
                    }

                    Debug.Log(
                        $"[BrickBloom GateB] Select | hand={(_hasActiveHand ? _activeHandedness.ToString() : "Unknown")} " +
                        $"| grabType={_activeGrabType} | id={evt.Identifier} | localPos={FormatVector(transform.localPosition)}");
                    break;

                case PointerEventType.Unselect:
                    EnforceKinematicAndScale();
                    IHand releasingHand = _activeInteractor != null ? _activeInteractor.Hand : null;
                    bool trackingLost = releasingHand != null &&
                                        (!releasingHand.IsTrackedDataValid || !releasingHand.IsConnected);
                    bool handConfident = releasingHand == null || releasingHand.IsHighConfidence;
                    bool indexConfident = releasingHand == null || releasingHand.GetFingerIsHighConfidence(HandFinger.Index);
                    bool thumbConfident = releasingHand == null || releasingHand.GetFingerIsHighConfidence(HandFinger.Thumb);
                    Debug.Log(
                        $"[BrickBloom GateB] ReleaseTracking | hand={(_hasActiveHand ? _activeHandedness.ToString() : "Unknown")} " +
                        $"| tracked={releasingHand?.IsTrackedDataValid} | connected={releasingHand?.IsConnected} " +
                        $"| handConfident={handConfident} | indexConfident={indexConfident} | thumbConfident={thumbConfident}");

                    if (trackingLost)
                    {
                        ResetToTrayPose($"TrackingLostDuringGrab({_activeHandedness})");
                    }
                    else if (_hasActiveHand && !(handConfident && indexConfident && thumbConfident))
                    {
                        // The grab ended because finger poses became unreliable, not because the user let go.
                        ResetToTrayPose($"LowConfidenceAtRelease({_activeHandedness})");
                    }
                    else if (IsOutOfReach())
                    {
                        ResetToTrayPose($"ReleasedOutOfReach({_activeHandedness})");
                    }
                    else
                    {
                        if (_hasActiveHand)
                        {
                            if (_activeHandedness == Handedness.Left)
                            {
                                _leftCompletedCycles++;
                            }
                            else
                            {
                                _rightCompletedCycles++;
                            }
                        }

                        Debug.Log(
                            $"[BrickBloom GateB] Unselect | hand={(_hasActiveHand ? _activeHandedness.ToString() : "Unknown")} " +
                            $"| grabType={_activeGrabType} | leftCycles={_leftCompletedCycles} | rightCycles={_rightCompletedCycles} " +
                            $"| localPos={FormatVector(transform.localPosition)} | localScale={FormatVector(transform.localScale)}");

                        if (_hasActiveHand && releasingHand != null)
                        {
                            _releasedHand = releasingHand;
                            _releasedHandedness = _activeHandedness;
                            _releaseWatchUntil = Time.unscaledTime + _postReleaseTrackingWatchSeconds;
                        }
                    }

                    ClearActiveHand();
                    break;

                case PointerEventType.Cancel:
                    ResetToTrayPose($"PointerCancel(id={evt.Identifier})");
                    ClearActiveHand();
                    break;
            }
        }

        private void UpdatePostReleaseWatch()
        {
            if (_releasedHand == null) return;
            if (Time.unscaledTime > _releaseWatchUntil || _hasActiveHand)
            {
                _releasedHand = null;
                return;
            }

            if (!_releasedHand.IsTrackedDataValid || !_releasedHand.IsConnected)
            {
                _releasedHand = null;
                ResetToTrayPose($"TrackingLostAfterRelease({_releasedHandedness})");
            }
        }

        public void ResetToTrayPose(string reason)
        {
            _releasedHand = null;
            _recoveryCount++;
            EnforceKinematicAndScale();
            transform.localPosition = _trayLocalPosition;
            transform.localRotation = _trayLocalRotation;
            transform.localScale = _trayLocalScale;
            if (_rigidbody != null)
            {
                _rigidbody.position = transform.position;
                _rigidbody.rotation = transform.rotation;
            }

            Debug.Log(
                $"[BrickBloom GateB] RecoveryReset | reason={reason} | recoveryCount={_recoveryCount} " +
                $"| localPos={FormatVector(transform.localPosition)} | localScale={FormatVector(transform.localScale)}");
        }

        private void ForceReleaseActiveInteractor()
        {
            // A forced release is a recovery, not a completed user cycle.
            _hasActiveHand = false;
            if (_activeInteractor != null)
            {
                _activeInteractor.ForceRelease();
            }

            ClearActiveHand();
        }

        private void ClearActiveHand()
        {
            _activeInteractor = null;
            _activeGrabType = GrabTypeFlags.None;
            _hasActiveHand = false;
            _releasePending = false;
        }

        private bool IsHeld => _hasActiveHand && !_releasePending;

        private bool IsOutOfReach()
        {
            return (transform.localPosition - _trayLocalPosition).sqrMagnitude >
                   (_maxReachDistanceMeters * _maxReachDistanceMeters);
        }

        private void EnforceKinematicAndScale()
        {
            if (transform.localScale != _trayLocalScale)
            {
                transform.localScale = _trayLocalScale;
            }

            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true;
            }
        }

        private static string FormatVector(Vector3 v)
        {
            return $"({v.x:F3},{v.y:F3},{v.z:F3})";
        }
    }
}
