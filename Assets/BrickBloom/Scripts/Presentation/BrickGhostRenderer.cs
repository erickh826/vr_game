using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrickBloom.Presentation
{
    // Shows a translucent target for every slot that can be filled right now (empty and fully supported).
    // Read-only view of the board: it never moves pieces or changes ownership.
    [DisallowMultipleComponent]
    public sealed class BrickGhostRenderer : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private const float GhostAlpha = 0.35f;
        private const float PreviewAlpha = 0.85f;
        // Slightly smaller than the body so neighbouring ghosts and placed bricks do not z-fight.
        private const float Shrink = 0.96f;

        [SerializeField] private BrickWorkbench _workbench;
        [SerializeField] private Renderer _ghostPrefab;

        private readonly Dictionary<string, Renderer> _ghosts = new Dictionary<string, Renderer>();
        private readonly Dictionary<string, Color> _colors = new Dictionary<string, Color>();
        private readonly HashSet<string> _previewed = new HashSet<string>();
        private readonly HashSet<string> _previewedLastFrame = new HashSet<string>();
        private MaterialPropertyBlock _block;
        private bool _built;

        public void Configure(BrickWorkbench workbench, Renderer ghostPrefab)
        {
            _workbench = workbench;
            _ghostPrefab = ghostPrefab;
        }

        private void OnEnable()
        {
            if (_workbench == null || _ghostPrefab == null)
            {
                Debug.LogError("[BrickBloom Workbench] BrickGhostRenderer is missing its workbench or ghost prefab reference; disabling.", this);
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
            if (_built)
            {
                UpdatePreview();
                return;
            }

            _built = true;
            _block = new MaterialPropertyBlock();
            foreach (var slot in _workbench.Board.Slots)
            {
                var ghost = Instantiate(_ghostPrefab, _workbench.BoardOrigin);
                ghost.name = "Ghost_" + slot.id;
                ghost.transform.localPosition = BoardPose.ToLocalPosition(SnapResolver.Center(slot));
                ghost.transform.localRotation = Quaternion.identity;
                ghost.transform.localScale = new Vector3(
                    BrickDefinition.Width(slot) * (float)BrickDefinition.StudPitch,
                    (float)BrickDefinition.LayerHeight,
                    BrickDefinition.Depth(slot) * (float)BrickDefinition.StudPitch) * Shrink;
                Color color = ColorFor(slot.color);
                color.a = GhostAlpha;
                _colors.Add(slot.id, color);
                _ghosts.Add(slot.id, ghost);
                SetAlpha(slot.id, GhostAlpha);
            }

            Refresh();
        }

        // Brightens the ghost a held piece would snap to if released now. Only the preview visual is written.
        private void UpdatePreview()
        {
            _previewed.Clear();
            foreach (var piece in _workbench.Pieces)
            {
                if (piece == null || !piece.IsHeld) continue;
                string slotId = _workbench.PreviewTargetSlotId(piece);
                if (slotId != null) _previewed.Add(slotId);
            }

            if (_previewed.SetEquals(_previewedLastFrame)) return;
            foreach (string slotId in _previewedLastFrame)
                if (!_previewed.Contains(slotId)) SetAlpha(slotId, GhostAlpha);
            foreach (string slotId in _previewed)
                if (!_previewedLastFrame.Contains(slotId)) SetAlpha(slotId, PreviewAlpha);
            _previewedLastFrame.Clear();
            _previewedLastFrame.UnionWith(_previewed);
        }

        private void SetAlpha(string slotId, float alpha)
        {
            Color color = _colors[slotId];
            color.a = alpha;
            _block.SetColor(BaseColorId, color);
            _ghosts[slotId].SetPropertyBlock(_block);
        }

        private void Refresh()
        {
            if (!_built || _workbench.Board == null) return;
            var board = _workbench.Board;
            var slots = board.Slots;
            var placed = slots.Where(s => board.IsOccupied(s.id)).ToArray();
            int visible = 0;
            foreach (var slot in slots)
            {
                bool show = !board.IsOccupied(slot.id) && BrickRules.Supported(slot, placed);
                _ghosts[slot.id].enabled = show;
                if (show) visible++;
            }

            Debug.Log($"[BrickBloom Workbench] Ghosts | visible={visible} | placed={board.PlacedCount}/{board.TotalCount}");
        }

        private static Color ColorFor(string color)
        {
            switch (color)
            {
                case BrickColors.Red: return new Color(0.95f, 0.2f, 0.15f);
                case BrickColors.White: return new Color(1f, 1f, 1f);
                case BrickColors.Blue: return new Color(0.2f, 0.45f, 1f);
                case BrickColors.Yellow: return new Color(1f, 0.85f, 0.15f);
                default: return Color.magenta;
            }
        }
    }
}
