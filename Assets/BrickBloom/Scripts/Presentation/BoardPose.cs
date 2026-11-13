using UnityEngine;

namespace BrickBloom.Presentation
{
    // Conversions between the core board-local frame (metres, origin at the board's lower X/Z corner on its top)
    // and Unity transforms parented to the board origin at unit scale.
    public static class BoardPose
    {
        public static Vector3 ToLocalPosition(BoardPoint point) =>
            new Vector3((float)point.X, (float)point.Y, (float)point.Z);

        public static BoardPoint ToBoardPoint(Vector3 localPosition) =>
            new BoardPoint(localPosition.x, localPosition.y, localPosition.z);

        public static Quaternion ToLocalRotation(int turns) => Quaternion.Euler(0f, 90f * turns, 0f);
    }
}
