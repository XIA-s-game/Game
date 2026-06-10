using UnityEngine;

public partial class ChapterOnePuzzle
{
    private bool IsPlayerNearTransform(Transform target, float distance)
    {
        return player != null &&
            target != null &&
            GetHorizontalDistanceToObject(Flatten(player.position), target) <= distance;
    }

    private bool IsPlayerOnTrigger(Transform target, float fallbackDistance)
    {
        if (player == null || target == null)
        {
            return false;
        }

        if (TryGetTargetBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool insideX = position.x >= bounds.min.x - 0.15f && position.x <= bounds.max.x + 0.15f;
            bool insideZ = position.z >= bounds.min.z - 0.15f && position.z <= bounds.max.z + 0.15f;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(4f, bounds.extents.y + 4f);
            return insideX && insideZ && nearY;
        }

        return GetHorizontalDistanceToObject(Flatten(player.position), target) <= fallbackDistance;
    }

    private static bool TryGetTargetBounds(Transform target, out Bounds bounds)
    {
        bounds = new Bounds(target.position, Vector3.zero);
        return TryGetColliderBounds(target, ref bounds) || TryGetRendererBounds(target, ref bounds);
    }

    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        return TryGetTargetBounds(target, out bounds);
    }

    private static bool TryGetColliderBounds(Transform target, ref Bounds bounds)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private static bool TryGetRendererBounds(Transform target, ref Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

}
