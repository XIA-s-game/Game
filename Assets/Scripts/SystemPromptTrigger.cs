using System.Collections.Generic;
using UnityEngine;

public class SystemPromptTrigger : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform firstPromptMarker;
    [SerializeField] private Transform secondPromptMarker;

    [Header("Prompts")]
    [SerializeField] private float triggerDistance = 1.5f;
    [SerializeField] private float markerBoundsPadding = 0.15f;
    [SerializeField] private float markerVerticalTolerance = 4f;
    [SerializeField] private float promptDuration = 3f;
    [SerializeField] private string puzzlePrompt = "What is this strange symbol?";
    [SerializeField] private string altarPrompt = "What is this mysterious altar?";

    private readonly Queue<string> promptQueue = new Queue<string>();
    private readonly Dictionary<Transform, Bounds> detectionBoundsCache = new Dictionary<Transform, Bounds>();
    private string currentPrompt;
    private float promptEndsAt;
    private bool puzzlePromptShown;
    private bool altarPromptShown;
    private GUIStyle promptStyle;

    private void Update()
    {
        UpdatePromptQueue();

        if (player == null)
        {
            return;
        }

        TryShowNearPrompt(ref puzzlePromptShown, firstPromptMarker, puzzlePrompt);
        TryShowNearPrompt(ref altarPromptShown, secondPromptMarker, altarPrompt);
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(currentPrompt) || Time.time >= promptEndsAt)
        {
            return;
        }

        Rect rect = GameUiStyle.SystemPromptRect(760f, 92f);
        GameUiStyle.DrawDialoguePanel(rect);

        GUIStyle style = GameUiStyle.LabelStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Normal, true);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, rect.height - 24f), currentPrompt, style);
    }

    private void TryShowNearPrompt(ref bool shown, Transform target, string text)
    {
        if (shown || target == null)
        {
            return;
        }

        if (!IsPlayerStandingOnTarget(target))
        {
            return;
        }

        ShowPromptOnce(ref shown, text);
    }

    private void ShowPromptOnce(ref bool shown, string text)
    {
        if (shown)
        {
            return;
        }

        shown = true;
        promptQueue.Enqueue(text);
        UpdatePromptQueue();
    }

    private void UpdatePromptQueue()
    {
        if (!string.IsNullOrEmpty(currentPrompt) && Time.time < promptEndsAt)
        {
            return;
        }

        if (promptQueue.Count == 0)
        {
            currentPrompt = null;
            return;
        }

        currentPrompt = promptQueue.Dequeue();
        promptEndsAt = Time.time + promptDuration;
        GameAudioManager.PlayKnob();
    }

    private bool IsPlayerStandingOnTarget(Transform target)
    {
        if (player == null || target == null)
        {
            return false;
        }

        if (TryGetDetectionBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool insideX = position.x >= bounds.min.x - markerBoundsPadding && position.x <= bounds.max.x + markerBoundsPadding;
            bool insideZ = position.z >= bounds.min.z - markerBoundsPadding && position.z <= bounds.max.z + markerBoundsPadding;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(markerVerticalTolerance, bounds.extents.y + markerVerticalTolerance);
            return insideX && insideZ && nearY;
        }

        Vector3 playerFlat = new Vector3(player.position.x, 0f, player.position.z);
        Vector3 targetFlat = new Vector3(target.position.x, 0f, target.position.z);
        return Vector3.Distance(playerFlat, targetFlat) <= triggerDistance;
    }

    private bool TryGetDetectionBounds(Transform target, out Bounds bounds)
    {
        if (detectionBoundsCache.TryGetValue(target, out bounds))
        {
            return true;
        }

        if (!TryBuildDetectionBounds(target, out bounds))
        {
            return false;
        }

        detectionBoundsCache[target] = bounds;
        return true;
    }

    private static bool TryBuildDetectionBounds(Transform target, out Bounds bounds)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bounds = new Bounds(target.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        return TryGetWorldBounds(target, out bounds);
    }

    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(target.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}
