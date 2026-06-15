using UnityEngine;

public partial class ChapterOnePuzzle
{
    private GUIStyle promptBoxStyle;
    private GUIStyle dialogueTextStyle;
    private GUIStyle dialogueHintStyle;

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (helpDialogueActive)
        {
            DrawDialogueBox();
            return;
        }

        if (portalPromptVisible)
        {
            DrawPromptBox(portalInteractPrompt, Screen.height * interactionPromptScreenY);
        }

        if (askHelpPromptVisible)
        {
            DrawPromptBox(askHelpPrompt, Screen.height * interactionPromptScreenY);
        }

        if (!string.IsNullOrEmpty(storyPrompt) && Time.time < storyPromptEndsAt)
        {
            DrawPromptBox(storyPrompt, systemPromptY);
        }

        bool hasResultPrompt = !string.IsNullOrEmpty(resultPrompt) && Time.time < resultPromptEndsAt;
        if ((!promptVisible || string.IsNullOrEmpty(pushPrompt)) && !hasResultPrompt)
        {
            return;
        }

        DrawPromptBox(hasResultPrompt ? resultPrompt : pushPrompt, Screen.height * interactionPromptScreenY);
    }

    private void DrawPromptBox(string text, float y)
    {
        Rect rect = y <= 60f
            ? GameUiStyle.SystemPromptRect(systemPromptSize.x, systemPromptSize.y)
            : GameUiStyle.InteractionPromptRect(interactionPromptSize.x, interactionPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref promptBoxStyle, y <= 60f ? systemPromptFontSize : promptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        GUI.Label(ApplyPadding(rect, promptTextPadding), text, style);
    }

    private void DrawDialogueBox()
    {
        Rect rect = GameUiStyle.DialogueRect(dialoguePanelHeight);
        GameUiStyle.DrawDialoguePanel(rect);

        string line = activeDialogueLines != null && helpDialogueIndex >= 0 && helpDialogueIndex < activeDialogueLines.Length
            ? activeDialogueLines[helpDialogueIndex]
            : string.Empty;
        GUIStyle textStyle = GameUiStyle.LabelStyle(ref dialogueTextStyle, dialogueFontSize, TextAnchor.UpperLeft, FontStyle.Normal, true);
        GUI.Label(ApplyPadding(rect, dialogueTextPadding), line, textStyle);

        GUIStyle hintStyle = GameUiStyle.LabelStyle(ref dialogueHintStyle, dialogueHintFontSize, TextAnchor.LowerRight);
        Rect hintRect = ApplyPadding(rect, dialogueHintPadding);
        GUI.Label(new Rect(hintRect.x, hintRect.yMax - 48f, hintRect.width, 48f), activeDialogueContinueHint, hintStyle);
    }

    private void ShowResult(string text)
    {
        resultPrompt = text;
        resultPromptEndsAt = Time.time + 3f;
        if (string.Equals(text, failurePrompt, System.StringComparison.OrdinalIgnoreCase))
        {
            GameAudioManager.PlayFail();
        }
        else if (string.Equals(text, successPrompt, System.StringComparison.OrdinalIgnoreCase))
        {
            GameAudioManager.PlaySuccess();
        }
    }

    private static Rect ApplyPadding(Rect rect, UiPadding padding)
    {
        return padding.Apply(rect);
    }
}
