using UnityEngine;

namespace BrickBloom.Presentation
{
    // Places the workbench within arm's reach in front of the user once head tracking is available.
    // Runs once; relocation while a piece is held is not allowed by the transform ownership contract.
    [DisallowMultipleComponent]
    public sealed class WorkbenchPlacer : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _workbench;
        [SerializeField] private float _forwardMeters = 0.42f;
        [SerializeField] private float _belowEyeMeters = 0.42f;
        [SerializeField] private float _minimumHeightMeters = 0.55f;
        [SerializeField] private float _settleSeconds = 0.75f;

        private float _trackedSince = -1f;

        public bool IsPlaced { get; private set; }

        public void Configure(Transform head, Transform workbench)
        {
            _head = head;
            _workbench = workbench;
        }

        private void OnEnable()
        {
            if (_head == null || _workbench == null)
            {
                Debug.LogError("[BrickBloom Workbench] WorkbenchPlacer is missing its head or workbench reference; disabling.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (IsPlaced) return;
            // Before tracking starts the head sits at the rig origin; a real head is well above the floor.
            if (_head.localPosition.y < 0.3f)
            {
                _trackedSince = -1f;
                return;
            }

            if (_trackedSince < 0f) _trackedSince = Time.unscaledTime;
            if (Time.unscaledTime - _trackedSince < _settleSeconds) return;
            Place();
        }

        public void Place()
        {
            Vector3 forward = Vector3.ProjectOnPlane(_head.forward, Vector3.up);
            forward = forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
            Vector3 position = _head.position + forward * _forwardMeters;
            position.y = Mathf.Max(_minimumHeightMeters, _head.position.y - _belowEyeMeters);
            _workbench.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            IsPlaced = true;
            Debug.Log($"[BrickBloom Workbench] Placed | position=({position.x:F3},{position.y:F3},{position.z:F3}) | head=({_head.position.x:F3},{_head.position.y:F3},{_head.position.z:F3})");
        }
    }
}
