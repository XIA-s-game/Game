// Draws the small chapter one prompts and dialogue boxes.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    private void OnGUI()
    {
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
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = y <= 60f ? 30 : 28
        };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, rect.height - 16f), text, style);
    }

    private void DrawDialogueBox()
    {
        Rect rect = GameUiStyle.DialogueRect(220f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 30,
            wordWrap = true
        };
        textStyle.normal.textColor = Color.white;

        string line = activeDialogueLines != null && helpDialogueIndex >= 0 && helpDialogueIndex < activeDialogueLines.Length
            ? activeDialogueLines[helpDialogueIndex]
            : string.Empty;
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 148f), line, textStyle);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 22
        };
        hintStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 24f, rect.y + rect.height - 42f, rect.width - 48f, 24f), activeDialogueContinueHint, hintStyle);
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
