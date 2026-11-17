using UnityEngine;

namespace BrickBloom.Presentation
{
    // English instruction sign behind the board: how to play while building, what to do next once complete.
    // Read-only view of the workbench.
    [DisallowMultipleComponent]
    public sealed class LevelSignDisplay : MonoBehaviour
    {
        [SerializeField] private BrickWorkbench _workbench;
        [SerializeField] private TextMesh _text;

        private string _shown;

        public void Configure(BrickWorkbench workbench, TextMesh text)
        {
            _workbench = workbench;
            _text = text;
        }

        public static string Compose(int levelNumber, int levelCount, int placed, int total)
        {
            if (total > 0 && placed >= total)
            {
                string next = levelCount <= 1 ? "Hold RESET to build it again"
                    : levelNumber >= levelCount ? "Hold NEXT to go back to level 1"
                    : "Hold NEXT for the next build";
                return $"Level {levelNumber} of {levelCount} complete!\n{next}";
            }

            return $"Level {levelNumber} of {levelCount}\n" +
                "Pinch a brick to pick it up\n" +
                "Drop it on its glowing outline\n" +
                "Hold RESET to start over";
        }

        private void OnEnable()
        {
            if (_workbench == null || _text == null)
            {
                Debug.LogError("[BrickBloom Workbench] LevelSignDisplay is missing its workbench or text reference; disabling.", this);
                enabled = false;
                return;
            }

            _workbench.PlacementChanged += Refresh;
            _workbench.LevelChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_workbench == null) return;
            _workbench.PlacementChanged -= Refresh;
            _workbench.LevelChanged -= Refresh;
        }

        private void Refresh()
        {
            var board = _workbench.Board;
            string message = board == null
                ? string.Empty
                : Compose(_workbench.LevelIndex + 1, _workbench.LevelCount, board.PlacedCount, board.TotalCount);
            if (message == _shown) return;
            _shown = message;
            _text.text = message;
        }
    }
}
