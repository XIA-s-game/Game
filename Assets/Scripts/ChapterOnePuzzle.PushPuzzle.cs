using UnityEngine;

public partial class ChapterOnePuzzle
{
    private void UpdatePushPuzzleInteraction()
    {
        // Lets every unfinished block show as interactable, but only the required order solves the puzzle.
        if (!HasPushPuzzleReady() || currentIndex >= requiredOrderedPushCount)
        {
            return;
        }

        int hoveredIndex = GetHoveredPushIndex();
        if (hoveredIndex < 0)
        {
            return;
        }

        promptVisible = true;
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        interactionInputConsumed = true;
        StartPushingBlock(hoveredIndex);
    }

    private void StartPushingBlock(int index)
    {
        // Wrong blocks still move once, then the puzzle resets after they reach their target.
        if (index < 0 || index >= pushBlocks.Count || pushBlocks[index] == null)
        {
            return;
        }

        SetIndicatorVisible(redIndicator, false);
        resultPrompt = null;
        movingBlockIndex = index;
        movingWrongBlock = index != currentIndex || index >= requiredOrderedPushCount;
        promptVisible = false;
    }

    private void MoveActiveBlock()
    {
        // Push motion is time-based so the block completes even after the player releases input.
        if (movingBlockIndex < 0 || movingBlockIndex >= pushBlocks.Count || pushBlocks[movingBlockIndex] == null)
        {
            movingBlockIndex = -1;
            movingWrongBlock = false;
            return;
        }

        Transform movingBlock = pushBlocks[movingBlockIndex];
        Vector3 targetLocalPosition = GetSolvedLocalPosition(movingBlock, movingBlockIndex);
        bool arrived = MoveBlockTowardLocalTarget(movingBlock, targetLocalPosition);
        if (!arrived)
        {
            return;
        }

        if (movingWrongBlock)
        {
            FailAndReset();
            return;
        }

        if (completedPushes != null && movingBlockIndex < completedPushes.Length)
        {
            completedPushes[movingBlockIndex] = true;
        }

        currentIndex++;
        movingBlockIndex = -1;
        movingWrongBlock = false;

        if (currentIndex >= requiredOrderedPushCount)
        {
            ShowResult(successPrompt);
            SetIndicatorVisible(greenIndicator, true);
            ApplyRescueResult();
        }
    }

    private int GetHoveredPushIndex()
    {
        // Chooses the closest valid block/marker pair near the player.
        int pushCount = Mathf.Min(pushMarkers.Count, pushBlocks.Count);
        if (pushCount <= 0)
        {
            return -1;
        }

        int bestIndex = -1;
        float bestDistance = float.PositiveInfinity;
        if (player == null)
        {
            return -1;
        }

        Vector3 playerPosition = Flatten(player.position);

        for (int i = 0; i < pushCount; i++)
        {
            if (completedPushes != null && i < completedPushes.Length && completedPushes[i])
            {
                continue;
            }

            if (!IsPlayerInPushArea(i))
            {
                continue;
            }

            float distance = GetPushInteractionDistance(playerPosition, i);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool IsPlayerInPushArea(int index)
    {
        if (player == null)
        {
            return false;
        }

        Vector3 playerPosition = Flatten(player.position);
        Transform marker = index >= 0 && index < pushMarkers.Count ? pushMarkers[index] : null;
        Transform block = index >= 0 && index < pushBlocks.Count ? pushBlocks[index] : null;

        bool nearMarker = false;
        if (marker != null)
        {
            float directMarkerDistance = Vector3.Distance(playerPosition, Flatten(marker.position));
            float markerDistance = GetHorizontalDistanceToObject(playerPosition, marker);
            float markerTriggerDistance = Mathf.Max(markerReachDistance, 3.2f);
            nearMarker = directMarkerDistance <= markerTriggerDistance || markerDistance <= markerTriggerDistance;
        }

        bool nearBlock = false;
        if (block != null)
        {
            float directBlockDistance = Vector3.Distance(playerPosition, Flatten(block.position));
            float distance = GetHorizontalDistanceToObject(playerPosition, block);
            float blockReachDistance = Mathf.Min(playerPushDistance, Mathf.Max(markerReachDistance + 1.6f, 4.2f));
            nearBlock = directBlockDistance <= blockReachDistance || distance <= blockReachDistance;
        }

        if (marker != null)
        {
            return nearMarker || nearBlock;
        }

        return nearBlock;
    }

    private float GetPushInteractionDistance(Vector3 playerPosition, int index)
    {
        float bestDistance = float.PositiveInfinity;

        Transform marker = index >= 0 && index < pushMarkers.Count ? pushMarkers[index] : null;
        if (marker != null)
        {
            bestDistance = Mathf.Min(bestDistance, Vector3.Distance(playerPosition, Flatten(marker.position)));
            bestDistance = Mathf.Min(bestDistance, GetHorizontalDistanceToObject(playerPosition, marker));
        }

        Transform block = index >= 0 && index < pushBlocks.Count ? pushBlocks[index] : null;
        if (block != null)
        {
            bestDistance = Mathf.Min(bestDistance, Vector3.Distance(playerPosition, Flatten(block.position)));
            bestDistance = Mathf.Min(bestDistance, GetHorizontalDistanceToObject(playerPosition, block));
        }

        return bestDistance;
    }

    private Transform GetCurrentPushMarker()
    {
        return currentIndex >= 0 && currentIndex < pushMarkers.Count ? pushMarkers[currentIndex] : null;
    }

    private void LoadPushStepReferences()
    {
        // Copies Inspector push steps into lists used by the runtime puzzle loop.
        pushBlocks.Clear();
        pushMarkers.Clear();

        if (pushSteps == null)
        {
            return;
        }

        for (int i = 0; i < pushSteps.Length; i++)
        {
            PushStep step = pushSteps[i];
            if (step == null || step.block == null)
            {
                continue;
            }

            pushBlocks.Add(step.block);
            pushMarkers.Add(step.marker);
        }
    }

    private bool MoveBlockTowardLocalTarget(Transform block, Vector3 targetLocalPosition)
    {
        block.localPosition = Vector3.MoveTowards(
            block.localPosition,
            targetLocalPosition,
            pushSpeed * Time.deltaTime);

        if (Vector3.Distance(block.localPosition, targetLocalPosition) <= solvedDistance)
        {
            block.localPosition = targetLocalPosition;
            return true;
        }

        return false;
    }

    private Vector3 GetSolvedLocalPosition(Transform block, int index)
    {
        if (solvedBlockPositions != null &&
            index >= 0 &&
            index < solvedBlockPositions.Length)
        {
            return solvedBlockPositions[index];
        }

        return block.localPosition;
    }

    private void SetSolvedBlockPositions()
    {
        // Caches initial and solved positions once the scene references are ready.
        bool hadState = solvedBlockPositions != null &&
            initialLocalPositions != null &&
            completedPushes != null;
        bool sameShape = hadState &&
            solvedBlockPositions.Length == pushBlocks.Count &&
            initialLocalPositions.Length == pushBlocks.Count &&
            completedPushes.Length == pushBlocks.Count;
        if (sameShape)
        {
            return;
        }

        bool[] previousCompletedPushes = completedPushes;
        int previousCurrentIndex = currentIndex;
        solvedBlockPositions = new Vector3[pushBlocks.Count];
        initialLocalPositions = new Vector3[pushBlocks.Count];
        completedPushes = new bool[pushBlocks.Count];

        for (int i = 0; i < pushBlocks.Count; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null)
            {
                continue;
            }

            initialLocalPositions[i] = block.localPosition;
            solvedBlockPositions[i] = GetSolvedLocalPositionForBlock(block, i);
            if (previousCompletedPushes != null && i < previousCompletedPushes.Length)
            {
                completedPushes[i] = previousCompletedPushes[i];
            }
        }

        if (hadState)
        {
            currentIndex = Mathf.Clamp(previousCurrentIndex, 0, requiredOrderedPushCount);
        }
    }

    private void ApplySavedPushState()
    {
        // Unfinished puzzle saves reload with every block back at its starting position.
        if (pushBlocks.Count == 0 || initialLocalPositions == null || completedPushes == null)
        {
            return;
        }

        for (int i = 0; i < pushBlocks.Count; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null)
            {
                continue;
            }

            completedPushes[i] = false;
            block.localPosition = initialLocalPositions[i];
        }

        currentIndex = 0;
    }

    private Vector3 GetSolvedLocalPositionForBlock(Transform block, int index)
    {
        if (pushSteps != null &&
            index >= 0 &&
            index < pushSteps.Length &&
            pushSteps[index] != null &&
            pushSteps[index].solvedLocalPosition != Vector3.zero)
        {
            return pushSteps[index].solvedLocalPosition;
        }

        return block.localPosition;
    }

    private void FailAndReset()
    {
        // Any wrong push restarts the ordered sequence from the beginning.
        for (int i = 0; i < pushBlocks.Count; i++)
        {
            if (pushBlocks[i] != null && initialLocalPositions != null && i < initialLocalPositions.Length)
            {
                pushBlocks[i].localPosition = initialLocalPositions[i];
            }
        }

        currentIndex = 0;
        if (completedPushes != null)
        {
            for (int i = 0; i < completedPushes.Length; i++)
            {
                completedPushes[i] = false;
            }
        }

        movingBlockIndex = -1;
        movingWrongBlock = false;
        promptVisible = false;
        ShowResult(failurePrompt);
        SetIndicatorVisible(redIndicator, true);
        SetIndicatorVisible(greenIndicator, false);
    }

    private void IgnorePlayerPushBlockCollisions()
    {
        // Player movement is controlled by interaction distance, not physical pushing against colliders.
        if (playerController == null)
        {
            return;
        }

        for (int blockIndex = 0; blockIndex < pushBlocks.Count; blockIndex++)
        {
            Transform block = pushBlocks[blockIndex];
            if (block == null)
            {
                continue;
            }

            Collider[] blockColliders = block.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < blockColliders.Length; i++)
            {
                Collider blockCollider = blockColliders[i];
                if (blockCollider != null && !blockCollider.isTrigger)
                {
                    Physics.IgnoreCollision(playerController, blockCollider, true);
                }
            }
        }
    }

    private void RotateResultIndicators()
    {
        // Red and green indicators only rotate while visible.
        RotateIndicator(redIndicator);
        RotateIndicator(greenIndicator);
    }

    private void RotateIndicator(Transform indicator)
    {
        if (indicator != null && indicator.gameObject.activeSelf)
        {
            indicator.Rotate(indicator.forward, resultRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private static void SetIndicatorVisible(Transform indicator, bool visible)
    {
        if (indicator != null && indicator.gameObject.activeSelf != visible)
        {
            indicator.gameObject.SetActive(visible);
        }
    }

    private Vector3 GetMoveInputDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 localInput = new Vector3(horizontal, 0f, vertical);
        if (localInput.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Transform basis = player;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (right * horizontal + forward * vertical).normalized;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
