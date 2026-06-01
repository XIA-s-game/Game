// Main function: Runs the magic cube color challenge, including arena creation, round checks, failure animation, win state, and challenge UI.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicCubeChallenge : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform interactObject;
    [SerializeField] private Transform arenaRoot;
    [SerializeField] private Renderer arenaFloor;

    [Header("Rules")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private int gridSize = 30;
    [SerializeField] private float tileSize = 1.35f;
    [SerializeField] private float failY = -8f;
    [SerializeField] private float fallAnimationSeconds = 1.25f;
    [SerializeField] private float fallAnimationDistance = 16f;
    [SerializeField] private Vector3 arenaCenter = new Vector3(0f, 300f, 0f);
    [SerializeField] private float tileHeight = 0.16f;
    [SerializeField] private float playerGroundOffset = 0.01f;

    private static readonly Color[] TileColors =
    {
        Color.red,
        Color.yellow,
        new Color(1f, 0.45f, 0f),
        Color.blue,
        Color.green,
        new Color(0.45f, 0.24f, 0.08f),
        new Color(0.48f, 0.12f, 0.78f),
        Color.black,
        Color.white
    };

    private static readonly string[] ColorNames =
    {
        "Red",
        "Yellow",
        "Orange",
        "Blue",
        "Green",
        "Brown",
        "Purple",
        "Black",
        "White"
    };

    private readonly int[] roundSeconds = { 30, 20, 15, 10, 5 };
    private readonly int[] targetColorIndices = { 0, 3, 4, 1, 6 };
    private readonly List<GameObject> tiles = new List<GameObject>();
    private readonly List<int> tileColorIndices = new List<int>();

    private CharacterController controller;
    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private Material[] colorMaterials;
    private Material whiteMaterial;
    private int currentRound;
    private float roundEndsAt;
    private bool active;
    private bool waitingForRound;
    private bool failed;
    private bool won;
    private bool showingResult;
    private bool rewardGiven;
    private Coroutine fallRoutine;
    private GUIStyle promptStyle;
    private GUIStyle titleStyle;

    // Function: Initializes component references, cached state, and default runtime data.
    private void Awake()
    {
        if (arenaRoot != null)
        {
            arenaRoot.gameObject.SetActive(false);
        }
    }

    // Function: Updates input handling, interaction checks, and active gameplay flow each frame.
    private void Update()
    {
        if (!active)
        {
            if (IsNearInteractObject() && Input.GetKeyDown(interactKey))
            {
                StartChallenge();
            }

            return;
        }

        if (showingResult)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                RestartChallenge();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                ExitChallenge();
            }

            return;
        }

        if (failed || won)
        {
            return;
        }

        if (player != null && player.position.y < arenaCenter.y + failY)
        {
            FailChallenge();
            return;
        }

        if (waitingForRound && Time.time >= roundEndsAt)
        {
            ResolveRound();
        }
    }

    // Function: Draws this script's IMGUI prompts, panels, and dialogue.
    private void OnGUI()
    {
        if (!active && IsNearInteractObject())
        {
            DrawPrompt("Press E to start");
            return;
        }

        if (!active)
        {
            return;
        }

        if (failed)
        {
            DrawResultPanel("Game failed");
            return;
        }

        if (won)
        {
            DrawResultPanel("Game won. Blue Key received.");
            return;
        }

        float timeLeft = Mathf.Max(0f, roundEndsAt - Time.time);
        string text = "Round " + (currentRound + 1) + "  Target: " + ColorNames[targetColorIndices[currentRound]] + "  Time: " + Mathf.CeilToInt(timeLeft);
        DrawPrompt(text);
    }

    // Function: Starts the challenge flow.
    private void StartChallenge()
    {
        if (player == null)
        {
            return;
        }

        returnPosition = player.position;
        returnRotation = player.rotation;
        EnsureArena();
        currentRound = 0;
        active = true;
        failed = false;
        won = false;
        showingResult = false;
        BuildGrid();
        MovePlayer(GetArenaPlayerStartPosition(), Quaternion.identity);
        StartRound();
    }

    // Function: Starts the round flow.
    private void StartRound()
    {
        BuildGrid();
        waitingForRound = true;
        roundEndsAt = Time.time + roundSeconds[Mathf.Clamp(currentRound, 0, roundSeconds.Length - 1)];
    }

    // Function: Runs the resolve round logic.
    private void ResolveRound()
    {
        waitingForRound = false;
        int targetColor = targetColorIndices[currentRound];
        RemoveWrongTiles(targetColor);

        if (!IsPlayerOnTargetColor(targetColor))
        {
            FailChallenge();
            return;
        }

        currentRound++;
        if (currentRound >= roundSeconds.Length)
        {
            WinChallenge();
            return;
        }

        StartCoroutine(NextRoundAfterDelay());
    }

    // Function: Runs the next round after delay logic.
    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(1.2f);
        if (active && !failed && !won)
        {
            StartRound();
        }
    }

    // Function: Handles failure for challenge, including prompts, reset, or penalty.
    private void FailChallenge()
    {
        failed = true;
        waitingForRound = false;
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
        }

        fallRoutine = StartCoroutine(PlayFallAnimation());
    }

    // Function: Handles success for challenge, including rewards and exit state.
    private void WinChallenge()
    {
        won = true;
        showingResult = true;
        waitingForRound = false;
        if (!rewardGiven)
        {
            rewardGiven = true;
            ChapterTwoPuzzle.AddItemToInventory("Blue Key");
        }
    }

    // Function: Runs the restart challenge logic.
    private void RestartChallenge()
    {
        failed = false;
        won = false;
        showingResult = false;
        currentRound = 0;
        MovePlayer(GetArenaPlayerStartPosition(), Quaternion.identity);
        StartRound();
    }

    // Function: Exits challenge and restores exploration state.
    private void ExitChallenge()
    {
        active = false;
        failed = false;
        won = false;
        showingResult = false;
        waitingForRound = false;
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }

        ClearTiles();
        if (arenaRoot != null)
        {
            arenaRoot.gameObject.SetActive(false);
        }

        MovePlayer(returnPosition, returnRotation);
    }

    // Function: Plays fall animation animation, audio, or cutscene behavior.
    private IEnumerator PlayFallAnimation()
    {
        if (player == null)
        {
            showingResult = true;
            yield break;
        }

        controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            controller.enabled = false;
        }

        Vector3 start = player.position;
        Vector3 target = start + Vector3.down * fallAnimationDistance;
        float elapsed = 0f;
        while (elapsed < fallAnimationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallAnimationSeconds);
            player.position = Vector3.Lerp(start, target, t * t);
            yield return null;
        }

        player.position = target;
        if (controller != null)
        {
            controller.enabled = wasEnabled;
        }

        showingResult = true;
        fallRoutine = null;
    }

    // Function: Ensures arena exists, is configured, or is ready to use.
    private void EnsureArena()
    {
        EnsureMaterials();
        if (arenaRoot == null)
        {
            return;
        }

        arenaRoot.gameObject.SetActive(true);
        if (arenaFloor != null)
        {
            arenaFloor.sharedMaterial = whiteMaterial;
        }
    }

    // Function: Builds the data or scene objects needed for grid.
    private void BuildGrid()
    {
        ClearTiles();
        EnsureMaterials();

        float offset = (gridSize - 1) * tileSize * 0.5f;
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int colorIndex = Random.Range(0, TileColors.Length);
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "MagicCubeTile";
                tile.transform.SetParent(arenaRoot, false);
                tile.transform.position = arenaCenter + new Vector3(x * tileSize - offset, 0f, z * tileSize - offset);
                tile.transform.localScale = new Vector3(tileSize * 0.96f, tileHeight, tileSize * 0.96f);
                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = colorMaterials[colorIndex];
                }

                tiles.Add(tile);
                tileColorIndices.Add(colorIndex);
            }
        }
    }

    // Function: Clears tiles.
    private void ClearTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null)
            {
                Destroy(tiles[i]);
            }
        }

        tiles.Clear();
        tileColorIndices.Clear();
    }

    // Function: Removes wrong tiles.
    private void RemoveWrongTiles(int targetColor)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null && tileColorIndices[i] != targetColor)
            {
                tiles[i].SetActive(false);
            }
        }
    }

    // Function: Checks whether player on target color is true.
    private bool IsPlayerOnTargetColor(int targetColor)
    {
        if (player == null)
        {
            return false;
        }

        Vector3 position = player.position;
        float bestDistance = float.PositiveInfinity;
        int bestIndex = -1;
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null || !tiles[i].activeInHierarchy)
            {
                continue;
            }

            Vector3 tilePosition = tiles[i].transform.position;
            float dx = Mathf.Abs(position.x - tilePosition.x);
            float dz = Mathf.Abs(position.z - tilePosition.z);
            if (dx <= tileSize * 0.5f && dz <= tileSize * 0.5f)
            {
                float distance = dx + dz;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
        }

        return bestIndex >= 0 && tileColorIndices[bestIndex] == targetColor;
    }

    // Function: Moves player toward its target position or state.
    private void MovePlayer(Vector3 position, Quaternion rotation)
    {
        if (player == null)
        {
            return;
        }

        controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.SetPositionAndRotation(position, rotation);

        if (controller != null)
        {
            controller.enabled = wasEnabled;
        }
    }

    // Function: Gets or calculates arena player start position.
    private Vector3 GetArenaPlayerStartPosition()
    {
        Vector3 position = arenaCenter;
        position.y = arenaCenter.y + tileHeight * 0.5f + playerGroundOffset - GetControllerFootOffset();
        return position;
    }

    // Function: Gets or calculates controller foot offset.
    private float GetControllerFootOffset()
    {
        if (player == null)
        {
            return 0f;
        }

        controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            return 0f;
        }

        return controller.center.y - controller.height * 0.5f;
    }

    // Function: Draws the UI elements for prompt.
    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(560f, 64f);
        DrawPanel(rect);
        GUI.Label(rect, text, GetStyle(ref promptStyle, 26, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    // Function: Draws the UI elements for result panel.
    private void DrawResultPanel(string title)
    {
        Rect rect = GameUiStyle.SystemPromptRect(480f, 220f);
        DrawPanel(rect);
        GUI.Label(new Rect(rect.x + 20f, rect.y + 22f, rect.width - 40f, 44f), title, GetStyle(ref titleStyle, 28, TextAnchor.MiddleCenter, FontStyle.Bold));

        GUI.Label(new Rect(rect.x + 20f, rect.y + 96f, rect.width - 40f, 34f), "Press A to restart", GetStyle(ref promptStyle, 22, TextAnchor.MiddleCenter, FontStyle.Bold));
        GUI.Label(new Rect(rect.x + 20f, rect.y + 132f, rect.width - 40f, 34f), "Press B to exit", GetStyle(ref promptStyle, 22, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    // Function: Draws a reusable dark UI panel background.
    private void DrawPanel(Rect rect)
    {
        GameUiStyle.DrawPanel(rect);
    }

    // Function: Gets or calculates style.
    private GUIStyle GetStyle(ref GUIStyle style, int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
        }

        style.fontSize = fontSize;
        style.alignment = alignment;
        style.fontStyle = fontStyle;
        style.normal.textColor = Color.white;
        return style;
    }

    // Function: Ensures materials exists, is configured, or is ready to use.
    private void EnsureMaterials()
    {
        if (colorMaterials != null && whiteMaterial != null)
        {
            return;
        }

        colorMaterials = new Material[TileColors.Length];
        for (int i = 0; i < TileColors.Length; i++)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = TileColors[i];
            colorMaterials[i] = material;
        }

        whiteMaterial = new Material(Shader.Find("Standard"));
        whiteMaterial.color = Color.white;
    }

    // Function: Checks whether nearby interact object is true.
    private bool IsNearInteractObject()
    {
        return player != null && interactObject != null && Vector3.Distance(player.position, interactObject.position) <= interactDistance;
    }
}
