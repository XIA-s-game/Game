using System.Collections;
using UnityEngine;

public partial class OldTreeInteraction
{
    private void ReadChoiceKeys()
    {
        if (IsChoiceKeyPressed(KeyCode.A, KeyCode.Alpha1))
        {
            Choose(answerA);
        }
        else if (IsChoiceKeyPressed(KeyCode.B, KeyCode.Alpha2))
        {
            StartBranchDialogue();
        }
        else if (IsChoiceKeyPressed(KeyCode.C, KeyCode.Alpha3))
        {
            StartAngryAttack();
        }
    }

    private void Choose(string answer)
    {
        branchFlowActive = false;
        currentAnswer = answer;
        state = DialogueState.Answered;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        resetCoroutine = StartCoroutine(CloseAnswerAndReset());
    }

    private void StartBranchDialogue()
    {
        branchFlowActive = true;
        StartDialogue(new[]
        {
            "Old Tree: Can you see the nest on my branch?",
            "Old Tree: I will lower it so you can look closer."
        }, StartNestMove);
    }

    private void StartNestDialogue()
    {
        StartDialogue(new[]
        {
            "Old Tree: This nest belongs to the reed bird.",
            "Old Tree: One egg does not belong here.",
            "Old Tree: Some birds leave eggs in smaller nests.",
            "Old Tree: When the chick hatches, the other eggs may be pushed out.",
            "Old Tree: Nature can be hard to judge.",
            "Old Tree: What would you do?"
        }, ShowRewardChoices, true);
    }

    private void StartDialogue(string[] lines, System.Action onComplete, bool autoCompleteLastLine)
    {
        StopCoroutineIfRunning(ref resetCoroutine);
        StopCoroutineIfRunning(ref finalInstructionCoroutine);

        currentLines = lines;
        currentLineIndex = 0;
        currentDialogueComplete = onComplete;
        autoCompleteOnLastLine = autoCompleteLastLine;
        currentAnswer = currentLines[0];
        state = DialogueState.Speaking;

        if (autoCompleteOnLastLine && currentLines.Length == 1)
        {
            currentDialogueComplete?.Invoke();
        }
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLines != null && currentLineIndex < currentLines.Length)
        {
            currentAnswer = currentLines[currentLineIndex];

            if (autoCompleteOnLastLine && currentLineIndex == currentLines.Length - 1)
            {
                System.Action finalLineComplete = currentDialogueComplete;
                currentLines = null;
                currentDialogueComplete = null;
                autoCompleteOnLastLine = false;
                finalLineComplete?.Invoke();
            }

            return;
        }

        System.Action dialogueComplete = currentDialogueComplete;
        currentLines = null;
        currentDialogueComplete = null;
        autoCompleteOnLastLine = false;
        dialogueComplete?.Invoke();
    }

    private void StartNestMove()
    {
        currentAnswer = null;
        state = DialogueState.MovingNest;

        if (nestMoveCoroutine != null)
        {
            StopCoroutine(nestMoveCoroutine);
        }

        nestMoveCoroutine = StartCoroutine(MoveNestBranchDown());
    }

    private IEnumerator MoveNestBranchDown()
    {
        if (nestBranch == null)
        {
            StartNestDialogue();
            nestMoveCoroutine = null;
            yield break;
        }

        Vector3 startPosition = nestBranch.position;
        Vector3 targetPosition = new Vector3(startPosition.x, nestTargetY, startPosition.z);
        float elapsed = 0f;

        while (elapsed < nestMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / nestMoveDuration);
            nestBranch.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        nestBranch.position = targetPosition;
        nestMoveCoroutine = null;
        StartNestDialogue();
    }

    private void CloseDialogueAndReset()
    {
        currentAnswer = null;
        branchFlowActive = false;
        state = DialogueState.Waiting;
        ResetTreeToInitialState();
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void ShowRewardChoices()
    {
        currentAnswer = null;
        state = DialogueState.RewardChoosing;
    }

    private void CloseSideQuestReminder()
    {
        currentAnswer = null;
        state = DialogueState.Waiting;
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void StartPeasantGirlDialogue()
    {
        StartDialogue(new[]
        {
            peasantFirstLine,
            peasantSecondLine
        }, CompletePeasantGirlTrade);
    }

    private void CompletePeasantGirlTrade()
    {
        peasantRewardGiven = true;
        collectedSaplingCount = requiredSaplingCount;
        GlobalBackpackUI.SetItemCount(saplingInventoryName, Mathf.Max(0, collectedSaplingCount - plantedSaplingCount));
        currentAnswer = null;
        state = DialogueState.Waiting;
        ShowSaplingPlantTargets();
    }

    private void ShowFinalInstructionForFiveSeconds()
    {
        state = DialogueState.FinalInstruction;

        StopCoroutineIfRunning(ref finalInstructionCoroutine);
        finalInstructionCoroutine = StartCoroutine(CloseFinalInstructionAfterDelay());
    }

    private IEnumerator CloseFinalInstructionAfterDelay()
    {
        yield return new WaitForSeconds(answerDuration);

        finalInstructionCoroutine = null;
        CloseDialogueAndReset();
    }

    public void ActivateFairyBackstorySideQuest()
    {
        if (sideQuestActivatedOnce)
        {
            return;
        }

        sideQuestActivatedOnce = true;
        sideQuestActive = true;
        collectedFenceCount = 0;
        collectedSaplingCount = 0;
        plantedSaplingCount = 0;
        GlobalBackpackUI.RemoveAll(fenceInventoryName);
        GlobalBackpackUI.RemoveAll(saplingInventoryName);
        fenceBuilt = false;
        fenceBuildTargetShown = false;
        nearbyFenceBuildTarget = false;
        peasantRewardGiven = false;
        saplingPlantTargetsShown = false;
        nearbySaplingPlantTarget = null;
        if (fenceBuildTarget != null)
        {
            fenceBuildTarget.gameObject.SetActive(false);
        }

        PrepareSaplingPlantTargets();
        CollectFenceTargets();
    }

    private void StopCoroutineIfRunning(ref Coroutine coroutine)
    {
        if (coroutine == null)
        {
            return;
        }

        StopCoroutine(coroutine);
        coroutine = null;
    }
}
