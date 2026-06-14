using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterTwoPuzzle
{
    private void ReadSceneReferences()
    {
        if (!IsTargetScene())
        {
            return;
        }

        if (!thirdPagePortalUnlocked && thirdPagePortalObject != null)
        {
            thirdPagePortalObject.SetActive(false);
        }

        ApplyHoneyQuestObjectVisibility();
        ReadBoardSlots();

        if (guard != null && !guardOriginalPositionReady)
        {
            guardOriginalPosition = guard.position;
            guardOriginalPositionReady = true;
        }

        if (mazeBlock != null && !mazeBlockOriginalPositionReady)
        {
            mazeBlockOriginalPosition = mazeBlock.transform.position;
            mazeBlockOriginalPositionReady = true;
        }
    }

    private void ReadBoardSlots()
    {
        bool allReady = HasExpectedBoardReferenceCounts() && startTile != null && endTile != null && dice != null;
        for (int i = 1; i < boardTiles.Length; i++)
        {
            allReady &= boardTiles[i] != null;
        }

        for (int i = 1; i < diceFaces.Length; i++)
        {
            allReady &= diceFaces[i] != null;
        }

        if (dice != null && !diceOriginalTransformReady)
        {
            CacheDiceOriginalTransform();
        }

        boardReferencesReady = allReady;
    }

    private bool HasExpectedBoardReferenceCounts()
    {
        return boardTiles != null && boardTiles.Length == BoardTileCount &&
               diceFaces != null && diceFaces.Length == DiceFaceCount;
    }

    private void CacheDiceOriginalTransform()
    {
        if (dice == null)
        {
            return;
        }

        diceOriginalPosition = dice.position;
        diceOriginalRotation = dice.rotation;
        diceOriginalTransformReady = true;

        for (int i = 1; i < diceFaces.Length; i++)
        {
            if (diceFaces[i] != null)
            {
                Vector3 fromCenter = diceFaces[i].position - dice.position;
                diceFaceLocalNormals[i] = dice.InverseTransformDirection(fromCenter).normalized;
            }
        }
    }

    private void ApplyHoneyQuestObjectVisibility()
    {
        if (silverLeafObject != null)
        {
            silverLeafObject.SetActive(bearAskedForSilverLeaf && !hasSilverLeaf);
        }

        if (honeyGive != null)
        {
            honeyGive.gameObject.SetActive(true);
        }

        if (rockBearObject != null && bakerIntroDone)
        {
            rockBearObject.SetActive(false);
        }
    }

    private bool IsNear(Transform target, float distance)
    {
        return player != null &&
               target != null &&
               Vector3.Distance(player.position, target.position) <= distance;
    }

    private bool IsNear(GameObject target, float distance)
    {
        return player != null &&
               target != null &&
               target.activeInHierarchy &&
               Vector3.Distance(player.position, target.transform.position) <= distance;
    }

    private bool IsNearGuard()
    {
        return IsNear(guardInteract, interactDistance);
    }

    private bool IsNearBaker()
    {
        return IsNear(bakerInteract, interactDistance);
    }

    private bool IsNearListener()
    {
        return IsNear(listenerInteract, interactDistance);
    }

    private bool IsNearLockedHouse()
    {
        return IsNear(lockedHouse, interactDistance);
    }

    private bool IsNearBox()
    {
        return IsNear(box, interactDistance);
    }

    private bool IsNearHoney()
    {
        return IsNear(honeyObject, interactDistance);
    }

    private bool IsNearBear()
    {
        return IsNear(bearInteract, interactDistance);
    }

    private bool IsNearHoneyGive()
    {
        return IsNear(honeyGive, interactDistance);
    }

    private bool IsNearSilverLeaf()
    {
        return IsNear(silverLeafObject, interactDistance);
    }

    private bool IsNearThirdPagePortal()
    {
        return IsNear(thirdPagePortalObject, interactDistance);
    }

    private void AddInventoryItem(string itemName)
    {
        if (!inventoryItems.Contains(itemName))
        {
            inventoryItems.Add(itemName);
            GlobalBackpackUI.AddItem(itemName);
        }
    }

    private void RemoveInventoryItem(string itemName)
    {
        if (inventoryItems.Remove(itemName))
        {
            GlobalBackpackUI.RemoveItem(itemName);
        }
    }

    private bool HasAllFourKeys()
    {
        return inventoryItems.Contains(redKeyItemName) &&
               inventoryItems.Contains(blueKeyItemName) &&
               inventoryItems.Contains(greenKeyItemName) &&
               inventoryItems.Contains(yellowKeyItemName);
    }

    private bool IsNearStartTile()
    {
        if (hasPass || player == null)
        {
            return false;
        }

        return IsNear(startTile, boardInteractDistance);
    }

    private bool IsNearExit()
    {
        return IsNear(exitInteract, exitDistance);
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }

}
