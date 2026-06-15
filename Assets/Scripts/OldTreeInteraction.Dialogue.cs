using System.Collections;
using UnityEngine;

public partial class OldTreeInteraction
{
    private void ReadChoiceKeys()
    {
        // First old tree choice: simple answer, nest lesson, or angry attack.
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
        GlobalBackpackUI.SetInputBlocked(false);

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        resetCoroutine = StartCoroutine(CloseAnswerAndReset());
    }

    private void StartBranchDialogue()
    {
        // The learning branch lowers the nest before asking the moral choice.
        branchFlowActive = true;
        StartDialogue(new[]
        {
            "Old Tree: Can you see the nest on my branch?",
            "Old Tree: I will lower it so you can look closer."
        }, StartNestMove);
    }

    private void StartNestDialogue()
    {
        // Nest lesson ends with reward choices instead of a normal close.
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

    private void StartDialogue(string[] lines, System.Action onComplete, bool finishOnLastLine)
    {
        // Shared dialogue runner for the old tree branch text.
        StopCoroutineIfRunning(ref resetCoroutine);
        StopCoroutineIfRunning(ref finalInstructionCoroutine);
        GlobalBackpackUI.SetInputBlocked(false);

        currentLines = lines;
        currentLineIndex = 0;
        currentDialogueComplete = onComplete;
        finishAfterLastLine = finishOnLastLine;
        currentAnswer = currentLines[0];
        state = DialogueState.Speaking;

        if (finishAfterLastLine && currentLines.Length == 1)
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

            if (finishAfterLastLine && currentLineIndex == currentLines.Length - 1)
            {
                System.Action finalLineComplete = currentDialogueComplete;
                currentLines = null;
                currentDialogueComplete = null;
                finishAfterLastLine = false;
                finalLineComplete?.Invoke();
            }

            return;
        }

        System.Action dialogueComplete = currentDialogueComplete;
        currentLines = null;
        currentDialogueComplete = null;
        finishAfterLastLine = false;
        dialogueComplete?.Invoke();
    }

    private void StartNestMove()
    {
        // Moves the branch before showing the nest explanation.
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
        // Lowers the nest to the chosen scene height, then continues the dialogue.
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
        // Normal branch close: clear text, reset tree objects, and turn away from the player.
        currentAnswer = null;
        branchFlowActive = false;
        state = DialogueState.Waiting;
        GlobalBackpackUI.SetInputBlocked(false);
        ResetTreeToInitialState();
        StartLookCoroutine(ReturnToOriginalRotation());
    }

    private void ShowRewardChoices()
    {
        currentAnswer = null;
        state = DialogueState.RewardChoosing;
        GlobalBackpackUI.SetInputBlocked(true);
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
