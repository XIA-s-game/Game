// Draws the old tree dialogue, choices, egg challenge, side quest, and reward UI.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void OnGUI()
    {
        if (!IsTargetScene())
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (state == DialogueState.Waiting && IsPlayerNear())
        {
            if (!sideQuestActive || (nearbyFence == null && !nearbyFenceBuildTarget && nearbySaplingPlantTarget == null && !IsPlayerNearPeasant()))
            {
                DrawCenteredLabel(prompt, Screen.height * 0.72f, 28);
            }
        }

        if (state == DialogueState.Choosing)
        {
            DrawDialogueBox(greeting, true);
        }
        else if (state == DialogueState.Speaking)
        {
            DrawDialogueBox(currentAnswer, false, true);
        }
        else if (state == DialogueState.FinalInstruction)
        {
            DrawDialogueBox(currentAnswer, false, false);
        }
        else if (state == DialogueState.MovingNest)
        {
            DrawDialogueBox("The nest is moving down...", false, false);
        }
        else if (state == DialogueState.Attacking)
        {
            DrawDialogueBox(currentAnswer, false);
        }
        else if (state == DialogueState.Answered)
        {
            DrawDialogueBox(currentAnswer, false);
        }
        else if (state == DialogueState.EggChallenge)
        {
            DrawEggChallengePanel();
        }
        else if (state == DialogueState.EggChallengeResult)
        {
            DrawCenteredResult(eggResultText);
        }
        else if (state == DialogueState.EggChallengeFailed)
        {
            DrawEggFailurePanel();
        }
        else if (state == DialogueState.RewardChoosing)
        {
            DrawRewardChoiceBox();
        }
        else if (state == DialogueState.MushroomGift)
        {
            DrawCenteredLabel(mushroomPickupPrompt, Screen.height * 0.72f, 28);
        }

        if (sideQuestActive)
        {
            DrawSideQuestPanel();

            if (nearbyFence != null)
            {
                DrawCenteredLabel(fencePickupPrompt, Screen.height * 0.68f, 26);
            }
            else if (nearbyFenceBuildTarget)
            {
                DrawCenteredLabel(fenceBuildPrompt, Screen.height * 0.68f, 26);
            }
            else if (nearbySaplingPlantTarget != null)
            {
                DrawCenteredLabel(saplingPlantPrompt, Screen.height * 0.68f, 26);
            }
            else if (IsSideQuestInProgress() && !peasantRewardGiven && IsPlayerNearPeasant())
            {
                DrawCenteredLabel(prompt, Screen.height * 0.68f, 26);
            }
        }
    }

    private void DrawDialogueBox(string text, bool showChoices, bool showContinueHint)
    {
        float height = showChoices ? 330f : (showContinueHint ? 190f : 160f);
        Rect rect = GameUiStyle.DialogueRect(height);

        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(textStyle);
        textStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, showContinueHint ? 92f : 70f), text, textStyle);

        if (showContinueHint)
        {
            GUIStyle continueHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleRight
            };
            ApplyDialogueFont(continueHintStyle);
            continueHintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(rect.x + 24f, rect.y + rect.height - 42f, rect.width - 48f, 24f), continueHint, continueHintStyle);
        }

        if (!showChoices)
        {
            return;
        }

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleLeft
        };
        ApplyDialogueFont(hintStyle);
        hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        GUI.Label(new Rect(rect.x + 24f, rect.y + 84f, rect.width - 48f, 24f), chooseHint, hintStyle);

        if (DrawChoiceButton(rect, 118f, 36f, choiceA))
        {
            Choose(answerA);
        }

        if (DrawChoiceButton(rect, 164f, 58f, choiceB))
        {
            StartBranchDialogue();
        }

        if (DrawChoiceButton(rect, 236f, 36f, choiceC))
        {
            StartAngryAttack();
        }
    }

    private bool DrawChoiceButton(Rect parent, float yOffset, float height, string text)
    {
        Rect rect = new Rect(parent.x + 24f, parent.y + yOffset, parent.width - 48f, height);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 24,
            wordWrap = true
        };
        ApplyDialogueFont(style);

        return GUI.Button(rect, text, style);
    }

    private void DrawEggChallengePanel()
    {
        float width = Mathf.Min(420f, Screen.width - 40f);
        Rect rect = GameUiStyle.SystemPromptRect(width, 104f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26
        };
        ApplyDialogueFont(titleStyle);
        titleStyle.normal.textColor = Color.white;

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18
        };
        ApplyDialogueFont(infoStyle);
        infoStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 34f), GetEggLevelTitle(), titleStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.y + 52f, rect.width - 32f, 28f), "Time: " + Mathf.CeilToInt(eggTimer) + "s", infoStyle);
    }

    private void DrawCenteredResult(string text)
    {
        float width = Mathf.Min(520f, Screen.width - 40f);
        Rect rect = GameUiStyle.SystemPromptRect(width, 110f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            wordWrap = true
        };
        ApplyDialogueFont(style);
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, rect.height - 40f), text, style);
    }

    private void DrawSideQuestPanel()
    {
        float width = 430f;
        float height = 150f;
        Rect rect = GameUiStyle.SideQuestRect(width, height);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(titleStyle);
        titleStyle.normal.textColor = Color.white;

        GUIStyle taskStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(taskStyle);
        taskStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        GUI.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 44f), sideQuestTitle, titleStyle);
        string fenceCompletionMark = collectedFenceCount >= requiredFenceCount ? " done" : string.Empty;
        string saplingCompletionMark = collectedSaplingCount >= requiredSaplingCount ? " done" : string.Empty;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 28f), "1: " + fenceTaskText + " " + collectedFenceCount + "/" + requiredFenceCount + fenceCompletionMark, taskStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 96f, rect.width - 28f, 28f), "2: " + saplingTaskText + " " + collectedSaplingCount + "/" + requiredSaplingCount + saplingCompletionMark, taskStyle);
    }

    private void DrawBackpackPanel()
    {
        float slotSize = 72f;
        Rect panelRect = GameUiStyle.BackpackRect(260f, 94f);
        GameUiStyle.DrawPanel(panelRect);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft
        };
        ApplyDialogueFont(labelStyle);
        labelStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 8f, 120f, 22f), "Backpack", labelStyle);

        int availableFenceCount = fenceBuilt ? 0 : collectedFenceCount;
        int availableSaplingCount = Mathf.Max(0, collectedSaplingCount - plantedSaplingCount);
        if (availableFenceCount <= 0 && availableSaplingCount <= 0)
        {
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 42f, panelRect.width - 24f, 24f), "Empty", labelStyle);
            return;
        }

        Rect slotRect = new Rect(panelRect.x + 12f, panelRect.y + 34f, slotSize, slotSize - 12f);
        if (availableFenceCount > 0)
        {
            GUI.Box(slotRect, GUIContent.none);
            GUI.Label(new Rect(slotRect.x + 8f, slotRect.y + 8f, slotRect.width - 16f, 22f), fenceInventoryName, labelStyle);
            GUI.Label(new Rect(slotRect.x + 8f, slotRect.y + 32f, slotRect.width - 16f, 22f), "x" + availableFenceCount, labelStyle);
        }

        if (availableSaplingCount > 0)
        {
            Rect saplingSlotRect = availableFenceCount > 0
                ? new Rect(slotRect.xMax + 12f, slotRect.y, slotSize, slotSize - 12f)
                : slotRect;
            GUI.Box(saplingSlotRect, GUIContent.none);
            GUI.Label(new Rect(saplingSlotRect.x + 8f, saplingSlotRect.y + 8f, saplingSlotRect.width - 16f, 22f), saplingInventoryName, labelStyle);
            GUI.Label(new Rect(saplingSlotRect.x + 8f, saplingSlotRect.y + 32f, saplingSlotRect.width - 16f, 22f), "x" + availableSaplingCount, labelStyle);
        }
    }

    private void DrawEggFailurePanel()
    {
        float width = Mathf.Min(520f, Screen.width - 40f);
        Rect rect = GameUiStyle.SystemPromptRect(width, 210f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            wordWrap = true
        };
        ApplyDialogueFont(style);
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, 54f), eggResultText, style);

        if (GUI.Button(new Rect(rect.x + 60f, rect.y + 112f, 170f, 48f), "Restart"))
        {
            RestartEggChallenge();
        }

        if (GUI.Button(new Rect(rect.x + rect.width - 230f, rect.y + 112f, 170f, 48f), "Exit"))
        {
            ExitEggChallenge();
        }
    }

    private void DrawRewardChoiceBox()
    {
        float width = Mathf.Min(900f, Screen.width - 80f);
        float height = 320f;
        Rect rect = GameUiStyle.DialogueRect(height);

        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyDialogueFont(textStyle);
        textStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 60f), rewardGreeting, textStyle);

        if (DrawRewardButton(rect, 92f, rewardChoiceA))
        {
            ChooseReward(rewardChoiceA);
        }

        if (DrawRewardButton(rect, 142f, rewardChoiceB))
        {
            ChooseReward(rewardChoiceB);
        }

        if (DrawRewardButton(rect, 192f, rewardChoiceC))
        {
            ChooseReward(rewardChoiceC);
        }

        if (DrawRewardButton(rect, 242f, rewardChoiceD))
        {
            ChooseReward(rewardChoiceD);
        }
    }

    private bool DrawRewardButton(Rect parent, float yOffset, string text)
    {
        Rect rect = new Rect(parent.x + 24f, parent.y + yOffset, parent.width - 48f, 38f);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 18,
            wordWrap = true
        };
        ApplyDialogueFont(style);

        return GUI.Button(rect, text, style);
    }

    private void DrawCenteredLabel(string text, float y, int fontSize)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize
        };
        ApplyDialogueFont(style);
        style.normal.textColor = Color.white;

        Rect rect = GameUiStyle.InteractionPromptRect(520f, 60f);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, text, style);
    }

    private void ApplyDialogueFont(GUIStyle style)
    {
        if (dialogueFont != null)
        {
            style.font = dialogueFont;
        }
    }
}
