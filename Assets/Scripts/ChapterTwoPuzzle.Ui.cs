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

        Rect rect = GameUiStyle.SystemPromptRect(systemPromptSize.x, systemPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, currentSystemPrompt, GameUiStyle.LabelStyle(ref promptStyle, systemPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void DrawDialogue()
    {
        string line = activeLines != null && lineIndex >= 0 && lineIndex < activeLines.Length ? activeLines[lineIndex] : string.Empty;
        Rect rect = GameUiStyle.DialogueRect(dialoguePanelHeight);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(
            new Rect(
                rect.x + dialogueTextOffset.x,
                rect.y + dialogueTextOffset.y,
                rect.width - dialogueTextOffset.x - dialogueTextRightPadding,
                rect.height - dialogueTextOffset.y - dialogueTextBottomPadding),
            line,
            GameUiStyle.LabelStyle(ref dialogueStyle, dialogueTextFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true));

        GUI.Label(
            new Rect(
                rect.x + continuePromptOffset.x,
                rect.y + rect.height - continuePromptOffset.y,
                rect.width - continuePromptRightPadding,
                continuePromptHeight),
            continuePrompt,
            GameUiStyle.LabelStyle(ref hintStyle, continuePromptFontSize, TextAnchor.LowerRight));
    }

    private void DrawQuiz()
    {
        Rect rect = ScreenRect(quizPanelRect);
        GameUiStyle.DrawDialoguePanel(rect);

        if (showingQuizFeedback)
        {
            GUI.Label(InnerRect(rect, quizFeedbackTextRect), quizFeedback, GameUiStyle.LabelStyle(ref dialogueStyle, quizFeedbackFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true));
            GUI.Label(InnerRect(rect, quizFeedbackContinueRect), continuePrompt, GameUiStyle.LabelStyle(ref hintStyle, quizFeedbackContinueFontSize, TextAnchor.MiddleRight));
            return;
        }

        Question q = quizQuestions[Mathf.Clamp(currentQuestionIndex, 0, quizQuestions.Count - 1)];
        GUI.Label(InnerRect(rect, quizHeaderRect), "Question " + (currentQuestionIndex + 1) + " / " + quizQuestions.Count + "   " + q.virtue + "   Correct " + correctAnswerCount + "/8   Wrong " + wrongAnswerCount + "/2", GameUiStyle.LabelStyle(ref titleStyle, quizHeaderFontSize, TextAnchor.MiddleCenter, FontStyle.Bold, true));
        GUI.Label(InnerRect(rect, quizQuestionRect), q.text, GameUiStyle.LabelStyle(ref dialogueStyle, quizQuestionFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true));

        string[] labels = { "A: ", "B: ", "C: ", "D: " };
        for (int i = 0; i < q.options.Length; i++)
        {
            Rect optionRect = quizOptionStartRect;
            optionRect.y += i * quizOptionSpacing;
            GUI.Label(InnerRect(rect, optionRect), labels[i] + q.options[i], GameUiStyle.LabelStyle(ref optionStyle, quizOptionFontSize, TextAnchor.MiddleLeft, FontStyle.Normal, true));
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
        Rect rect = GameUiStyle.InteractionPromptRect(interactionPromptSize.x, interactionPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, interactionPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));
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

        Rect rect = GameUiStyle.SystemPromptRect(boardPromptSize.x, boardPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, boardPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private static Rect ScreenRect(Rect localRect)
    {
        float width = localRect.width >= 0f ? localRect.width : Screen.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : Screen.height + localRect.height;
        return new Rect(localRect.x, localRect.y, width, height);
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
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
