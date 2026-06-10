using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterTwoPuzzle
{
    public static void AddItemToInventory(string itemName)
    {
        if (instance != null)
        {
            instance.AddInventoryItem(itemName);
        }
    }

    private void UpdateExplorationInput()
    {
        bool interactPressed = Input.GetKeyDown(interactKey);

        if (!hasPass && IsNearStartTile() && interactPressed)
        {
            StartBoardGame();
            return;
        }

        if (waitingForHoneyBottle && !hasHoneyBottle && IsNearHoney() && interactPressed)
        {
            PickHoneyBottle();
            return;
        }

        if (bearAskedForSilverLeaf && !hasSilverLeaf && IsNearSilverLeaf() && interactPressed)
        {
            PickSilverLeaf();
            return;
        }

        if (honeyPourReady && !hasFullHoneyBottle && IsNearHoneyGive() && interactPressed)
        {
            PourHoney();
            return;
        }

        if (!lockedHouseOpened && HasAllFourKeys() && IsNearLockedHouse() && interactPressed)
        {
            StartCoroutine(OpenLockedHouse());
            return;
        }

        if (lockedHouseOpened && waitingForFourthPagePickup && !fourthPagePicked && IsNearBox() && interactPressed)
        {
            PickFourthPage();
            return;
        }

        if (thirdPagePortalUnlocked && IsNearThirdPagePortal() && interactPressed)
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (lockedHouseOpened && !boxDialogueShown && !fourthPagePicked && IsNearBox())
        {
            boxDialogueShown = true;
            StartDialogue(boxFoundDialogue, () => waitingForFourthPagePickup = true);
            return;
        }

        if (!lockedHouseDialogueShown && !HasAllFourKeys() && IsNearLockedHouse())
        {
            lockedHouseDialogueShown = true;
            StartDialogue(lockedHouseDialogue, null);
            return;
        }

        if (!listenerDialogueShown && IsNearListener() && interactPressed)
        {
            listenerDialogueShown = true;
            StartDialogue(listenerDialogue, null);
            return;
        }

        if (IsNearBaker() && interactPressed)
        {
            HandleBakerInteraction();
            return;
        }

        if (IsNearBear() && interactPressed)
        {
            HandleBearInteraction();
            return;
        }

        if (IsNearGuard() && interactPressed)
        {
            HandleGuardInteraction();
            return;
        }

        if (mazeOpened && !exitedMaze && IsNearExit())
        {
            exitedMaze = true;
            HideMazeBlock();
            StartDialogue(mazeExitDialogue, null);
        }
    }

    private void HandleBakerInteraction()
    {
        if (bakerQuestCompleted)
        {
            StartDialogue(bakerReadyDialogue, null);
            return;
        }

        if (hasFullHoneyBottle)
        {
            bakerQuestCompleted = true;
            RemoveInventoryItem(fullHoneyJarItemName);
            AddInventoryItem(yellowKeyItemName);
            StartDialogue(bakerRewardDialogue, null);
            return;
        }

        if (!bakerIntroDone)
        {
            bakerIntroDone = true;
            StartDialogue(bakerIntroDialogue, () =>
            {
                waitingForHoneyBottle = true;
                HideRockBear();
                ShowSystemPrompt(findHoneyPrompt, 3f);
            });
            return;
        }

        if (!hasFullHoneyBottle)
        {
            StartDialogue(bakerHintDialogue, null);
            return;
        }

        StartDialogue(bakerRewardDialogue, null);
    }

    private void HandleBearInteraction()
    {
        if (hasFullHoneyBottle || bakerQuestCompleted)
        {
            StartDialogue(bearEmptyDialogue, null);
            return;
        }

        if (hasSilverLeaf && !bearRewardReady)
        {
            RemoveInventoryItem(silverLeafItemName);
            bearRewardReady = true;
            honeyPourReady = true;
            StartDialogue(bearLeafFoundDialogue, () => ShowSystemPrompt(useHoneyStationPrompt, 3f));
            return;
        }

        if (!bearIntroDone)
        {
            bearIntroDone = true;
            bearAskedForSilverLeaf = true;
            ShowSilverLeaf();
            StartDialogue(bearIntroDialogue, null);
            return;
        }

        if (!hasSilverLeaf)
        {
            StartDialogue(bearWaitingDialogue, null);
            return;
        }

        StartDialogue(bearPourDialogue, null);
    }

    private void PickHoneyBottle()
    {
        hasHoneyBottle = true;
        waitingForHoneyBottle = false;
        AddInventoryItem(honeyJarItemName);
        ShowSystemPrompt(honeyFoundPrompt, 3f);
    }

    private void PickSilverLeaf()
    {
        hasSilverLeaf = true;
        AddInventoryItem(silverLeafItemName);
        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(false);
        }

        ShowSystemPrompt(silverLeafFoundPrompt, 3f);
    }

    private void PourHoney()
    {
        hasFullHoneyBottle = true;
        honeyPourReady = false;
        RemoveInventoryItem(honeyJarItemName);
        AddInventoryItem(fullHoneyJarItemName);
        ShowSystemPrompt(fullHoneyFoundPrompt, 3f);
    }

    private IEnumerator OpenLockedHouse()
    {
        if (unlockingHouse || lockedHouseOpened)
        {
            yield break;
        }

        unlockingHouse = true;
        ShowSystemPrompt(unlockingPrompt, 3f);

        Transform[] lockParts = { redLockPart, blueLockPart, greenLockPart, yellowLockPart };
        Vector3[] lockStarts = new Vector3[lockParts.Length];
        Vector3[] lockTargets = new Vector3[lockParts.Length];
        for (int i = 0; i < lockParts.Length; i++)
        {
            if (lockParts[i] == null)
            {
                continue;
            }

            lockStarts[i] = lockParts[i].localPosition;
            lockTargets[i] = new Vector3(lockStarts[i].x, lockPartTargetLocalY, lockStarts[i].z);
        }

        float elapsed = 0f;
        while (elapsed < finalUnlockMoveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / finalUnlockMoveSeconds));
            for (int i = 0; i < lockParts.Length; i++)
            {
                if (lockParts[i] != null)
                {
                    lockParts[i].localPosition = Vector3.Lerp(lockStarts[i], lockTargets[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < lockParts.Length; i++)
        {
            if (lockParts[i] != null)
            {
                lockParts[i].localPosition = lockTargets[i];
            }
        }

        GameObject[] keyObjects = { redKeyObject, blueKeyObject, greenKeyObject, yellowKeyObject };
        Vector3[] keyStarts = new Vector3[keyObjects.Length];
        Vector3[] keyTargets = new Vector3[keyObjects.Length];
        for (int i = 0; i < keyObjects.Length; i++)
        {
            if (keyObjects[i] == null)
            {
                continue;
            }

            keyStarts[i] = keyObjects[i].transform.position;
            keyTargets[i] = keyStarts[i] + Vector3.down * keyDropDistance;
        }

        elapsed = 0f;
        while (elapsed < finalUnlockMoveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / finalUnlockMoveSeconds));
            for (int i = 0; i < keyObjects.Length; i++)
            {
                if (keyObjects[i] != null)
                {
                    keyObjects[i].transform.position = Vector3.Lerp(keyStarts[i], keyTargets[i], t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < keyObjects.Length; i++)
        {
            if (keyObjects[i] != null)
            {
                keyObjects[i].SetActive(false);
            }
        }

        if (finalDoorObject != null)
        {
            finalDoorObject.SetActive(false);
        }

        if (finalWindowObject != null)
        {
            finalWindowObject.SetActive(false);
        }

        RemoveInventoryItem(redKeyItemName);
        RemoveInventoryItem(blueKeyItemName);
        RemoveInventoryItem(greenKeyItemName);
        RemoveInventoryItem(yellowKeyItemName);

        lockedHouseOpened = true;
        unlockingHouse = false;
        ShowSystemPrompt(doorUnlockedPrompt, 3f);
    }

    private void PickFourthPage()
    {
        fourthPagePicked = true;
        waitingForFourthPagePickup = false;
        AddInventoryItem(thirdPageItemName);

        if (fourthPagePaperObject != null)
        {
            fourthPagePaperObject.SetActive(false);
        }

        UnlockThirdPagePortal();
        ShowSystemPrompt(thirdPageFoundPrompt, 3f);
    }

    private void UnlockThirdPagePortal()
    {
        thirdPagePortalUnlocked = true;

        if (thirdPagePortalObject != null)
        {
            thirdPagePortalObject.SetActive(true);
        }
    }

    private void HideRockBear()
    {
        if (rockBearObject != null)
        {
            rockBearObject.SetActive(false);
        }

        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(false);
        }
    }

    private void ShowSilverLeaf()
    {
        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(true);
        }
    }

    private void HandleGuardInteraction()
    {
        if (quizCompleted)
        {
            StartDialogue(guardDoneDialogue, null);
            return;
        }

        if (exitedMaze)
        {
            StartQuizIntro();
            return;
        }

        if (hasPass)
        {
            StartDialogue(guardMazeIntroDialogue, OpenMaze);
            return;
        }

        if (!firstGuardDialogueShown)
        {
            firstGuardDialogueShown = true;
            StartDialogue(guardFirstDialogue, null);
        }
        else
        {
            StartDialogue(guardNoPassDialogue, null);
        }
    }
}
