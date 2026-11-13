using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace BrickBloom.XR
{
    // Tints the piece while a hand is within grab range (Hover) or holding it (Select),
    // so the user can tell "not in range yet" apart from "grab did not trigger".
    [DisallowMultipleComponent]
    public sealed class GrabStateColorFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private HandGrabInteractable _interactable;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _hoverColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color _selectColor = new Color(0.2f, 0.9f, 0.3f, 1f);
        // For a dedicated highlight shell: show it only while hovered or selected.
        [SerializeField] private bool _hideRendererWhenIdle;

        private MaterialPropertyBlock _block;
        private bool _subscribed;

        public void Configure(HandGrabInteractable interactable, Renderer targetRenderer)
        {
            _interactable = interactable;
            _renderer = targetRenderer;
        }

        public void ConfigureHighlightShell(Color hoverColor, Color selectColor)
        {
            _hoverColor = hoverColor;
            _selectColor = selectColor;
            _hideRendererWhenIdle = true;
        }

        private void OnEnable()
        {
            if (_interactable == null || _renderer == null)
            {
                Debug.LogError("[BrickBloom] GrabStateColorFeedback is missing its interactable or renderer reference; disabling.", this);
                enabled = false;
                return;
            }

            _block ??= new MaterialPropertyBlock();
            _interactable.WhenStateChanged += HandleStateChanged;
            _subscribed = true;
            Apply(_interactable.State);
        }

        private void OnDisable()
        {
            if (_subscribed && _interactable != null)
            {
                _interactable.WhenStateChanged -= HandleStateChanged;
            }

            _subscribed = false;
            if (_renderer != null)
            {
                _renderer.SetPropertyBlock(null);
                if (_hideRendererWhenIdle) _renderer.enabled = false;
            }
        }

        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            Apply(args.NewState);
        }

        private void Apply(InteractableState state)
        {
            switch (state)
            {
                case InteractableState.Hover:
                    SetColor(_hoverColor);
                    break;
                case InteractableState.Select:
                    SetColor(_selectColor);
                    break;
                default:
                    // Normal/Disabled: fall back to the colour of the material itself.
                    _renderer.SetPropertyBlock(null);
                    if (_hideRendererWhenIdle) _renderer.enabled = false;
                    break;
            }
        }

        private void SetColor(Color color)
        {
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
            if (_hideRendererWhenIdle) _renderer.enabled = true;
        }
    }
}
