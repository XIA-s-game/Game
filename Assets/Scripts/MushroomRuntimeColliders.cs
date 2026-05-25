using System.Collections.Generic;
using UnityEngine;

public static class MushroomRuntimeColliders
{
    private const string CapName = "Cap Collider";
    private const string BodyName = "Body Blocker";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddSceneMushroomColliders()
    {
        HashSet<Transform> processedRoots = new HashSet<Transform>();

        foreach (AquariusMax.Fae.demo.BounceMushroom bounceMushroom in UnityEngine.Object.FindObjectsOfType<AquariusMax.Fae.demo.BounceMushroom>())
        {
            if (bounceMushroom == null || !IsMushroomNamedObject(bounceMushroom.gameObject))
            {
                continue;
            }

            AddOrUpdateMushroomColliders(bounceMushroom.gameObject);
            processedRoots.Add(bounceMushroom.transform);
        }

        foreach (GameObject rootObject in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            if (!IsBounceMushroomPart(rootObject) || HasBounceMushroomAncestor(rootObject.transform) || HasBounceMushroomPartAncestor(rootObject.transform) || processedRoots.Contains(rootObject.transform))
            {
                continue;
            }

            if (rootObject.GetComponent<AquariusMax.Fae.demo.BounceMushroom>() == null)
            {
                rootObject.AddComponent<AquariusMax.Fae.demo.BounceMushroom>();
            }

            AddOrUpdateMushroomColliders(rootObject);
            processedRoots.Add(rootObject.transform);
        }

        foreach (GameObject rootObject in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            if (!IsMushroomNamedObject(rootObject) || IsGeneratedCollider(rootObject.transform))
            {
                continue;
            }

            if (rootObject.GetComponent<AquariusMax.Fae.demo.BounceMushroom>() == null)
            {
                RemoveGeneratedColliderChildren(rootObject.transform);
            }
        }
    }

    private static void AddOrUpdateMushroomColliders(GameObject rootObject)
    {
        if (HasExistingGeneratedColliders(rootObject.transform))
        {
            EnableExistingGeneratedColliders(rootObject.transform);
            return;
        }

        Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        DisableOriginalColliders(rootObject);

        float capThickness = Mathf.Max(bounds.size.y * 0.28f, 0.45f);
        Vector3 capCenter = new Vector3(bounds.center.x, bounds.max.y - capThickness * 0.5f, bounds.center.z);
        Vector3 capSize = new Vector3(bounds.size.x * 0.72f, capThickness, bounds.size.z * 0.72f);
        AddOrUpdateBox(rootObject, CapName, capCenter, capSize);

        float bodyHeight = Mathf.Max(bounds.size.y * 0.75f, 1f);
        Vector3 bodyCenter = new Vector3(bounds.center.x, bounds.min.y + bodyHeight * 0.5f, bounds.center.z);
        Vector3 bodySize = new Vector3(bounds.size.x * 0.42f, bodyHeight, bounds.size.z * 0.42f);
        AddOrUpdateBox(rootObject, BodyName, bodyCenter, bodySize);
    }

    private static void DisableOriginalColliders(GameObject rootObject)
    {
        Collider[] colliders = rootObject.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            if (collider.transform.name == CapName || collider.transform.name == BodyName)
            {
                continue;
            }

            collider.enabled = false;
        }
    }

    private static bool IsMushroomNamedObject(GameObject rootObject)
    {
        return rootObject.name.IndexOf("Mushroom", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsBounceMushroomPart(GameObject rootObject)
    {
        string objectName = rootObject.name;
        bool isBounceMushroomName =
            objectName.IndexOf("Mushroom_Large_A1", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            objectName.IndexOf("Mushroom_Large_A3", System.StringComparison.OrdinalIgnoreCase) >= 0;

        return isBounceMushroomName && rootObject.GetComponentsInChildren<Renderer>().Length > 0;
    }

    private static bool HasBounceMushroomAncestor(Transform transform)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.GetComponent<AquariusMax.Fae.demo.BounceMushroom>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasBounceMushroomPartAncestor(Transform transform)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (IsBounceMushroomPart(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsGeneratedCollider(Transform transform)
    {
        return transform.name == CapName || transform.name == BodyName;
    }

    private static bool HasExistingGeneratedColliders(Transform root)
    {
        return root.Find(CapName) != null || root.Find(BodyName) != null;
    }

    private static void EnableExistingGeneratedColliders(Transform root)
    {
        EnableColliderChild(root, CapName);
        EnableColliderChild(root, BodyName);
    }

    private static void EnableColliderChild(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        if (child == null)
        {
            return;
        }

        Collider collider = child.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    private static void RemoveGeneratedColliderChildren(Transform root)
    {
        RemoveGeneratedColliderChild(root, CapName);
        RemoveGeneratedColliderChild(root, BodyName);
    }

    private static void RemoveGeneratedColliderChild(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        if (child != null)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    private static void AddOrUpdateBox(GameObject rootObject, string name, Vector3 worldCenter, Vector3 worldSize)
    {
        Transform existing = rootObject.transform.Find(name);
        GameObject colliderObject = existing != null ? existing.gameObject : new GameObject(name);

        colliderObject.transform.SetParent(rootObject.transform, false);
        colliderObject.transform.position = worldCenter;
        colliderObject.transform.rotation = rootObject.transform.rotation;
        colliderObject.transform.localScale = Vector3.one;

        SphereCollider oldSphere = colliderObject.GetComponent<SphereCollider>();
        if (oldSphere != null)
        {
            oldSphere.enabled = false;
            UnityEngine.Object.Destroy(oldSphere);
        }

        BoxCollider collider = colliderObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = colliderObject.AddComponent<BoxCollider>();
        }

        collider.size = ToLocalSize(rootObject.transform, worldSize);
        collider.center = Vector3.zero;
        collider.isTrigger = false;
        collider.enabled = true;
    }

    private static Vector3 ToLocalSize(Transform root, Vector3 worldSize)
    {
        Vector3 scale = root.lossyScale;
        return new Vector3(
            SafeDivide(worldSize.x, scale.x),
            SafeDivide(worldSize.y, scale.y),
            SafeDivide(worldSize.z, scale.z));
    }

    private static float SafeDivide(float size, float scale)
    {
        float absScale = Mathf.Abs(scale);
        return absScale > 0.0001f ? Mathf.Abs(size / absScale) : Mathf.Abs(size);
    }
}
