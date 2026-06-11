// Runs the odd-egg visual challenge inside the old tree quest.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void StartEggChallenge()
    {
        currentAnswer = null;
        branchFlowActive = true;
        LockPlayerForEggChallenge();
        BeginEggLevel(1);
    }

    private void BeginEggLevel(int level)
    {
        ClearEggGrid();

        currentEggLevel = Mathf.Clamp(level, 1, 3);
        currentEggGridSize = GetEggGridSize(currentEggLevel);
        eggTimer = eggLevelDuration;
        eggResultText = null;
        state = DialogueState.EggChallenge;

        SpawnEggGrid(currentEggGridSize, GetOddEggPrefab(currentEggLevel));
    }

    private void UpdateEggChallenge()
    {
        eggTimer -= Time.deltaTime;
        if (eggTimer <= 0f)
        {
            FailEggChallenge();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TrySelectEgg();
        }
    }

    private void TrySelectEgg()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return;
        }

        GameObject selectedEgg = FindSpawnedEggRoot(hit.transform);
        if (selectedEgg == null)
        {
            return;
        }

        if (selectedEgg == correctEgg)
        {
            CompleteEggLevel();
        }
        else
        {
            FailEggChallenge();
        }
    }

    private GameObject FindSpawnedEggRoot(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            GameObject currentObject = current.gameObject;
            if (spawnedEggs.Contains(currentObject))
            {
                return currentObject;
            }

            current = current.parent;
        }

        return null;
    }

    private void CompleteEggLevel()
    {
        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
        }

        string successText = GetEggSuccessText(currentEggLevel);
        GameAudioManager.PlaySuccess();
        eggResultCoroutine = StartCoroutine(ShowEggSuccessThenContinue(successText));
    }

    private IEnumerator ShowEggSuccessThenContinue(string successText)
    {
        eggResultText = successText;
        state = DialogueState.EggChallengeResult;
        yield return new WaitForSeconds(eggResultDuration);

        ClearEggGrid();
        eggResultText = gameSuccessText;
        yield return new WaitForSeconds(eggResultDuration);
        eggResultText = null;
        state = DialogueState.RewardChoosing;
        eggResultCoroutine = null;
    }

    private void FailEggChallenge()
    {
        ClearEggGrid();
        eggResultText = gameFailedText;
        state = DialogueState.EggChallengeFailed;
        GameAudioManager.PlayFail();

        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
            eggResultCoroutine = null;
        }
    }

    private void RestartEggChallenge()
    {
        if (eggResultCoroutine != null)
        {
            StopCoroutine(eggResultCoroutine);
            eggResultCoroutine = null;
        }

        BeginEggLevel(1);
    }

    private void ExitEggChallenge()
    {
        ClearEggGrid();
        eggResultText = null;
        CloseDialogueAndReset();
    }

    private void LockPlayerForEggChallenge()
    {
        if (eggPlayerControlLocked || player == null)
        {
            return;
        }

        eggPlayerControlLocked = true;
        originalCursorLockMode = Cursor.lockState;
        originalCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        eggDisabledPlayerBehaviours.Clear();
        MonoBehaviour[] playerBehaviours = player.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < playerBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = playerBehaviours[i];
            if (behaviour != null && behaviour.enabled)
            {
                behaviour.enabled = false;
                eggDisabledPlayerBehaviours.Add(behaviour);
            }
        }
    }

    private void UnlockPlayerForEggChallenge()
    {
        if (!eggPlayerControlLocked)
        {
            return;
        }

        for (int i = 0; i < eggDisabledPlayerBehaviours.Count; i++)
        {
            MonoBehaviour behaviour = eggDisabledPlayerBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        eggDisabledPlayerBehaviours.Clear();
        Cursor.lockState = originalCursorLockMode;
        Cursor.visible = originalCursorVisible;
        eggPlayerControlLocked = false;
    }

    private void SpawnEggGrid(int gridSize, GameObject oddPrefab)
    {
        if (eggPrefab == null || oddPrefab == null)
        {
            FailEggChallenge();
            return;
        }

        eggGridRoot = new GameObject("Old Tree Egg Challenge");
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 center;
        Vector3 right;
        Vector3 up;
        Quaternion rotation;

        if (cameraTransform != null)
        {
            center = cameraTransform.position + cameraTransform.forward * eggGridDistance;
            right = cameraTransform.right;
            up = cameraTransform.up;
            rotation = Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up);
        }
        else
        {
            center = interactionTarget.position + Vector3.up * 2.5f + transform.forward * eggGridDistance;
            right = transform.right;
            up = Vector3.up;
            rotation = transform.rotation;
        }

        int totalCount = gridSize * gridSize;
        int oddIndex = Random.Range(0, totalCount);
        float half = (gridSize - 1) * 0.5f;

        for (int i = 0; i < totalCount; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;
            bool isOdd = i == oddIndex;
            GameObject prefab = isOdd ? oddPrefab : eggPrefab;
            Vector3 position = center + right * ((x - half) * eggGridSpacing) + up * ((half - y) * eggGridSpacing);
            GameObject egg = Instantiate(prefab, position, rotation, eggGridRoot.transform);
            egg.transform.localScale = Vector3.one * eggScale;
            EnsureClickableCollider(egg);
            spawnedEggs.Add(egg);

            if (isOdd)
            {
                correctEgg = egg;
            }
        }
    }

    private void EnsureClickableCollider(GameObject target)
    {
        if (target.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        BoxCollider collider = target.AddComponent<BoxCollider>();
        if (renderers.Length == 0)
        {
            collider.size = Vector3.one;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        collider.center = target.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private void ClearEggGrid()
    {
        for (int i = 0; i < spawnedEggs.Count; i++)
        {
            if (spawnedEggs[i] != null)
            {
                Destroy(spawnedEggs[i]);
            }
        }

        spawnedEggs.Clear();
        correctEgg = null;

        if (eggGridRoot != null)
        {
            Destroy(eggGridRoot);
            eggGridRoot = null;
        }
    }

    private int GetEggGridSize(int level)
    {
        if (level == 1)
        {
            return 5;
        }

        if (level == 2)
        {
            return 10;
        }

        return 20;
    }

    private GameObject GetOddEggPrefab(int level)
    {
        if (level == 1)
        {
            return levelOneOddEggPrefab;
        }

        if (level == 2)
        {
            return levelTwoOddEggPrefab;
        }

        return levelThreeOddEggPrefab;
    }

    private string GetEggLevelTitle()
    {
        if (currentEggLevel == 1)
        {
            return levelOneTitle;
        }

        if (currentEggLevel == 2)
        {
            return levelTwoTitle;
        }

        return levelThreeTitle;
    }

    private string GetEggSuccessText(int level)
    {
        if (level == 1)
        {
            return levelOneSuccessText;
        }

        if (level == 2)
        {
            return levelTwoSuccessText;
        }

        return gameSuccessText;
    }
}
