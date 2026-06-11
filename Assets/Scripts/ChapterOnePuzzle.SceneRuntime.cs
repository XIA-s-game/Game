// Keeps chapter one scene objects touchable and easy to detect at runtime.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (TryGetDetectionBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool insideX = position.x >= bounds.min.x - 0.15f && position.x <= bounds.max.x + 0.15f;
            bool insideZ = position.z >= bounds.min.z - 0.15f && position.z <= bounds.max.z + 0.15f;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(4f, bounds.extents.y + 4f);
            return insideX && insideZ && nearY;
        }

        return GetHorizontalDistanceToObject(Flatten(player.position), target) <= fallbackDistance;
    }

    private static bool TryGetDetectionBounds(Transform target, out Bounds bounds)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bounds = new Bounds(target.position, Vector3.zero);
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

        if (hasBounds)
        {
            return true;
        }

        return TryGetWorldBounds(target, out bounds);
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
            collider.size = DivideByLossyScale(renderer.bounds.size, renderer.transform.lossyScale);
            addedAny = true;
        }

        return addedAny;
    }

    private static float GetClosestDistance(Vector3 point, Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        float closestSqrDistance = float.PositiveInfinity;
        bool found = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            Vector3 closestPoint = collider.ClosestPoint(point);
            float sqrDistance = (point - closestPoint).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                found = true;
            }
        }

        if (found)
        {
            return Mathf.Sqrt(closestSqrDistance);
        }

        if (TryGetWorldBounds(target, out Bounds bounds))
        {
            return Vector3.Distance(point, bounds.ClosestPoint(point));
        }

        return Vector3.Distance(point, target.position);
    }

    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(target.position, Vector3.zero);
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

    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            DivideByScale(size.x, lossyScale.x),
            DivideByScale(size.y, lossyScale.y),
            DivideByScale(size.z, lossyScale.z));
    }

    private static float DivideByScale(float value, float scale)
    {
        return Mathf.Abs(scale) > 0.0001f ? value / Mathf.Abs(scale) : value;
    }
}
