// Caches, restores, and finds old tree scene objects during the quest.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void KeepPlayerInsideTreeRange()
    {
        Vector3 center = interactionTarget.position;
        Vector3 playerPosition = player.position;
        Vector3 offset = playerPosition - center;
        offset.y = 0f;

        if (offset.magnitude <= interactDistance)
        {
            return;
        }

        Vector3 clampedPosition = center + offset.normalized * interactDistance;
        clampedPosition.y = playerPosition.y;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            player.position = clampedPosition;
            controller.enabled = true;
        }
        else
        {
            player.position = clampedPosition;
        }
    }

    private void CacheResetTransforms()
    {
        hasInteractionTargetOriginal = TryCacheTransform(interactionTarget, out interactionTargetOriginalPosition, out interactionTargetOriginalRotation, out interactionTargetOriginalScale);
        hasNestBranchOriginal = TryCacheTransform(nestBranch, out nestBranchOriginalPosition, out nestBranchOriginalRotation, out nestBranchOriginalScale);
        hasCylinderOriginal = TryCacheTransform(cylinderResetTarget, out cylinderOriginalPosition, out cylinderOriginalRotation, out cylinderOriginalScale);
        hasNestOriginal = TryCacheTransform(nest, out nestOriginalPosition, out nestOriginalRotation, out nestOriginalScale);
        hasFaceOriginal = TryCacheTransform(faceResetTarget, out faceOriginalPosition, out faceOriginalRotation, out faceOriginalScale);
        hasMushroomOriginal = TryCacheTransform(mushroomGift, out mushroomOriginalPosition, out mushroomOriginalRotation, out mushroomOriginalScale);
    }

    private static bool TryCacheTransform(Transform target, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        if (target == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            return false;
        }

        position = target.position;
        rotation = target.rotation;
        scale = target.localScale;
        return true;
    }

    private void ResetTreeToInitialState()
    {
        DisableMushroomGlow();
        RestoreTransform(interactionTarget, hasInteractionTargetOriginal, interactionTargetOriginalPosition, interactionTargetOriginalRotation, interactionTargetOriginalScale);
        RestoreTransform(nestBranch, hasNestBranchOriginal, nestBranchOriginalPosition, nestBranchOriginalRotation, nestBranchOriginalScale);
        RestoreTransform(cylinderResetTarget, hasCylinderOriginal, cylinderOriginalPosition, cylinderOriginalRotation, cylinderOriginalScale);
        RestoreTransform(nest, hasNestOriginal, nestOriginalPosition, nestOriginalRotation, nestOriginalScale);
        RestoreTransform(faceResetTarget, hasFaceOriginal, faceOriginalPosition, faceOriginalRotation, faceOriginalScale);
        RestoreTransform(mushroomGift, hasMushroomOriginal, mushroomOriginalPosition, mushroomOriginalRotation, mushroomOriginalScale);
        RestoreAttackCylinders();
        lookRoot.rotation = originalRotation;
    }

    private static void RestoreTransform(Transform target, bool hasOriginal, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (target == null || !hasOriginal)
        {
            return;
        }

        target.position = position;
        target.rotation = rotation;
        target.localScale = scale;
    }

    private bool IsPlayerNear()
    {
        Vector3 targetPosition = interactionTarget.position;
        Vector3 playerPosition = player.position;
        targetPosition.y = 0f;
        playerPosition.y = 0f;

        return Vector3.Distance(targetPosition, playerPosition) <= interactDistance;
    }

    private bool IsPlayerNearPeasant()
    {
        FindPeasantGirl();
        if (peasantGirl == null || player == null)
        {
            return false;
        }

        Vector3 targetPosition = peasantGirl.position;
        Vector3 playerPosition = player.position;
        targetPosition.y = 0f;
        playerPosition.y = 0f;

        return Vector3.Distance(targetPosition, playerPosition) <= peasantInteractDistance;
    }

    private Transform FindSceneTransform(string objectName)
    {
        Transform found = FindChildByName(transform, objectName);
        if (found != null)
        {
            return found;
        }

        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject != null)
        {
            return foundObject.transform;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate != null && NamesMatch(candidate.name, objectName) && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (NamesMatch(root.name, childName))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool NamesMatch(string sceneName, string searchName)
    {
        if (string.Equals(sceneName, searchName, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return NormalizeName(sceneName) == NormalizeName(searchName);
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
