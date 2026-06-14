using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void UseTreeAsMissingTargets()
    {
        if (lookRoot == null)
        {
            lookRoot = transform;
        }

        if (interactionTarget == null)
        {
            interactionTarget = lookRoot;
        }

        if (faceResetTarget == null)
        {
            faceResetTarget = interactionTarget;
        }
    }

    private void KeepPlayerInsideTreeRange()
    {
        if (interactionTarget == null || player == null)
        {
            return;
        }

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
        RestoreTransform(interactionTarget, hasInteractionTargetOriginal, interactionTargetOriginalPosition, interactionTargetOriginalRotation, interactionTargetOriginalScale);
        RestoreTransform(nestBranch, hasNestBranchOriginal, nestBranchOriginalPosition, nestBranchOriginalRotation, nestBranchOriginalScale);
        RestoreTransform(cylinderResetTarget, hasCylinderOriginal, cylinderOriginalPosition, cylinderOriginalRotation, cylinderOriginalScale);
        RestoreTransform(nest, hasNestOriginal, nestOriginalPosition, nestOriginalRotation, nestOriginalScale);
        RestoreTransform(faceResetTarget, hasFaceOriginal, faceOriginalPosition, faceOriginalRotation, faceOriginalScale);
        RestoreAttackCylinders();
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
        return IsPlayerWithinDistance(interactionTarget, interactDistance);
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }

    private bool IsPlayerWithinDistance(Transform target, float distance)
    {
        if (target == null || player == null)
        {
            return false;
        }

        Vector3 targetPosition = target.position;
        Vector3 playerPosition = player.position;
        targetPosition.y = 0f;
        playerPosition.y = 0f;
        return Vector3.Distance(targetPosition, playerPosition) <= distance;
    }
}
