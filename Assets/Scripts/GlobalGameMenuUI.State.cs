using System.Collections;
using UnityEngine;

public partial class GlobalGameMenuUI
{
    private static void SavePlayerTransform(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            PlayerPrefs.DeleteKey(SaveHasPlayerTransformKey);
            return;
        }

        Vector3 position = playerTransform.position;
        PlayerPrefs.SetInt(SaveHasPlayerTransformKey, 1);
        PlayerPrefs.SetFloat(SavedPlayerPositionXKey, position.x);
        PlayerPrefs.SetFloat(SavedPlayerPositionYKey, position.y);
        PlayerPrefs.SetFloat(SavedPlayerPositionZKey, position.z);
        PlayerPrefs.SetFloat(SavedPlayerRotationYKey, playerTransform.eulerAngles.y);
    }

    private void TryRestoreSavedPlayerTransform(string loadedSceneName)
    {
        if (PlayerPrefs.GetInt(PendingContinueLoadKey, 0) != 1)
        {
            return;
        }

        string savedSceneName = PlayerPrefs.GetString(SaveSceneKey, string.Empty);
        if (string.Equals(savedSceneName, loadedSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(RestoreSavedPlayerTransformWhenReady());
        }
    }

    private IEnumerator RestoreSavedPlayerTransformWhenReady()
    {
        for (int attempt = 0; attempt < 90; attempt++)
        {
            if (player != null)
            {
                ApplySavedPlayerTransform(player);
                PlayerPrefs.DeleteKey(PendingContinueLoadKey);
                PlayerPrefs.Save();
                yield break;
            }

            yield return null;
        }
    }

    private static void ApplySavedPlayerTransform(Transform playerTransform)
    {
        if (PlayerPrefs.GetInt(SaveHasPlayerTransformKey, 0) != 1)
        {
            return;
        }

        Vector3 position = new Vector3(
            PlayerPrefs.GetFloat(SavedPlayerPositionXKey, playerTransform.position.x),
            PlayerPrefs.GetFloat(SavedPlayerPositionYKey, playerTransform.position.y),
            PlayerPrefs.GetFloat(SavedPlayerPositionZKey, playerTransform.position.z));

        float yaw = PlayerPrefs.GetFloat(SavedPlayerRotationYKey, playerTransform.eulerAngles.y);
        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (controller != null)
        {
            controller.enabled = false;
        }

        playerTransform.position = position;
        playerTransform.rotation = Quaternion.Euler(playerTransform.eulerAngles.x, yaw, playerTransform.eulerAngles.z);

        if (controller != null)
        {
            controller.enabled = wasEnabled;
        }
    }

    private void SaveAndUnlockCursor()
    {
        if (!cursorStateSaved)
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            cursorStateSaved = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreCursor()
    {
        if (!cursorStateSaved)
        {
            return;
        }

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        cursorStateSaved = false;
    }
}
