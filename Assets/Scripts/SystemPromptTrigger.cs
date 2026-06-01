// Main function: Shows one-time system prompts based on player position and briefly highlights prompt targets with runtime collider support.

using System.Collections;
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
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.12f, 1f);

    private readonly Queue<string> promptQueue = new Queue<string>();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> propertyBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
    private readonly HashSet<Transform> colliderReadyTargets = new HashSet<Transform>();
    private string currentPrompt;
    private float promptEndsAt;
    private bool puzzlePromptShown;
    private bool altarPromptShown;

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        EnsureSolidCollider(firstPromptMarker);
        EnsureSolidCollider(secondPromptMarker);
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        UpdatePromptQueue();

        if (player == null)
        {
            return;
        }

        TryShowNearPrompt(ref puzzlePromptShown, firstPromptMarker, puzzlePrompt, false);
        TryShowNearPrompt(ref altarPromptShown, secondPromptMarker, altarPrompt, false);
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        if (string.IsNullOrEmpty(currentPrompt) || Time.time >= promptEndsAt)
        {
            return;
        }

        Rect rect = GameUiStyle.SystemPromptRect(760f, 92f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            wordWrap = true
        };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, rect.height - 24f), currentPrompt, style);
    }

    // Function: Tries to process show nearby prompt and returns whether it succeeded.
    private void TryShowNearPrompt(ref bool shown, Transform target, string text, bool highlightTarget)
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

        if (highlightTarget)
        {
            StartCoroutine(HighlightForSeconds(target, promptDuration));
        }
    }

    // Function: Shows prompt once.
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

    // Function: Updates prompt queue state, input, or presentation.
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
    }

    // Function: Checks whether player standing on target is true.
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

    // Function: Tries to get detection bounds and returns whether it was found.
    private static bool TryGetDetectionBounds(Transform target, out Bounds bounds)
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

    // Function: Runs the highlight for seconds logic.
    private IEnumerator HighlightForSeconds(Transform target, float seconds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        float endTime = Time.time + seconds;

        while (Time.time < endTime)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = GetPropertyBlock(renderer);
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", highlightColor);
                block.SetColor("_BaseColor", highlightColor);
                block.SetColor("_EmissionColor", highlightColor);
                renderer.SetPropertyBlock(block);
            }

            yield return null;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }
    }

    // Function: Gets or calculates property block.
    private MaterialPropertyBlock GetPropertyBlock(Renderer renderer)
    {
        if (!propertyBlocks.TryGetValue(renderer, out MaterialPropertyBlock block))
        {
            block = new MaterialPropertyBlock();
            propertyBlocks.Add(renderer, block);
        }

        return block;
    }

    // Function: Ensures solid collider exists, is configured, or is ready to use.
    private void EnsureSolidCollider(Transform target)
    {
        if (target == null || colliderReadyTargets.Contains(target))
        {
            return;
        }

        bool alreadyHadCollider = HasSolidCollider(target);
        bool addedCollider = AddMeshColliders(target);
        if (!alreadyHadCollider && !addedCollider)
        {
            addedCollider = AddRendererBoxColliders(target);
        }

        if (alreadyHadCollider || addedCollider)
        {
            colliderReadyTargets.Add(target);
        }
    }

    // Function: Checks whether solid collider already exists or is available.
    private static bool HasSolidCollider(Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null && !collider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    // Function: Adds mesh colliders.
    private static bool AddMeshColliders(Transform target)
    {
        bool addedAny = false;
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Collider existingCollider = meshFilter.GetComponent<Collider>();
            if (existingCollider != null && !existingCollider.isTrigger)
            {
                continue;
            }

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            addedAny = true;
        }

        return addedAny;
    }

    // Function: Adds renderer box colliders.
    private static bool AddRendererBoxColliders(Transform target)
    {
        bool addedAny = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.GetComponent<Collider>() != null)
            {
                continue;
            }

            BoxCollider collider = renderer.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.center = renderer.transform.InverseTransformPoint(renderer.bounds.center);
            collider.size = DivideByLossyScale(renderer.bounds.size, renderer.transform.lossyScale);
            addedAny = true;
        }

        return addedAny;
    }

    // Function: Tries to get world bounds and returns whether it was found.
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

    // Function: Safely converts by lossy scale through object scale.
    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            DivideByScale(size.x, lossyScale.x),
            DivideByScale(size.y, lossyScale.y),
            DivideByScale(size.z, lossyScale.z));
    }

    // Function: Safely converts by scale through object scale.
    private static float DivideByScale(float value, float scale)
    {
        return Mathf.Abs(scale) > 0.0001f ? value / Mathf.Abs(scale) : value;
    }
}
