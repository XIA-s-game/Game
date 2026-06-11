// Handles the stone pushing puzzle and its reset/win checks.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    private void StartPushingBlock(int index)
    {
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
        if (movingBlockIndex < 0 || movingBlockIndex >= pushBlocks.Count || pushBlocks[movingBlockIndex] == null)
        {
            movingBlockIndex = -1;
            movingWrongBlock = false;
            return;
        }

        bool arrived = MoveBlockTowardLocalTarget(pushBlocks[movingBlockIndex], GetSolvedLocalPosition(pushBlocks[movingBlockIndex], movingBlockIndex));
        if (!arrived)
        {
            return;
        }

        if (movingWrongBlock)
        {
            FailAndReset();
            return;
        }

        completedPushes[movingBlockIndex] = true;
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
        for (int i = 0; i < pushMarkers.Count; i++)
        {
            if (completedPushes != null && i < completedPushes.Length && completedPushes[i])
            {
                continue;
            }

            if (IsPlayerInPushArea(i))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsPlayerInPushArea(int index)
    {
        Vector3 playerPosition = Flatten(player.position);
        Transform marker = index >= 0 && index < pushMarkers.Count ? pushMarkers[index] : null;
        if (marker != null)
        {
            float markerDistance = GetHorizontalDistanceToObject(playerPosition, marker);
            if (markerDistance > markerReachDistance)
            {
                return false;
            }
        }
        else
        {
            Transform block = index >= 0 && index < pushBlocks.Count ? pushBlocks[index] : null;
            if (block == null)
            {
                return false;
            }

            float distance = GetHorizontalDistanceToObject(playerPosition, block);
            if (distance > playerPushDistance)
            {
                return false;
            }
        }

        return true;
    }

    private Transform GetCurrentPushMarker()
    {
        return currentIndex >= 0 && currentIndex < pushMarkers.Count ? pushMarkers[currentIndex] : null;
    }

    private void RefreshPushReferences()
    {
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
            EnsureSolidCollider(step.block);
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
        if (runtimeSolvedLocalPositions != null &&
            index >= 0 &&
            index < runtimeSolvedLocalPositions.Length)
        {
            return runtimeSolvedLocalPositions[index];
        }

        return block.localPosition;
    }

    private void BuildRuntimeSolvedLocalPositions()
    {
        runtimeSolvedLocalPositions = new Vector3[pushBlocks.Count];
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
            runtimeSolvedLocalPositions[i] = GetSolvedLocalPositionForBlock(block, i);
        }
    }

    private Vector3 GetSolvedLocalPositionForBlock(Transform block, int index)
    {
        // If I filled in a solved position in the Inspector, use it. Otherwise the block's starting spot is the answer.
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

    private void RotateResultIndicators()
    {
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

        Transform basis = Camera.main != null ? Camera.main.transform : player;
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
