// Draws chapter two prompts, dialogue, quiz UI, inventory, and the mobile look pad.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        DrawPanel(rect);
        GUI.Label(rect, currentSystemPrompt, GetStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void DrawDialogue()
    {
        string line = activeLines != null && lineIndex >= 0 && lineIndex < activeLines.Length ? activeLines[lineIndex] : string.Empty;
        Rect rect = GameUiStyle.DialogueRect(220f);
        DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 26f, rect.y + 24f, rect.width - 52f, 128f), line, GetStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));
        GUI.Label(new Rect(rect.x + 26f, rect.y + rect.height - 48f, rect.width - 52f, 28f), continuePrompt, GetStyle(ref hintStyle, 22, TextAnchor.MiddleRight, FontStyle.Normal));
    }

    private void DrawQuiz()
    {
        Rect rect = new Rect(70f, 70f, Screen.width - 140f, Screen.height - 140f);
        GUI.Box(rect, GUIContent.none);

        if (showingQuizFeedback)
        {
            GUI.Label(new Rect(rect.x + 40f, rect.y + 48f, rect.width - 80f, rect.height - 120f), quizFeedback, GetStyle(ref dialogueStyle, 30, TextAnchor.UpperLeft, FontStyle.Normal, true));
            GUI.Label(new Rect(rect.x + 40f, rect.y + rect.height - 54f, rect.width - 80f, 30f), continuePrompt, GetStyle(ref hintStyle, 22, TextAnchor.MiddleRight, FontStyle.Normal));
            return;
        }

        Question q = quizQuestions[Mathf.Clamp(currentQuestionIndex, 0, quizQuestions.Count - 1)];
        GUI.Label(new Rect(rect.x + 36f, rect.y + 24f, rect.width - 72f, 38f), "Question " + (currentQuestionIndex + 1) + " / " + quizQuestions.Count + "   " + q.virtue + "   Correct " + correctAnswerCount + "/8   Wrong " + wrongAnswerCount + "/2", GetStyle(ref titleStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 46f, rect.y + 82f, rect.width - 92f, 118f), q.text, GetStyle(ref dialogueStyle, 28, TextAnchor.UpperLeft, FontStyle.Normal, true));

        string[] labels = { "A: ", "B: ", "C: ", "D: " };
        for (int i = 0; i < q.options.Length; i++)
        {
            GUI.Label(new Rect(rect.x + 58f, rect.y + 220f + i * 52f, rect.width - 116f, 42f), labels[i] + q.options[i], GetStyle(ref optionStyle, 25, TextAnchor.MiddleLeft, FontStyle.Normal, true));
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
        DrawPanel(rect);
        GUI.Label(rect, text, GetStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
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
        DrawPanel(rect);
        GUI.Label(rect, text, GetStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void DrawInventory()
    {
        float width = inventoryOpen ? 230f : 118f;
        float height = inventoryOpen ? 150f : 48f;
        Rect rect = GameUiStyle.BackpackRect(width, height);
        DrawPanel(rect);

        GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 34f), backpackTitle, GetStyle(ref inventoryStyle, 20, TextAnchor.MiddleCenter, FontStyle.Bold));

        if (!inventoryOpen)
        {
            return;
        }

        if (inventoryItems.Count == 0)
        {
            GUI.Label(new Rect(rect.x + 12f, rect.y + 52f, rect.width - 24f, 28f), backpackEmptyText, GetStyle(ref hintStyle, 18, TextAnchor.MiddleLeft, FontStyle.Normal));
            return;
        }

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            GUI.Label(new Rect(rect.x + 16f, rect.y + 52f + i * 28f, rect.width - 32f, 26f), inventoryItems[i], GetStyle(ref hintStyle, 18, TextAnchor.MiddleLeft, FontStyle.Normal));
        }
    }

    private void DrawPanel(Rect rect)
    {
        GameUiStyle.DrawPanel(rect);
    }

    private void DrawLookPad()
    {
        if (state == FlowState.Quiz)
        {
            return;
        }

        Vector2 center = GetLookPadCenter();
        EnsureLookPadTextures();
        Color previous = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(center.x - lookPadRadius, center.y - lookPadRadius, lookPadRadius * 2f, lookPadRadius * 2f), lookPadTexture);

        Vector2 knob = center + lookPadDirection * (lookPadRadius - lookPadKnobRadius - 8f);
        GUI.DrawTexture(new Rect(knob.x - lookPadKnobRadius, knob.y - lookPadKnobRadius, lookPadKnobRadius * 2f, lookPadKnobRadius * 2f), lookPadKnobTexture);
        GUI.color = previous;
    }

    private void UpdateLookPadInput()
    {
        if (state == FlowState.Quiz)
        {
            draggingLookPad = false;
            lookPadDirection = Vector2.zero;
            AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
            return;
        }

        Vector2 center = GetLookPadCenter();
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        if (Input.GetMouseButtonDown(0) && Vector2.Distance(mouse, center) <= lookPadRadius)
        {
            draggingLookPad = true;
        }

        if (!Input.GetMouseButton(0))
        {
            draggingLookPad = false;
        }

        if (draggingLookPad)
        {
            Vector2 offset = mouse - center;
            lookPadDirection = Vector2.ClampMagnitude(offset / lookPadRadius, 1f);
        }
        else
        {
            lookPadDirection = Vector2.zero;
        }

        AquariusMax.Fae.demo.DemoCharacter.LookPadInput = new Vector2(lookPadDirection.x, -lookPadDirection.y) * lookPadSensitivity;
    }

    private Vector2 GetLookPadCenter()
    {
        return new Vector2(Screen.width - lookPadRadius - 34f, Screen.height - lookPadRadius - 34f);
    }

    private void EnsureLookPadTextures()
    {
        if (lookPadTexture == null)
        {
            lookPadTexture = CreateCircleTexture(128, new Color(1f, 1f, 1f, 0.13f), new Color(1f, 1f, 1f, 0.28f), 0.08f);
        }

        if (lookPadKnobTexture == null)
        {
            lookPadKnobTexture = CreateCircleTexture(64, new Color(1f, 1f, 1f, 0.36f), new Color(1f, 1f, 1f, 0.58f), 0.18f);
        }
    }

    private Texture2D CreateCircleTexture(int size, Color fill, Color edge, float edgeWidth)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center;
        float innerRadius = radius * (1f - edgeWidth);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance > radius)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = Mathf.InverseLerp(innerRadius, radius, distance);
                    texture.SetPixel(x, y, Color.Lerp(fill, edge, t));
                }
            }
        }

        texture.Apply();
        return texture;
    }

    private GUIStyle GetStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, FontStyle fontStyle, bool wordWrap = false)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.fontSize = fontSize;
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.wordWrap = wordWrap;
        style.normal.textColor = Color.white;
        return style;
    }

    private void ShowSystemPrompt(string text, float seconds)
    {
        currentSystemPrompt = text;
        systemPromptEndsAt = Time.time + seconds;
        GameAudioManager.PlayKnob();

        if (PromptLooksLikeFailure(text))
        {
            GameAudioManager.PlayFail();
        }
        else if (PromptLooksLikeSuccess(text))
        {
            GameAudioManager.PlaySuccess();
        }
    }

    private static bool PromptLooksLikeFailure(string text)
    {
        return ContainsPromptWord(text, "failed") ||
               ContainsPromptWord(text, "wrong");
    }

    private static bool PromptLooksLikeSuccess(string text)
    {
        return ContainsPromptWord(text, "won") ||
               ContainsPromptWord(text, "passed") ||
               ContainsPromptWord(text, "unlocked") ||
               ContainsPromptWord(text, "solved") ||
               ContainsPromptWord(text, "received");
    }

    private static bool ContainsPromptWord(string text, string word)
    {
        return !string.IsNullOrEmpty(text) &&
               text.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
