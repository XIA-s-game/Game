// Finds and poses the player and peasant girl around the old tree quest.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void FindPlayer()
    {
        GameObject playerObject = GameObject.Find(playerName);
        if (playerObject != null)
        {
            player = playerObject.transform;
            FindPlayerAnimator();
            FindPlayerCamera();
        }
    }

    private void FindPeasantGirl()
    {
        if (peasantGirl != null)
        {
            return;
        }

        Transform found = FindSceneTransform(peasantGirlName);
        if (found == null)
        {
            return;
        }

        peasantGirl = found;
        peasantAnimator = peasantGirl.GetComponentInChildren<Animator>();
    }

    private void SetPeasantDance()
    {
        FindPeasantGirl();
        if (peasantAnimator == null || peasantRewardGiven)
        {
            return;
        }

        if (peasantDanceController != null && peasantAnimator.runtimeAnimatorController != peasantDanceController)
        {
            peasantAnimator.runtimeAnimatorController = peasantDanceController;
        }

        if (!string.IsNullOrEmpty(peasantDanceStateName))
        {
            peasantAnimator.Play(peasantDanceStateName);
        }
    }

    private void SetPeasantStand()
    {
        FindPeasantGirl();
        if (peasantAnimator == null)
        {
            return;
        }

        if (peasantStandController != null)
        {
            peasantAnimator.runtimeAnimatorController = peasantStandController;
        }

        if (!string.IsNullOrEmpty(peasantStandStateName))
        {
            peasantAnimator.Play(peasantStandStateName);
        }
    }

    private void UpdatePeasantGirlDance()
    {
        if (!peasantRewardGiven)
        {
            SetPeasantDance();
        }
    }

    private IEnumerator TurnTowardPlayer()
    {
        Quaternion startRotation = lookRoot.rotation;
        Quaternion targetRotation = GetLookAtPlayerRotation();
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            lookRoot.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        lookRoot.rotation = targetRotation;
        lookCoroutine = null;
    }

    private IEnumerator ReturnToOriginalRotation()
    {
        Quaternion startRotation = lookRoot.rotation;
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            lookRoot.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }

        lookRoot.rotation = originalRotation;
        lookCoroutine = null;
    }

    private Quaternion GetLookAtPlayerRotation()
    {
        Vector3 direction = player.position - lookRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return originalRotation;
        }

        Quaternion yawRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return yawRotation * Quaternion.Euler(lookDownAngle, 0f, 0f);
    }

    private void FindPlayerAnimator()
    {
        if (playerAnimator != null)
        {
            return;
        }

        if (player != null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null || player == null)
        {
            return;
        }

        Animator[] animators = FindObjectsOfType<Animator>();
        float bestDistance = float.MaxValue;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator candidate = animators[i];
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.transform.position - player.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                playerAnimator = candidate;
            }
        }
    }

    private void FindPlayerCamera()
    {
        if (playerCameraTransform != null)
        {
            return;
        }

        Camera playerCamera = null;
        if (player != null)
        {
            playerCamera = player.GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            playerCameraTransform = playerCamera.transform;
        }
    }
}
