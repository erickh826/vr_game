using BrickBloom.Presentation;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace BrickBloom.XR
{
    // Hold-to-confirm button: an index fingertip resting on the cap for the dwell time resets the build
    // or, for the NextLevel action, moves on to the next blueprint (shown only while the build is complete).
    // Reads fingertip poses only; a hand that is holding a piece is ignored so it cannot fire by accident.
    [DisallowMultipleComponent]
    public sealed class ResetDwellButton : MonoBehaviour
    {
        public enum ButtonAction { Reset = 0, NextLevel = 1 }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ButtonAction _action = ButtonAction.Reset;
        // NextLevel only: the visible parts, hidden until the build is complete.
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private BrickWorkbench _workbench;
        [SerializeField] private HandGrabInteractor[] _hands;
        [SerializeField] private Transform _cap;
        [SerializeField] private Renderer _capRenderer;
        // Touch zone in this transform's local space: centred on X/Z, from the base up to the given height.
        [SerializeField] private Vector3 _touchSize = new Vector3(0.06f, 0.035f, 0.06f);
        [SerializeField] private float _dwellSeconds = 0.8f;
        [SerializeField] private float _capTravel = 0.008f;
        [SerializeField] private Color _idleColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private Color _pressedColor = new Color(1f, 1f, 1f);

        private DwellTimer _timer;
        private MaterialPropertyBlock _block;
        private Vector3 _capRestPosition;
        private bool _wasTouching;

        public void Configure(BrickWorkbench workbench, HandGrabInteractor[] hands, Transform cap, Renderer capRenderer)
        {
            _workbench = workbench;
            _hands = hands;
            _cap = cap;
            _capRenderer = capRenderer;
        }

        public void ConfigureNextLevel(GameObject visualRoot, Color idleColor)
        {
            _action = ButtonAction.NextLevel;
            _visualRoot = visualRoot;
            _idleColor = idleColor;
        }

        private void OnEnable()
        {
            if (_workbench == null || _hands == null || _hands.Length == 0 || _cap == null || _capRenderer == null || _dwellSeconds <= 0f ||
                (_action == ButtonAction.NextLevel && _visualRoot == null))
            {
                Debug.LogError("[BrickBloom Reset] ResetDwellButton is missing a serialized reference; disabling.", this);
                enabled = false;
                return;
            }

            _timer ??= new DwellTimer(_dwellSeconds);
            _block ??= new MaterialPropertyBlock();
            _capRestPosition = _cap.localPosition;
            _timer.Clear();
            _wasTouching = false;
            Show(0f);
        }

        private void OnDisable()
        {
            if (_timer == null) return;
            _timer.Clear();
            _cap.localPosition = _capRestPosition;
        }

        private void Update()
        {
            bool available = _workbench.Board != null &&
                (_action == ButtonAction.Reset || _workbench.Board.IsComplete);
            if (_visualRoot != null && _visualRoot.activeSelf != available) _visualRoot.SetActive(available);

            bool touching = available && AnyFingertipInside();
            if (touching && !_wasTouching) Debug.Log($"[BrickBloom Button] TouchBegin | action={_action}");
            _wasTouching = touching;

            if (_timer.Tick(touching, Time.deltaTime))
            {
                Debug.Log($"[BrickBloom Button] Fired | action={_action}");
                if (_action == ButtonAction.NextLevel) _workbench.TryAdvanceLevel("NextButton");
                else _workbench.ResetBuild("ResetButton");
            }

            Show((float)_timer.Progress);
        }

        private bool AnyFingertipInside()
        {
            foreach (var interactor in _hands)
            {
                if (interactor == null || !interactor.isActiveAndEnabled || interactor.HasSelectedInteractable) continue;
                IHand hand = interactor.Hand;
                if (hand == null || !hand.IsTrackedDataValid) continue;
                if (!hand.GetJointPose(HandJointId.HandIndexTip, out Pose tip)) continue;
                Vector3 local = transform.InverseTransformPoint(tip.position);
                if (Mathf.Abs(local.x) <= _touchSize.x / 2f && Mathf.Abs(local.z) <= _touchSize.z / 2f &&
                    local.y >= 0f && local.y <= _touchSize.y)
                {
                    return true;
                }
            }

            return false;
        }

        private void Show(float progress)
        {
            _cap.localPosition = _capRestPosition + Vector3.down * (_capTravel * progress);
            _block.SetColor(BaseColorId, Color.Lerp(_idleColor, _pressedColor, progress));
            _capRenderer.SetPropertyBlock(_block);
        }
    }
}
