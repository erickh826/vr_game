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
        public bool IsPlaced { get; private set; }
        // Set by the grab adapter while a hand owns the transform; read-only input for previews.
        public bool IsHeld { get; private set; }
        public BrickWorkbench Workbench { get; private set; }
        public Vector3 TrayLocalPosition => _trayLocalPosition;

        // Editor-time wiring of prefab references.
        public void Configure(Renderer[] bodyRenderers, Rigidbody body)
        {
            _bodyRenderers = bodyRenderers;
            _rigidbody = body;
        }

        public void Initialize(BrickWorkbench workbench, string pieceId, string kind, string color, Material material,
            Vector3 trayLocalPosition, Quaternion trayLocalRotation)
        {
            Workbench = workbench;
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

        public void SetHeld(bool held) => IsHeld = held && !IsPlaced;

        public float DistanceFromTray() => Vector3.Distance(transform.localPosition, _trayLocalPosition);

        // Caller must have released any XR selection first; this is the InTray transform writer.
        public void ReturnToTray()
        {
            if (IsPlaced) return;
            ApplyLocalPose(_trayLocalPosition, _trayLocalRotation);
        }

        // Placed transform writer: exact board-local centre and canonical rotation from the committed slot.
        public void PlaceAt(Vector3 boardLocalPosition, Quaternion boardLocalRotation)
        {
            IsPlaced = true;
            IsHeld = false;
            ApplyLocalPose(boardLocalPosition, boardLocalRotation);
        }

        // Raised at the start of ResetToTray so the XR adapter can end any grab and make the piece grabbable again.
        public event System.Action ResetRequested;

        // Reset transaction for one piece: XR ownership is released by subscribers first, then the piece
        // leaves the board and returns to its supply pose. The caller clears the board model.
        public void ResetToTray()
        {
            ResetRequested?.Invoke();
            IsPlaced = false;
            IsHeld = false;
            ApplyLocalPose(_trayLocalPosition, _trayLocalRotation);
        }

        private void ApplyLocalPose(Vector3 localPosition, Quaternion localRotation)
        {
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
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
