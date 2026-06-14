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

        if (heroPromptVisible)
        {
            DrawPromptBox(heroInteractPrompt, Screen.height * 0.72f);
        }

        if (portalPromptVisible)
        {
            DrawPromptBox(portalInteractPrompt, Screen.height * 0.72f);
        }

        if (askHelpPromptVisible)
        {
            DrawPromptBox(askHelpPrompt, Screen.height * 0.72f);
        }

        if (!string.IsNullOrEmpty(storyPrompt) && Time.time < storyPromptEndsAt)
        {
            DrawPromptBox(storyPrompt, 36f);
        }

        bool hasResultPrompt = !string.IsNullOrEmpty(resultPrompt) && Time.time < resultPromptEndsAt;
        if ((!promptVisible || string.IsNullOrEmpty(pushPrompt)) && !hasResultPrompt)
        {
            return;
        }

        DrawPromptBox(hasResultPrompt ? resultPrompt : pushPrompt, Screen.height * 0.72f);
    }

    private void DrawPromptBox(string text, float y)
    {
        Rect rect = y <= 60f
            ? GameUiStyle.SystemPromptRect(760f, 92f)
            : GameUiStyle.InteractionPromptRect(520f, 64f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref promptBoxStyle, y <= 60f ? 30 : 28, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, rect.height - 16f), text, style);
    }

    private void DrawDialogueBox()
    {
        Rect rect = GameUiStyle.DialogueRect(260f);
        GameUiStyle.DrawDialoguePanel(rect);

        string line = activeDialogueLines != null && helpDialogueIndex >= 0 && helpDialogueIndex < activeDialogueLines.Length
            ? activeDialogueLines[helpDialogueIndex]
            : string.Empty;
        GUIStyle textStyle = GameUiStyle.LabelStyle(ref dialogueTextStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true);
        GUI.Label(new Rect(rect.x + 180f, rect.y + 130f, rect.width - 72f, rect.height - 126f), line, textStyle);

        GUIStyle hintStyle = GameUiStyle.LabelStyle(ref dialogueHintStyle, 22, TextAnchor.LowerRight);
        GUI.Label(new Rect(rect.x + 0f, rect.y + rect.height - 130f, rect.width - 160f, 48f), activeDialogueContinueHint, hintStyle);
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
}
