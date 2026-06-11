// Finds chapter two scene objects and adds missing colliders where the level needs them.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterTwoPuzzle
{
    private void RefreshReferences()
    {
        if (!IsTargetScene())
        {
            return;
        }

        FixReferenceArraySizes();

        if (!thirdPagePortalUnlocked && thirdPagePortalObject != null)
        {
            thirdPagePortalObject.SetActive(false);
        }

        ApplyHoneyQuestObjectVisibility();
        RefreshBoardReferences();

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

    private void RefreshBoardReferences()
    {
        FixReferenceArraySizes();

        bool allReady = startTile != null && endTile != null && dice != null;
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

    private void FixReferenceArraySizes()
    {
        if (boardTiles == null || boardTiles.Length != BoardTileCount)
        {
            System.Array.Resize(ref boardTiles, BoardTileCount);
        }

        if (diceFaces == null || diceFaces.Length != DiceFaceCount)
        {
            System.Array.Resize(ref diceFaces, DiceFaceCount);
        }
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

    private void EnsureMazeColliders()
    {
        if (mazeCollidersReady)
        {
            return;
        }

        if (mazeBlock != null)
        {
            mazeBlock.SetActive(true);
            SetCollidersEnabled(mazeBlock, true);
            EnsureBoxCollider(mazeBlock);
        }

        Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid() || renderer.gameObject.scene != SceneManager.GetActiveScene())
            {
                continue;
            }

            if (!IsInsideMazeColliderArea(renderer.bounds.center) || ShouldSkipRuntimeCollider(renderer.transform))
            {
                continue;
            }

            EnsureSolidCollider(renderer);
        }

        mazeCollidersReady = true;
    }

    private bool IsInsideMazeColliderArea(Vector3 point)
    {
        Vector3 delta = point - mazeColliderCenter;
        return Mathf.Abs(delta.x) <= mazeColliderExtents.x &&
               Mathf.Abs(delta.y) <= mazeColliderExtents.y &&
               Mathf.Abs(delta.z) <= mazeColliderExtents.z;
    }

    private bool ShouldSkipRuntimeCollider(Transform transform)
    {
        if (transform == null)
        {
            return true;
        }

        Transform root = transform.root;
        if ((player != null && root == player.root) ||
            (guard != null && root == guard.root))
        {
            return true;
        }

        string objectName = transform.name;
        return objectName.Contains("interact") ||
               objectName.Contains("Camera") ||
               objectName.Contains("Light");
    }

    private void EnsureSolidCollider(Renderer renderer)
    {
        Collider existing = renderer.GetComponent<Collider>();
        if (existing != null)
        {
            existing.isTrigger = false;
            existing.enabled = true;
            return;
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
        }
    }

    private void EnsureBoxCollider(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        BoxCollider boxCollider = target.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = target.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = false;
        boxCollider.enabled = true;
    }

    private void SetCollidersEnabled(GameObject target, bool enabled)
    {
        if (target == null)
        {
            return;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            collider.isTrigger = false;
            collider.enabled = enabled;
        }
    }

    private void EnsureGuardStandAnimation()
    {
        if (guard == null || guardStandController == null)
        {
            return;
        }

        Animator animator = guard.GetComponent<Animator>();
        if (animator == null)
        {
            animator = guard.gameObject.AddComponent<Animator>();
        }

        if (guardAvatar != null)
        {
            animator.avatar = guardAvatar;
        }

        animator.runtimeAnimatorController = guardStandController;
        animator.applyRootMotion = false;
        animator.enabled = true;
    }

    private bool IsNearGuard()
    {
        return player != null && guardInteract != null && Vector3.Distance(player.position, guardInteract.position) <= interactDistance;
    }

    private bool IsNearBaker()
    {
        return player != null && bakerInteract != null && Vector3.Distance(player.position, bakerInteract.position) <= interactDistance;
    }

    private bool IsNearListener()
    {
        return player != null && listenerInteract != null && Vector3.Distance(player.position, listenerInteract.position) <= interactDistance;
    }

    private bool IsNearLockedHouse()
    {
        return player != null && lockedHouse != null && Vector3.Distance(player.position, lockedHouse.position) <= interactDistance;
    }

    private bool IsNearBox()
    {
        return player != null && box != null && Vector3.Distance(player.position, box.position) <= interactDistance;
    }

    private bool IsNearHoney()
    {
        return player != null && honeyObject != null && honeyObject.activeInHierarchy && Vector3.Distance(player.position, honeyObject.transform.position) <= interactDistance;
    }

    private bool IsNearBear()
    {
        return player != null && bearInteract != null && Vector3.Distance(player.position, bearInteract.position) <= interactDistance;
    }

    private bool IsNearHoneyGive()
    {
        return player != null && honeyGive != null && Vector3.Distance(player.position, honeyGive.position) <= interactDistance;
    }

    private bool IsNearSilverLeaf()
    {
        return player != null && silverLeafObject != null && silverLeafObject.activeInHierarchy && Vector3.Distance(player.position, silverLeafObject.transform.position) <= interactDistance;
    }

    private bool IsNearThirdPagePortal()
    {
        return player != null &&
               thirdPagePortalObject != null &&
               thirdPagePortalObject.activeInHierarchy &&
               Vector3.Distance(player.position, thirdPagePortalObject.transform.position) <= interactDistance;
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

        return startTile != null && Vector3.Distance(player.position, startTile.position) <= boardInteractDistance;
    }

    private bool IsNearExit()
    {
        return player != null && exitInteract != null && Vector3.Distance(player.position, exitInteract.position) <= exitDistance;
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }
}
