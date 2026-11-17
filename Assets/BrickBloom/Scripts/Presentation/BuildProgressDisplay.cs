using System.Collections.Generic;
using UnityEngine;

namespace BrickBloom.Presentation
{
    // One pip per blueprint slot behind the board: grey while empty, green once placed, and a gold
    // bouncing wave when the build is complete. Read-only view of the board.
    [DisallowMultipleComponent]
    public sealed class BuildProgressDisplay : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color EmptyColor = new Color(0.22f, 0.22f, 0.25f);
        private static readonly Color PlacedColor = new Color(0.25f, 0.85f, 0.35f);
        private static readonly Color CompleteColor = new Color(1f, 0.8f, 0.15f);
        private const float PipSize = 0.016f;
        private const float PipSpacing = 0.03f;
        private const float PipHeight = 0.02f;
        private const float BehindBoard = 0.035f;
        private const float CelebrationSeconds = 4f;
        private const float BounceHeight = 0.03f;

        [SerializeField] private BrickWorkbench _workbench;
        [SerializeField] private Renderer _pipPrefab;

        private readonly List<Renderer> _pips = new List<Renderer>();
        private MaterialPropertyBlock _block;
        private Vector3 _firstPipPosition;
        private bool _built;
        private bool _complete;
        private float _celebrationStarted;
        private bool _celebrating;

        public void Configure(BrickWorkbench workbench, Renderer pipPrefab)
        {
            _workbench = workbench;
            _pipPrefab = pipPrefab;
        }

        private void OnEnable()
        {
            if (_workbench == null || _pipPrefab == null)
            {
                Debug.LogError("[BrickBloom Workbench] BuildProgressDisplay is missing its workbench or pip prefab reference; disabling.", this);
                enabled = false;
                return;
            }

            _workbench.PlacementChanged += Refresh;
        }

        private void OnDisable()
        {
            if (_workbench != null) _workbench.PlacementChanged -= Refresh;
        }

        private void Update()
        {
            // The workbench creates its board in Start; build once it exists.
            if (_workbench.Board == null) return;
            if (!_built)
            {
                Build();
                Refresh();
            }

            if (_celebrating) Animate();
        }

        private void Build()
        {
            _built = true;
            _block = new MaterialPropertyBlock();
            var board = _workbench.Board;
            int count = board.TotalCount;
            float pitch = (float)BrickDefinition.StudPitch;
            _firstPipPosition = new Vector3(
                board.Width * pitch / 2f - (count - 1) * PipSpacing / 2f,
                PipHeight,
                board.Depth * pitch + BehindBoard);
            for (int i = 0; i < count; i++)
            {
                var pip = Instantiate(_pipPrefab, _workbench.BoardOrigin);
                pip.name = "ProgressPip_" + (i + 1);
                pip.transform.localRotation = Quaternion.identity;
                _pips.Add(pip);
            }

            RestPose();
        }

        private void Refresh()
        {
            if (!_built || _workbench.Board == null) return;
            var board = _workbench.Board;
            bool complete = board.IsComplete;
            for (int i = 0; i < _pips.Count; i++)
            {
                _block.SetColor(BaseColorId, complete ? CompleteColor : i < board.PlacedCount ? PlacedColor : EmptyColor);
                _pips[i].SetPropertyBlock(_block);
            }

            if (complete && !_complete)
            {
                _celebrating = true;
                _celebrationStarted = Time.time;
                Debug.Log($"[BrickBloom Workbench] Celebration | placed={board.PlacedCount}/{board.TotalCount}");
            }
            else if (!complete && _celebrating)
            {
                _celebrating = false;
                RestPose();
            }

            _complete = complete;
        }

        private void Animate()
        {
            float elapsed = Time.time - _celebrationStarted;
            if (elapsed >= CelebrationSeconds)
            {
                _celebrating = false;
                RestPose();
                return;
            }

            for (int i = 0; i < _pips.Count; i++)
            {
                float bounce = Mathf.Abs(Mathf.Sin(elapsed * 6f - i * 0.6f));
                _pips[i].transform.localPosition = PipPosition(i) + Vector3.up * (bounce * BounceHeight);
                _pips[i].transform.localScale = Vector3.one * (PipSize * (1f + 0.4f * bounce));
            }
        }

        private void RestPose()
        {
            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].transform.localPosition = PipPosition(i);
                _pips[i].transform.localScale = Vector3.one * PipSize;
            }
        }

        private Vector3 PipPosition(int index) => _firstPipPosition + Vector3.right * (index * PipSpacing);
    }
}
