// Handles the old tree angry branch attack and the player launch sequence.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void StartAngryAttack()
    {
        // This is the wrong-choice branch: freeze the talk and let the tree lash out.
        branchFlowActive = false;
        currentAnswer = answerC;
        currentLines = null;
        currentDialogueComplete = null;
        autoCompleteOnLastLine = false;
        state = DialogueState.Attacking;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        if (finalInstructionCoroutine != null)
        {
            StopCoroutine(finalInstructionCoroutine);
            finalInstructionCoroutine = null;
        }

        CacheAttackSceneState();
        LockPlayerControl();
        CollectAttackCylinders();

        if (cylinderAttackCoroutine != null)
        {
            StopCoroutine(cylinderAttackCoroutine);
        }

        if (playerLaunchCoroutine != null)
        {
            StopCoroutine(playerLaunchCoroutine);
        }

        cylinderAttackCoroutine = StartCoroutine(AnimateAttackCylinders());
        playerLaunchCoroutine = StartCoroutine(SweepAndLaunchPlayer());
    }

    private void CacheAttackSceneState()
    {
        if (player != null)
        {
            originalPlayerPosition = player.position;
            originalPlayerRotation = player.rotation;
            hasPlayerOriginal = true;

            CharacterController controller = player.GetComponent<CharacterController>();
            characterControllerWasEnabled = controller != null && controller.enabled;

            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            rigidbodyWasKinematic = playerRigidbody == null || playerRigidbody.isKinematic;
        }

        FindPlayerAnimator();
        if (playerAnimator != null)
        {
            originalAnimatorController = playerAnimator.runtimeAnimatorController;
            hasAnimatorOriginal = true;
        }

        FindPlayerCamera();
        if (playerCameraTransform != null)
        {
            originalCameraLocalPosition = playerCameraTransform.localPosition;
            originalCameraLocalRotation = playerCameraTransform.localRotation;
            originalCameraForward = playerCameraTransform.forward;
            hasCameraOriginal = true;
        }
    }

    private void StartLookCoroutine(IEnumerator routine)
    {
        if (lookCoroutine != null)
        {
            StopCoroutine(lookCoroutine);
        }

        lookCoroutine = StartCoroutine(routine);
    }

    private void LockPlayerControl()
    {
        disabledPlayerBehaviours.Clear();

        PlayerCharacterController playerController = player.GetComponent<PlayerCharacterController>();
        if (playerController != null)
        {
            if (playerController.enabled)
            {
                playerController.enabled = false;
                disabledPlayerBehaviours.Add(playerController);
            }
        }

        MonoBehaviour[] playerBehaviours = player.GetComponents<MonoBehaviour>();
        for (int i = 0; i < playerBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = playerBehaviours[i];
            if (behaviour != null && behaviour.enabled && !disabledPlayerBehaviours.Contains(behaviour))
            {
                behaviour.enabled = false;
                disabledPlayerBehaviours.Add(behaviour);
            }
        }
    }

    private void CollectAttackCylinders(Transform root)
    {
        if (root == null)
        {
            return;
        }

        if (IsAttackCylinder(root))
        {
            attackCylinders.Add(root);
            attackCylinderOriginalPositions.Add(root.position);
            attackCylinderOriginalRotations.Add(root.rotation);
            attackCylinderOriginalScales.Add(root.localScale);
            attackCylinderMoveAxes.Add(GetIrregularMoveAxis(root));
            attackCylinderSeeds.Add(attackCylinders.Count * 1.73f);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectAttackCylinders(root.GetChild(i));
        }
    }

    private bool IsAttackCylinder(Transform target)
    {
        string normalizedName = NormalizeName(target.name);
        if (!normalizedName.Contains("cylinder") && !normalizedName.Contains("cyliner"))
        {
            return false;
        }

        if (NamesMatch(target.name, nestBranchName) ||
            NamesMatch(target.name, cylinderResetName) ||
            NamesMatch(target.name, cylinderResetFallbackName))
        {
            return false;
        }

        return target != nestBranch && target != cylinderResetTarget;
    }

    private Vector3 GetIrregularMoveAxis(Transform target)
    {
        float seed = Mathf.Abs(target.name.GetHashCode() % 1000) * 0.01f;
        Vector3 axis = new Vector3(
            Mathf.Sin(seed * 1.7f),
            Mathf.Cos(seed * 2.3f),
            Mathf.Sin(seed * 3.1f));

        if (axis.sqrMagnitude < 0.01f)
        {
            axis = Vector3.up;
        }

        return axis.normalized;
    }

    private IEnumerator AnimateAttackCylinders()
    {
        while (state == DialogueState.Attacking)
        {
            float time = Time.time * attackMoveSpeed;
            for (int i = 0; i < attackCylinders.Count; i++)
            {
                Transform target = attackCylinders[i];
                if (target == null)
                {
                    continue;
                }

                float seed = attackCylinderSeeds[i];
                Vector3 basePosition = attackCylinderOriginalPositions[i];
                Vector3 axis = attackCylinderMoveAxes[i];
                Vector3 offset = axis * Mathf.Sin(time + seed) * attackMoveAmount;
                offset += Vector3.up * Mathf.Cos(time * 1.37f + seed) * attackMoveAmount * 0.55f;

                target.position = basePosition + offset;
                target.Rotate(
                    attackRotateSpeed * Time.deltaTime * (0.6f + Mathf.Abs(Mathf.Sin(seed))),
                    attackRotateSpeed * Time.deltaTime * (0.8f + Mathf.Abs(Mathf.Cos(seed))),
                    attackRotateSpeed * Time.deltaTime,
                    Space.Self);
            }

            yield return null;
        }

        cylinderAttackCoroutine = null;
    }

    private IEnumerator SweepAndLaunchPlayer()
    {
        yield return new WaitForSeconds(0.28f);

        Transform sweeper = GetSweepCylinder();
        if (sweeper != null)
        {
            yield return SweepCylinderThroughPlayer(sweeper);
        }

        yield return LaunchPlayerHigh();
        playerLaunchCoroutine = null;
    }

    private Transform GetSweepCylinder()
    {
        Transform best = null;
        float bestDistance = float.MaxValue;
        Vector3 playerPosition = player.position;

        for (int i = 0; i < attackCylinders.Count; i++)
        {
            Transform candidate = attackCylinders[i];
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.position - playerPosition);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private IEnumerator SweepCylinderThroughPlayer(Transform sweeper)
    {
        Vector3 playerPosition = player.position + Vector3.up * sweepHeightOffset;
        Vector3 direction = playerPosition - interactionTarget.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 startPosition = playerPosition - side * sweepStartDistance;
        Vector3 endPosition = playerPosition + side * sweepEndDistance;
        Quaternion startRotation = sweeper.rotation;
        float elapsed = 0f;

        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sweepDuration);
            sweeper.position = Vector3.Lerp(startPosition, endPosition, t);
            sweeper.rotation = startRotation * Quaternion.Euler(720f * t, 0f, 1080f * t);
            yield return null;
        }

        sweeper.position = endPosition;
    }

    private IEnumerator LaunchPlayerHigh()
    {
        // The launch is a story beat, so it is animated by hand instead of relying on physics.
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        PlayPlayerController(fallingController);

        Vector3 startPosition = player.position;
        Vector3 launchDirection = startPosition - interactionTarget.position;
        launchDirection.y = 0f;

        if (launchDirection.sqrMagnitude < 0.01f)
        {
            launchDirection = transform.forward;
        }

        launchDirection.Normalize();
        float topY = Mathf.Max(launchTopY, startPosition.y);
        Vector3 topPosition = startPosition + launchDirection * launchForwardDistance;
        topPosition.y = topY;
        float elapsed = 0f;

        while (elapsed < launchRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / launchRiseDuration);
            float arc = Mathf.Sin(t * Mathf.PI * 0.5f);
            player.position = Vector3.Lerp(startPosition, topPosition, arc);
            SetLaunchCameraView(launchDirection);
            yield return null;
        }

        player.position = topPosition;
        SetLaunchCameraView(launchDirection);

        StopAttackCylindersInPlace();

        Vector3 impactPosition = topPosition + launchDirection * launchForwardDistance;
        impactPosition.y = impactY;
        elapsed = 0f;

        while (elapsed < launchFallDuration && player.position.y > impactY)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / launchFallDuration);
            float fall = t * t;
            player.position = Vector3.Lerp(topPosition, impactPosition, fall);
            SetLaunchCameraView(launchDirection);
            yield return null;
        }

        player.position = impactPosition;
        SetImpactCameraView();
        PlayPlayerController(impactController);
        yield return new WaitForSeconds(impactHoldDuration);

        PlayPlayerController(standingUpController);
        yield return WaitForCurrentPlayerAnimation(standingUpDuration);
        yield return new WaitForSeconds(postStandingResetDelay);

        ResetAttackSequence();
    }

    private void PlayPlayerController(RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            return;
        }

        FindPlayerAnimator();

        if (playerAnimator == null)
        {
            return;
        }

        playerAnimator.enabled = true;
        playerAnimator.runtimeAnimatorController = controller;

        if (!string.IsNullOrEmpty(controllerStateName))
        {
            playerAnimator.Play(controllerStateName, 0, 0f);
        }
    }

    private IEnumerator WaitForCurrentPlayerAnimation(float fallbackDuration)
    {
        yield return null;

        if (playerAnimator == null)
        {
            yield return new WaitForSeconds(fallbackDuration);
            yield break;
        }

        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        float speed = Mathf.Abs(stateInfo.speed);
        float duration = speed > 0.01f ? stateInfo.length / speed : fallbackDuration;
        if (duration <= 0.01f)
        {
            duration = fallbackDuration;
        }

        yield return new WaitForSeconds(duration);
    }

    private void StopAttackCylindersInPlace()
    {
        if (cylinderAttackCoroutine != null)
        {
            StopCoroutine(cylinderAttackCoroutine);
            cylinderAttackCoroutine = null;
        }
    }

    private void SetCameraLocalView(Vector3 localPosition, Quaternion localRotation)
    {
        FindPlayerCamera();
        if (playerCameraTransform == null)
        {
            return;
        }

        playerCameraTransform.localPosition = localPosition;
        playerCameraTransform.localRotation = localRotation;
    }

    private void SetLaunchCameraView(Vector3 launchDirection)
    {
        FindPlayerCamera();
        if (playerCameraTransform == null || player == null)
        {
            return;
        }

        Vector3 flatDirection = hasCameraOriginal ? originalCameraForward : player.forward;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.01f)
        {
            flatDirection = launchDirection;
            flatDirection.y = 0f;
        }

        if (flatDirection.sqrMagnitude < 0.01f)
        {
            flatDirection = player.forward;
        }

        flatDirection.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, flatDirection).normalized;
        Vector3 cameraPosition = player.position
            - flatDirection * Mathf.Abs(launchCameraOffset.z)
            + side * launchCameraOffset.x
            + Vector3.up * launchCameraOffset.y;

        Vector3 lookTarget = player.position + launchCameraLookOffset;
        Vector3 lookDirection = lookTarget - cameraPosition;
        if (lookDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        playerCameraTransform.position = cameraPosition;
        playerCameraTransform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void SetImpactCameraView()
    {
        FindPlayerCamera();
        if (playerCameraTransform == null || player == null)
        {
            return;
        }

        playerCameraTransform.localPosition = impactCameraLocalPosition;
        Vector3 lookTarget = player.position + impactCameraLookOffset;
        Vector3 direction = lookTarget - playerCameraTransform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            playerCameraTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ResetAttackSequence()
    {
        StopAttackCylindersInPlace();
        ResetTreeToInitialState();
        RestorePlayerState();
        currentAnswer = null;
        state = DialogueState.Waiting;
        playerLaunchCoroutine = null;
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void RestoreAttackCylinders()
    {
        if (!hasAttackOriginals)
        {
            return;
        }

        for (int i = 0; i < attackCylinders.Count; i++)
        {
            Transform target = attackCylinders[i];
            if (target == null || i >= attackCylinderOriginalPositions.Count || i >= attackCylinderOriginalRotations.Count)
            {
                continue;
            }

            target.position = attackCylinderOriginalPositions[i];
            target.rotation = attackCylinderOriginalRotations[i];
            if (i < attackCylinderOriginalScales.Count)
            {
                target.localScale = attackCylinderOriginalScales[i];
            }
        }
    }

    private void RestorePlayerState()
    {
        if (player != null && hasPlayerOriginal)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.position = originalPlayerPosition;
            player.rotation = originalPlayerRotation;

            if (controller != null)
            {
                controller.enabled = characterControllerWasEnabled;
            }

            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
                playerRigidbody.isKinematic = rigidbodyWasKinematic;
            }
        }

        if (hasCameraOriginal)
        {
            SetCameraLocalView(originalCameraLocalPosition, originalCameraLocalRotation);
        }

        FindPlayerAnimator();
        if (playerAnimator != null && hasAnimatorOriginal)
        {
            playerAnimator.runtimeAnimatorController = originalAnimatorController;
        }

        for (int i = 0; i < disabledPlayerBehaviours.Count; i++)
        {
            MonoBehaviour behaviour = disabledPlayerBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledPlayerBehaviours.Clear();
    }
}
