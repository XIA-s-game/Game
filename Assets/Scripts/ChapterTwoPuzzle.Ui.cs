using UnityEngine;

public partial class ChapterTwoPuzzle
{
    private void StartDialogue(string[] lines, System.Action onFinished)
    {
        activeLines = lines;
        lineIndex = 0;
        dialogueFinished = onFinished;
        state = FlowState.Dialogue;
    }

    private void AdvanceDialogue()
    {
        lineIndex++;
        if (activeLines != null && lineIndex < activeLines.Length)
        {
            return;
        }

        System.Action finished = dialogueFinished;
        activeLines = null;
        dialogueFinished = null;
        lineIndex = 0;
        state = FlowState.Exploring;

        if (finished != null)
        {
            finished.Invoke();
        }
    }

    private void DrawSystemPrompt()
    {
        if (string.IsNullOrEmpty(currentSystemPrompt) || Time.time >= systemPromptEndsAt)
        {
            return;
        }

        Rect rect = GameUiStyle.SystemPromptRect(760f, 92f);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, currentSystemPrompt, GameUiStyle.LabelStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void DrawDialogue()
    {
        string line = activeLines != null && lineIndex >= 0 && lineIndex < activeLines.Length ? activeLines[lineIndex] : string.Empty;
        Rect rect = GameUiStyle.DialogueRect(260f);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 130f, rect.width - 72f, rect.height - 126f), line, GameUiStyle.LabelStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));
        GUI.Label(new Rect(rect.x + 0f, rect.y + rect.height - 130f, rect.width - 160f, 48f), continuePrompt, GameUiStyle.LabelStyle(ref hintStyle, 22, TextAnchor.LowerRight));
    }

    private void DrawQuiz()
    {
        Rect rect = new Rect(70f, 50f, Screen.width - 140f, Screen.height - 100f);
        GameUiStyle.DrawDialoguePanel(rect);

        if (showingQuizFeedback)
        {
            GUI.Label(new Rect(rect.x + 396f, rect.y + 558f, rect.width - 112f, rect.height - 160f), quizFeedback, GameUiStyle.LabelStyle(ref dialogueStyle, 36, TextAnchor.UpperLeft, FontStyle.Normal, true));
            GUI.Label(new Rect(rect.x - 200f, rect.y + 988f, rect.width - 112f, 54f), continuePrompt, GameUiStyle.LabelStyle(ref hintStyle, 26, TextAnchor.MiddleRight));
            return;
        }

        Question q = quizQuestions[Mathf.Clamp(currentQuestionIndex, 0, quizQuestions.Count - 1)];
        GUI.Label(new Rect(rect.x + 98f, rect.y + 428f, rect.width - 96f, 72f), "Question " + (currentQuestionIndex + 1) + " / " + quizQuestions.Count + "   " + q.virtue + "   Correct " + correctAnswerCount + "/8   Wrong " + wrongAnswerCount + "/2", GameUiStyle.LabelStyle(ref titleStyle, 28, TextAnchor.MiddleCenter, FontStyle.Bold, true));
        GUI.Label(new Rect(rect.x + 388f, rect.y + 528f, rect.width - 116f, 168f), q.text, GameUiStyle.LabelStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));

        string[] labels = { "A: ", "B: ", "C: ", "D: " };
        for (int i = 0; i < q.options.Length; i++)
        {
            GUI.Label(new Rect(rect.x + 388f, rect.y + 590f + i * 92f, rect.width - 140f, 78f), labels[i] + q.options[i], GameUiStyle.LabelStyle(ref optionStyle, 26, TextAnchor.MiddleLeft, FontStyle.Normal, true));
        }
    }

    private void DrawInteractPrompts()
    {
        if (!hasPass && IsNearStartTile())
        {
            DrawPrompt(startPrompt);
            return;
        }

        if (waitingForHoneyBottle && !hasHoneyBottle && IsNearHoney())
        {
            DrawPrompt(takeHoneyPrompt);
            return;
        }

        if (bearAskedForSilverLeaf && !hasSilverLeaf && IsNearSilverLeaf())
        {
            DrawPrompt(pickLeafPrompt);
            return;
        }

        if (honeyPourReady && !hasFullHoneyBottle && IsNearHoneyGive())
        {
            DrawPrompt(pourHoneyPrompt);
            return;
        }

        if (!lockedHouseOpened && HasAllFourKeys() && IsNearLockedHouse())
        {
            DrawPrompt(unlockPrompt);
            return;
        }

        if (lockedHouseOpened && waitingForFourthPagePickup && !fourthPagePicked && IsNearBox())
        {
            DrawPrompt(pickupPrompt);
            return;
        }

        if (thirdPagePortalUnlocked && IsNearThirdPagePortal())
        {
            DrawPrompt(travelPrompt);
            return;
        }

        if (IsNearBaker() || (!listenerDialogueShown && IsNearListener()) || IsNearBear())
        {
            DrawPrompt(talkPrompt);
            return;
        }

        if (IsNearGuard())
        {
            DrawPrompt(exitedMaze ? interactPrompt : talkPrompt);
        }
    }

    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(440f, 60f);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void DrawBoardGame()
    {
        string text;
        if (boardGamePhase == BoardGamePhase.WaitingToRoll)
        {
            text = string.Format(boardRollShortFormat, boardRound);
        }
        else if (boardGamePhase == BoardGamePhase.Rolling)
        {
            text = rollingDicePrompt;
        }
        else if (boardGamePhase == BoardGamePhase.Moving)
        {
            text = lastDiceRoll > 0 ? string.Format(boardMovingPromptFormat, lastDiceRoll) : movingPrompt;
        }
        else
        {
            text = boardWonPrompt;
        }

        Rect rect = GameUiStyle.SystemPromptRect(620f, 86f);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void ShowSystemPrompt(string text, float seconds)
    {
        currentSystemPrompt = text;
        systemPromptEndsAt = Time.time + seconds;
        GameAudioManager.PlayKnob();

        if (!string.IsNullOrEmpty(text) &&
            (text.IndexOf("failed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             text.IndexOf("wrong", System.StringComparison.OrdinalIgnoreCase) >= 0))
        {
            GameAudioManager.PlayFail();
        }
        else if (!string.IsNullOrEmpty(text) &&
                 (text.IndexOf("won", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                  text.IndexOf("passed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                  text.IndexOf("unlocked", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                  text.IndexOf("solved", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                  text.IndexOf("received", System.StringComparison.OrdinalIgnoreCase) >= 0))
        {
            GameAudioManager.PlaySuccess();
        }
    }
}
