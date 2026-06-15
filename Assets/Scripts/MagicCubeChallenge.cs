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

    [Header("Start Prompt")]
    [SerializeField] private string startPromptText = "Stand on the target colored block. When the countdown ends, all other colored blocks will disappear.";
    [SerializeField] private float startPromptSeconds = 5f;
    [SerializeField] private Vector2 startPromptSize = new Vector2(920f, 140f);
    [SerializeField] private int startPromptFontSize = 26;

    [Header("Game UI")]
    [SerializeField] private Vector2 roundPromptSize = new Vector2(560f, 64f);
    [SerializeField] private int roundPromptFontSize = 26;
    [SerializeField] private Vector2 resultPanelSize = new Vector2(480f, 220f);
    [SerializeField] private Rect resultTitleRect = new Rect(20f, 22f, -40f, 44f);
    [SerializeField] private Rect resultRestartRect = new Rect(20f, 96f, -40f, 34f);
    [SerializeField] private Rect resultExitRect = new Rect(20f, 132f, -40f, 34f);
    [SerializeField] private int resultTitleFontSize = 28;
    [SerializeField] private int resultPromptFontSize = 22;

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
    private GUIStyle startPromptStyle;
    private float startPromptEndsAt;

    private void Awake()
    {
        if (arenaRoot != null)
        {
            arenaRoot.gameObject.SetActive(false);
        }
    }

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
            FinishRound();
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (!active)
        {
            if (IsNearInteractObject())
            {
                DrawPrompt("Press E to start");
            }

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

        if (Time.time < startPromptEndsAt)
        {
            DrawStartPrompt();
        }
    }

    private void StartChallenge()
    {
        if (player == null)
        {
            return;
        }

        returnPosition = player.position;
        returnRotation = player.rotation;
        OpenArena();
        currentRound = 0;
        active = true;
        failed = false;
        won = false;
        showingResult = false;
        MovePlayer(GetArenaPlayerStartPosition(), Quaternion.identity);
        StartRound();
        ShowStartPrompt();
    }

    private void StartRound()
    {
        BuildGrid();
        waitingForRound = true;
        roundEndsAt = Time.time + roundSeconds[Mathf.Clamp(currentRound, 0, roundSeconds.Length - 1)];
    }

    private void FinishRound()
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

    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(1.2f);
        if (active && !failed && !won)
        {
            StartRound();
        }
    }

    private void FailChallenge()
    {
        failed = true;
        GameAudioManager.PlayFail();
        waitingForRound = false;
        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
        }

        fallRoutine = StartCoroutine(PlayFallAnimation());
    }

    private void WinChallenge()
    {
        won = true;
        GameAudioManager.PlaySuccess();
        showingResult = true;
        waitingForRound = false;
        if (!rewardGiven)
        {
            rewardGiven = true;
            ChapterTwoPuzzle.AddItemToInventory("Blue Key");
        }
    }

    private void RestartChallenge()
    {
        failed = false;
        won = false;
        showingResult = false;
        currentRound = 0;
        MovePlayer(GetArenaPlayerStartPosition(), Quaternion.identity);
        StartRound();
        ShowStartPrompt();
    }

    private void ExitChallenge()
    {
        active = false;
        failed = false;
        won = false;
        showingResult = false;
        waitingForRound = false;
        startPromptEndsAt = 0f;
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

    private IEnumerator PlayFallAnimation()
    {
        if (player == null)
        {
            showingResult = true;
            yield break;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
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

    private void OpenArena()
    {
        BuildMaterials();
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

    private void BuildGrid()
    {
        ClearTiles();
        BuildMaterials();

        float offset = (gridSize - 1) * tileSize * 0.5f;
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int colorIndex = Random.Range(0, TileColors.Length);
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "MagicCubeTile";
                tile.transform.SetParent(null, true);
                tile.transform.position = arenaCenter + new Vector3(x * tileSize - offset, 0f, z * tileSize - offset);
                tile.transform.rotation = Quaternion.identity;
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

    private void MovePlayer(Vector3 position, Quaternion rotation)
    {
        if (player == null)
        {
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
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

    private Vector3 GetArenaPlayerStartPosition()
    {
        Vector3 position = arenaCenter;
        position.y = arenaCenter.y + tileHeight * 0.5f + playerGroundOffset - GetControllerFootOffset();
        return position;
    }

    private float GetControllerFootOffset()
    {
        if (player == null)
        {
            return 0f;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            return 0f;
        }

        return controller.center.y - controller.height * 0.5f;
    }

    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect(roundPromptSize.x, roundPromptSize.y);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, roundPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void ShowStartPrompt()
    {
        startPromptEndsAt = Time.time + startPromptSeconds;
    }

    private void DrawStartPrompt()
    {
        Rect rect = GameUiStyle.SystemPromptRect(startPromptSize.x, startPromptSize.y);
        GameUiStyle.DrawDialoguePanel(rect);
        GUI.Label(rect, startPromptText, GameUiStyle.LabelStyle(ref startPromptStyle, startPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold, true));
    }

    private void DrawResultPanel(string title)
    {
        Rect rect = GameUiStyle.SystemPromptRect(resultPanelSize.x, resultPanelSize.y);
        GameUiStyle.DrawPanel(rect);
        GUI.Label(InnerRect(rect, resultTitleRect), title, GameUiStyle.LabelStyle(ref titleStyle, resultTitleFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));

        GUI.Label(InnerRect(rect, resultRestartRect), "Press A to restart", GameUiStyle.LabelStyle(ref promptStyle, resultPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));
        GUI.Label(InnerRect(rect, resultExitRect), "Press B to exit", GameUiStyle.LabelStyle(ref promptStyle, resultPromptFontSize, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private static Rect InnerRect(Rect parent, Rect localRect)
    {
        float y = localRect.y >= 0f ? parent.y + localRect.y : parent.yMax + localRect.y;
        float width = localRect.width >= 0f ? localRect.width : parent.width + localRect.width;
        float height = localRect.height >= 0f ? localRect.height : parent.height + localRect.height;
        return new Rect(parent.x + localRect.x, y, width, height);
    }

    private void BuildMaterials()
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

    private bool IsNearInteractObject()
    {
        return player != null && interactObject != null && Vector3.Distance(player.position, interactObject.position) <= interactDistance;
    }

}
