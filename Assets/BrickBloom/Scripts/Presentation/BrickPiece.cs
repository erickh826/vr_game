using UnityEngine;

namespace BrickBloom.Presentation
{
    // One physical brick. Root transform is the body centre (studs excluded), parented to the board origin.
    [DisallowMultipleComponent]
    public sealed class BrickPiece : MonoBehaviour
    {
        [SerializeField] private Renderer[] _bodyRenderers;
        [SerializeField] private Rigidbody _rigidbody;

        private Vector3 _trayLocalPosition;
        private Quaternion _trayLocalRotation = Quaternion.identity;

        public string PieceId { get; private set; }
        public string Kind { get; private set; }
        public string Color { get; private set; }
        public bool IsInitialized { get; private set; }
        public Vector3 TrayLocalPosition => _trayLocalPosition;

        // Editor-time wiring of prefab references.
        public void Configure(Renderer[] bodyRenderers, Rigidbody body)
        {
            _bodyRenderers = bodyRenderers;
            _rigidbody = body;
        }

        public void Initialize(string pieceId, string kind, string color, Material material,
            Vector3 trayLocalPosition, Quaternion trayLocalRotation)
        {
            PieceId = pieceId;
            Kind = kind;
            Color = color;
            _trayLocalPosition = trayLocalPosition;
            _trayLocalRotation = trayLocalRotation;
            name = $"Piece_{pieceId}_{kind}_{color}";
            if (_bodyRenderers != null && material != null)
            {
                foreach (var bodyRenderer in _bodyRenderers)
                {
                    if (bodyRenderer != null) bodyRenderer.sharedMaterial = material;
                }
            }

            IsInitialized = true;
            ReturnToTray();
        }

        public float DistanceFromTray() => Vector3.Distance(transform.localPosition, _trayLocalPosition);

        // Caller must have released any XR selection first; this is the InTray transform writer.
        public void ReturnToTray()
        {
            transform.localPosition = _trayLocalPosition;
            transform.localRotation = _trayLocalRotation;
            transform.localScale = Vector3.one;
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true;
                _rigidbody.position = transform.position;
                _rigidbody.rotation = transform.rotation;
            }
        }
    }
}
