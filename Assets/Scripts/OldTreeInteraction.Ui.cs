using UnityEngine;

public partial class OldTreeInteraction
{
    private GUIStyle dialogueTextStyle;
    private GUIStyle continueHintStyle;
    private GUIStyle choiceHintStyle;
    private GUIStyle sideQuestTitleStyle;
    private GUIStyle sideQuestTaskStyle;
    private GUIStyle rewardTextStyle;
    private GUIStyle centeredLabelStyle;

    private void OnGUI()
    {
        if (!IsTargetScene() || player == null)
        {
            return;
        }

        if (state == DialogueState.Waiting && IsPlayerNear())
        {
            if (!sideQuestActive || (nearbyFence == null && !nearbyFenceBuildTarget && nearbySaplingPlantTarget == null && !IsPlayerNearPeasant()))
            {
                DrawCenteredLabel(prompt, 28);
            }
        }

        DrawCurrentDialogueState();

        if (sideQuestActive)
        {
            DrawSideQuestPanel();

            if (nearbyFence != null)
            {
                DrawCenteredLabel(fencePickupPrompt, 26);
            }
            else if (nearbyFenceBuildTarget)
            {
                DrawCenteredLabel(fenceBuildPrompt, 26);
            }
            else if (nearbySaplingPlantTarget != null)
            {
                DrawCenteredLabel(saplingPlantPrompt, 26);
            }
            else if (IsSideQuestInProgress() && !peasantRewardGiven && IsPlayerNearPeasant())
            {
                DrawCenteredLabel(prompt, 26);
            }
        }
    }

    private void DrawCurrentDialogueState()
    {
        switch (state)
        {
            case DialogueState.Choosing:
                DrawDialogueBox(greeting, true);
                break;
            case DialogueState.Speaking:
                DrawDialogueBox(currentAnswer, false, true);
                break;
            case DialogueState.FinalInstruction:
                DrawDialogueBox(currentAnswer, false, false);
                break;
            case DialogueState.MovingNest:
                DrawDialogueBox("The nest is moving down...", false, false);
                break;
            case DialogueState.Attacking:
            case DialogueState.Answered:
                DrawDialogueBox(currentAnswer, false);
                break;
            case DialogueState.RewardChoosing:
                DrawRewardChoiceBox();
                break;
        }
    }

    private void DrawDialogueBox(string text, bool showChoices, bool showContinueHint)
    {
        float height = showChoices ? 470f : (showContinueHint ? 260f : 230f);
        Rect rect = GameUiStyle.DialogueRect(height);
        if (showChoices)
        {
            float width = Mathf.Min(1280f, Screen.width - 96f);
            rect.x = (Screen.width - width) * 0.5f;
            rect.width = width;
        }

        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle textStyle = LabelStyle(ref dialogueTextStyle, 30, TextAnchor.UpperLeft, Color.white, true);
        float choiceTextOffsetY = showChoices ? 80f : 0f;
        GUI.Label(new Rect(rect.x + 180f, rect.y + 130f + choiceTextOffsetY, rect.width - 252f, showChoices ? 120f : rect.height - 126f), text, textStyle);

        if (showContinueHint)
        {
            GUIStyle continueStyle = LabelStyle(ref continueHintStyle, 22, TextAnchor.MiddleRight, new Color(0.9f, 0.9f, 0.9f));
            GUI.Label(new Rect(rect.x, rect.y + rect.height - 130f, rect.width - 160f, 48f), continueHint, continueStyle);
        }

        if (!showChoices)
        {
            return;
        }

        GUIStyle choiceStyle = LabelStyle(ref choiceHintStyle, 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.9f, 0.9f));
        GUI.Label(new Rect(rect.x + 180f, rect.y + 326f, rect.width - 252f, 38f), chooseHint, choiceStyle);

        GUIStyle optionStyle = LabelStyle(ref choiceHintStyle, 26, TextAnchor.MiddleLeft, Color.white, true);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 382f, rect.width - 252f, 42f), choiceA, optionStyle);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 432f, rect.width - 252f, 54f), choiceB, optionStyle);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 494f, rect.width - 252f, 42f), choiceC, optionStyle);
    }

    private void DrawSideQuestPanel()
    {
        float width = 560f;
        float height = 250f;
        Rect rect = GameUiStyle.SideQuestRect(width, height);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle titleStyle = LabelStyle(ref sideQuestTitleStyle, 18, TextAnchor.UpperLeft, Color.white, true);
        GUIStyle taskStyle = LabelStyle(ref sideQuestTaskStyle, 16, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f), true);

        GUI.Label(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 70f), sideQuestTitle, titleStyle);
        string fenceCompletionMark = collectedFenceCount >= requiredFenceCount ? " done" : string.Empty;
        string saplingCompletionMark = collectedSaplingCount >= requiredSaplingCount ? " done" : string.Empty;
        GUI.Label(new Rect(rect.x + 22f, rect.y + 96f, rect.width - 44f, 58f), "1: " + fenceTaskText + " " + collectedFenceCount + "/" + requiredFenceCount + fenceCompletionMark, taskStyle);
        GUI.Label(new Rect(rect.x + 22f, rect.y + 166f, rect.width - 44f, 58f), "2: " + saplingTaskText + " " + collectedSaplingCount + "/" + requiredSaplingCount + saplingCompletionMark, taskStyle);
    }

    private void DrawRewardChoiceBox()
    {
        float width = Mathf.Min(900f, Screen.width - 80f);
        float height = 500f;
        Rect rect = GameUiStyle.DialogueRect(height);

        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle textStyle = LabelStyle(ref rewardTextStyle, 28, TextAnchor.UpperLeft, Color.white, true);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 210f, rect.width - 252f, 88f), rewardGreeting, textStyle);

        GUIStyle optionStyle = LabelStyle(ref choiceHintStyle, 24, TextAnchor.MiddleLeft, Color.white, true);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 310f, rect.width - 252f, 42f), rewardChoiceA, optionStyle);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 366f, rect.width - 252f, 42f), rewardChoiceB, optionStyle);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 422f, rect.width - 252f, 42f), rewardChoiceC, optionStyle);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 478f, rect.width - 252f, 42f), rewardChoiceD, optionStyle);
    }

    private void DrawCenteredLabel(string text, int fontSize)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(520f, 60f);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, LabelStyle(ref centeredLabelStyle, fontSize, TextAnchor.MiddleCenter, Color.white));
    }

    private GUIStyle LabelStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, Color color, bool wordWrap = false)
    {
        style = GameUiStyle.LabelStyle(ref style, fontSize, alignment, FontStyle.Normal, wordWrap);
        if (dialogueFont != null)
        {
            style.font = dialogueFont;
        }

        style.normal.textColor = color;
        return style;
    }

}
