using UnityEngine;

public partial class ChapterTwoPuzzle
{
    public static bool TryGetResumePosition(out Vector3 position)
    {
        // If the player quits mid-maze after earning a pass, continue starts at the maze door.
        position = Vector3.zero;
        if (instance == null || !instance.IsTargetScene())
        {
            return false;
        }

        if (instance.hasPass && !instance.exitedMaze && instance.startTile != null)
        {
            position = instance.startTile.position;
            return true;
        }

        return false;
    }
}
