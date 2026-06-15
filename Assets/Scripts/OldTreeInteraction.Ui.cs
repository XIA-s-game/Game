using UnityEngine;

public partial class OldTreeInteraction
{
    private GUIStyle dialogueTextStyle;
    private GUIStyle continueHintStyle;
    private GUIStyle choiceHintStyle;
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
            DrawCenteredLabel(prompt, 28);
        }

        DrawCurrentDialogueState();
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
        float height = showChoices ? choiceDialogueHeight : (showContinueHint ? continueDialogueHeight : normalDialogueHeight);
        Rect rect = GameUiStyle.DialogueRect(height);
        if (showChoices)
        {
            float width = Mathf.Min(choiceDialogueMaxWidth, Screen.width - choiceDialogueHorizontalPadding);
            rect.x = (Screen.width - width) * 0.5f;
            rect.width = width;
        }

        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle textStyle = LabelStyle(ref dialogueTextStyle, dialogueTextFontSize, TextAnchor.UpperLeft, Color.white, true);
        Rect textRect = dialogueTextRect;
        if (showChoices)
        {
            textRect.y += choiceTextExtraY;
            textRect.height = choiceTextHeight;
        }

        GUI.Label(InnerRect(rect, textRect), text, textStyle);

        if (showContinueHint)
        {
            GUIStyle continueStyle = LabelStyle(ref continueHintStyle, continueHintFontSize, TextAnchor.MiddleRight, new Color(0.9f, 0.9f, 0.9f));
            GUI.Label(ContinueHintRect(rect), continueHint, continueStyle);
        }

        if (!showChoices)
        {
            return;
        }

        GUIStyle choiceStyle = LabelStyle(ref choiceHintStyle, choiceHintFontSize, TextAnchor.MiddleLeft, new Color(0.9f, 0.9f, 0.9f));
        GUI.Label(InnerRect(rect, choiceHintRect), chooseHint, choiceStyle);

        GUIStyle optionStyle = LabelStyle(ref choiceHintStyle, choiceOptionFontSize, TextAnchor.MiddleLeft, Color.white, true);
        GUI.Label(InnerRect(rect, choiceARect), choiceA, optionStyle);
        GUI.Label(InnerRect(rect, choiceBRect), choiceB, optionStyle);
        GUI.Label(InnerRect(rect, choiceCRect), choiceC, optionStyle);
    }

    private void DrawRewardChoiceBox()
    {
        Rect rect = GameUiStyle.DialogueRect(rewardDialogueHeight);

        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle textStyle = LabelStyle(ref rewardTextStyle, rewardGreetingFontSize, TextAnchor.UpperLeft, Color.white, true);
        GUI.Label(InnerRect(rect, rewardGreetingRect), rewardGreeting, textStyle);

        GUIStyle optionStyle = LabelStyle(ref choiceHintStyle, rewardChoiceFontSize, TextAnchor.MiddleLeft, Color.white, true);
        GUI.Label(InnerRect(rect, rewardChoiceARect), rewardChoiceA, optionStyle);
        GUI.Label(InnerRect(rect, rewardChoiceBRect), rewardChoiceB, optionStyle);
        GUI.Label(InnerRect(rect, rewardChoiceCRect), rewardChoiceC, optionStyle);
    }

    private void DrawCenteredLabel(string text, int fontSize)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(interactionPromptSize.x, interactionPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, text, LabelStyle(ref centeredLabelStyle, interactionPromptFontSize, TextAnchor.MiddleCenter, Color.white));
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }

    private Rect ContinueHintRect(Rect parent)
    {
        return new Rect(
            parent.x + continueHintX,
            parent.y + continueHintY,
            continueHintWidth,
            continueHintHeight);
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
