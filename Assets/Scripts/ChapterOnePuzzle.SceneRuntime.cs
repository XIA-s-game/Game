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

    private void EnsureSolidCollider(Transform target)
    {
        if (target == null || colliderReadyTargets.Contains(target))
        {
            return;
        }

        bool alreadyHadCollider = HasSolidCollider(target);
        bool addedCollider = AddMeshColliders(target);
        if (!alreadyHadCollider && !addedCollider)
        {
            addedCollider = AddRendererBoxColliders(target);
        }

        if (alreadyHadCollider || addedCollider)
        {
            colliderReadyTargets.Add(target);
        }
    }

    private static bool HasSolidCollider(Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null && !collider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private static bool AddMeshColliders(Transform target)
    {
        bool addedAny = false;
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshCollider collider = meshFilter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            addedAny = true;
        }

        return addedAny;
    }

    private static bool AddRendererBoxColliders(Transform target)
    {
        bool addedAny = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.GetComponent<Collider>() != null)
            {
                continue;
            }

            BoxCollider collider = renderer.gameObject.AddComponent<BoxCollider>();
            collider.center = renderer.transform.InverseTransformPoint(renderer.bounds.center);
            collider.size = ToLocalColliderSize(renderer.bounds.size, renderer.transform.lossyScale);
            addedAny = true;
        }

        return addedAny;
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

    private static Vector3 ToLocalColliderSize(Vector3 worldSize, Vector3 lossyScale)
    {
        return new Vector3(
            DivideAxis(worldSize.x, lossyScale.x),
            DivideAxis(worldSize.y, lossyScale.y),
            DivideAxis(worldSize.z, lossyScale.z));
    }

    private static float DivideAxis(float worldSize, float scale)
    {
        float absScale = Mathf.Abs(scale);
        return absScale > 0.0001f ? Mathf.Abs(worldSize / absScale) : Mathf.Abs(worldSize);
    }
}
