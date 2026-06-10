using UnityEngine;

public partial class OldTreeInteraction
{
    private void ReadRewardChoiceKeys()
    {
        if (IsChoiceKeyPressed(KeyCode.A, KeyCode.Alpha1))
        {
            ChooseReward(rewardChoiceA);
        }
        else if (IsChoiceKeyPressed(KeyCode.B, KeyCode.Alpha2))
        {
            ChooseReward(rewardChoiceB);
        }
        else if (IsChoiceKeyPressed(KeyCode.C, KeyCode.Alpha3))
        {
            ChooseReward(rewardChoiceC);
        }
        else if (IsChoiceKeyPressed(KeyCode.D, KeyCode.Alpha4))
        {
            ChooseReward(rewardChoiceD);
        }
    }

    private static bool IsChoiceKeyPressed(KeyCode letterKey, KeyCode numberKey)
    {
        return Input.GetKeyDown(letterKey) || Input.GetKeyDown(numberKey);
    }

    private void ChooseReward(string choice)
    {
        branchFlowActive = false;

        if (choice == rewardChoiceA)
        {
            StartDialogue(new[]
            {
                "Old Tree: You want to take the egg?",
                "Old Tree: That is not your choice to make.",
                "Old Tree: The forest has its own rules.",
                "Old Tree: Good intentions can still cause harm.",
                "Old Tree: Watch first, then act.",
                "Old Tree: You are not ready for this lesson.",
                "Old Tree: Leave the nest alone.",
                "Old Tree: Come back when you understand patience."
            }, CloseDialogueAndReset);
        }
        else if (choice == rewardChoiceB)
        {
            StartDialogue(new[]
            {
                "Old Tree: Destroy it?",
                "Old Tree: Magic is not for removing things you dislike.",
                "Old Tree: A mage must understand balance.",
                "Old Tree: Both lives belong to the forest.",
                "Old Tree: Deciding who should live is not wisdom.",
                "Old Tree: That kind of certainty is dangerous.",
                "Old Tree: I will not help you with that.",
                "Old Tree: Step away from the nest.",
                "Old Tree: Think before you judge."
            }, CloseDialogueAndReset);
        }
        else if (choice == rewardChoiceC)
        {
            StartDialogue(new[]
            {
                "Old Tree: Good. You chose restraint.",
                "Old Tree: Many young mages rush to interfere.",
                "Old Tree: The forest does not always need rescue.",
                "Old Tree: It needs understanding.",
                "Old Tree: You kept your hands still.",
                "Old Tree: That deserves a small gift."
            }, GiveMagicMushroomReward);
        }
        else
        {
            ActivateFairyBackstorySideQuest();
            StartDialogue(new[]
            {
                "Old Tree: Moving it sounds kind, but it still changes the nest.",
                "Old Tree: Help should solve the problem, not create a new one.",
                "Old Tree: If you want to help, build a safe shelter nearby.",
                "Old Tree: Use wisdom, not force.",
                "Old Tree: That is the lesson."
            }, CompleteShelterInstruction);
        }
    }

    private void CompleteShelterInstruction()
    {
        ActivateFairyBackstorySideQuest();
        CloseDialogueAndReset();
    }

    private void GiveMagicMushroomReward()
    {
        GlobalBackpackUI.AddItem(magicMushroomInventoryName);
        StartDialogue(new[]
        {
            "Old Tree: This is a magic mushroom.",
            "Old Tree: It reminds careful mages to observe before acting.",
            "Old Tree: Keep that lesson with you."
        }, CloseDialogueAndReset);
    }
}
