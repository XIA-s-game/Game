using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle : MonoBehaviour
{
    private const string SaveKeyPrefix = "ChapterOnePuzzle.";
    private const string SaveSceneName = "Enchanted Forest A";

    [System.Serializable]
    private class PushStep
    {
        public Transform block;
        public Transform marker;
        public Vector3 solvedLocalPosition;
    }

    [SerializeField] private string nextSceneName = "Fae Homes Demo";
    [SerializeField] private RuntimeAnimatorController heroWalkController;
    [SerializeField] private RuntimeAnimatorController heroAttackController;
    [SerializeField] private string heroWalkStateName = "mixamo_com";
    [SerializeField] private string heroAttackStateName = "mixamo_com";
    [SerializeField] private float heroMoveSpeed = 5f;
    [SerializeField] private float heroTurnSpeed = 540f;
    [SerializeField] private float heroAttackDistance = 2.1f;
    [SerializeField] private float heroAttackHitDelay = 0.85f;
    [SerializeField] private float heroInteractDistance = 4f;
    [SerializeField] private float portalInteractDistance = 3f;
    [SerializeField] private PushStep[] pushSteps;
    [SerializeField] private int requiredOrderedPushCount = 6;
    [SerializeField] private float playerPushDistance = 10f;
    [SerializeField] private float markerReachDistance = 1.6f;
    [SerializeField] private float pushSpeed = 3.5f;
    [SerializeField] private float solvedDistance = 0.03f;
    [SerializeField] private string pushPrompt = "Press E to push";
    [SerializeField] private string failurePrompt = "Puzzle failed";
    [SerializeField] private string successPrompt = "Puzzle solved";
    [SerializeField] private string recognizeHelpPrompt = "Someone is calling for help. Go check it out.";
    [SerializeField] private string askHelpPrompt = "Press E to ask";
    [SerializeField] private string[] helpDialogueLines =
    {
        "Fairy: Please help me. Dark magic trapped me here.",
        "Player: How can I help?",
        "Fairy: Push the stone buttons in the right order to break the spell.",
        "Player: I saw strange marks nearby. They may be the clue.",
        "Fairy: Yes. The clues are hidden around the forest.",
        "Player: I will do my best."
    };
    [SerializeField] private string[] clueDialogueLines =
    {
        "Fairy: The code has six steps. Keep looking for clues."
    };
    [SerializeField] private string[] pageRewardDialogueLines =
    {
        "Fairy: Thank you for saving me. Take this first magic page.",
        "Player: Thank you."
    };
    [SerializeField] private string[] forestAttackDialogueLines =
    {
        "Fairy: What was that sound?",
        "Fairy: The Dark King's monsters are attacking the forest.",
        "Fairy: Please be careful and take a look first.",
        "Fairy: Stay safe."
    };
    [SerializeField] private string[] heroWarningDialogueLines =
    {
        "Hero: Stay back. These monsters are dangerous."
    };
    [SerializeField] private string[] heroAfterCombatDialogueLines =
    {
        "Player: You are strong. You do not look like you are from here.",
        "Hero: I was sent to protect the forest. The Dark King is getting stronger.",
        "Player: Do you know where I can find the second magic page?",
        "Hero: I know a place. I will open a portal for you."
    };
    [SerializeField] private string firstPageItemName = "First Page";
    [SerializeField] private string heroInteractPrompt = "Press E to talk";
    [SerializeField] private string portalInteractPrompt = "Press E to travel";
    [SerializeField] private Vector3 cageChildSolvedLocalPosition = new Vector3(0f, -0.99f, 0f);
    [SerializeField] private Vector3 fairySolvedWorldPosition = new Vector3(559.99f, 16.86f, 579.14f);
    [SerializeField] private Vector3 fairySolvedEulerOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private float resultRotationSpeed = 90f;
    [SerializeField] private float storyAreaReachDistance = 2f;
    [SerializeField] private float enemyTriggerDistance = 6f;

    private readonly List<Transform> pushBlocks = new List<Transform>();
    private readonly List<Transform> pushMarkers = new List<Transform>();
    private readonly HashSet<Transform> colliderReadyTargets = new HashSet<Transform>();
    private Vector3[] runtimeSolvedLocalPositions;
    private Vector3[] initialLocalPositions;
    private bool[] completedPushes;
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform puzzleRoot;
    [SerializeField] private Transform center;
    [SerializeField] private Transform recognizeHelp;
    [SerializeField] private Transform askHelp;
    [SerializeField] private Transform cage;
    [SerializeField] private Transform fairy;
    [SerializeField] private Transform enemyTrigger;
    [SerializeField] private Transform hero;
    [SerializeField] private Transform portalTrigger;
    [SerializeField] private GameObject portalDoor;
    [SerializeField] private GameObject[] delayedEnemyObjects;
    private Animator heroAnimator;
    [SerializeField] private Transform redIndicator;
    [SerializeField] private Transform greenIndicator;
    private readonly List<GameObject> delayedEnemies = new List<GameObject>();
    private readonly List<Renderer> delayedEnemyRenderers = new List<Renderer>();
    private readonly List<Collider> delayedEnemyColliders = new List<Collider>();
    private readonly List<RouteWaypointWalker> delayedEnemyWalkers = new List<RouteWaypointWalker>();
    private readonly List<Animator> delayedEnemyAnimators = new List<Animator>();
    private int currentIndex;
    private bool referencesReady;
    private bool promptVisible;
    private int movingBlockIndex = -1;
    private bool movingWrongBlock;
    private string resultPrompt;
    private float resultPromptEndsAt;
    private string storyPrompt;
    private float storyPromptEndsAt;
    private bool recognizeHelpShown;
    private bool askHelpPromptVisible;
    private bool helpDialogueActive;
    private int helpDialogueIndex;
    private string[] activeDialogueLines;
    private KeyCode activeDialogueContinueKey = KeyCode.C;
    private string activeDialogueContinueHint = "Press C to continue";
    private bool initialHelpDialogueFinished;
    private bool rescueApplied;
    private bool pageRewardFinished;
    private bool forestAttackDialogueFinished;
    private bool enemiesPrepared;
    private bool enemiesActivated;
    private bool heroCombatActive;
    private bool heroAttacking;
    private bool heroWarningShown;
    private bool heroCombatFinished;
    private bool heroPostCombatDialogueFinished;
    private bool portalUnlocked;
    private bool heroPromptVisible;
    private bool portalPromptVisible;
    private bool firstPageAddedToBackpack;
    private float heroAttackHitsAt;
    private float heroCombatY;
    private GameObject heroTargetEnemy;
    private readonly HashSet<GameObject> defeatedEnemies = new HashSet<GameObject>();

    private void Awake()
    {
        RefreshReferences();
        LoadPersistentState();
    }

    private void Start()
    {
        RefreshReferences();
        if (!rescueApplied)
        {
            SetAudioSourcesPlayingInHierarchy(cage, true);
        }

        ApplyRuntimeStateToScene();
    }

    private void OnDisable()
    {
        StopAudioSourcesInHierarchy(cage);
        GameAudioManager.StopRoarLoop();
        if (enemiesActivated && !heroCombatFinished)
        {
            GameAudioManager.StopEnemyLoop();
        }
    }

    private void Update()
    {
        RotateResultIndicators();

        if (!referencesReady)
        {
            RefreshReferences();
        }

        if (!referencesReady)
        {
            return;
        }

        UpdateHelpStory();
        UpdateEnemyAmbush();
        UpdateHeroCombat();
        UpdateHeroStory();
        UpdatePortalInteraction();

        if (movingBlockIndex >= 0)
        {
            MoveActiveBlock();
            return;
        }

        if (currentIndex >= requiredOrderedPushCount)
        {
            promptVisible = false;
            return;
        }

        int hoveredIndex = GetHoveredPushIndex();
        promptVisible = hoveredIndex >= 0;

        if (!promptVisible || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        StartPushingBlock(hoveredIndex);
    }

    private void RefreshReferences()
    {
        if (hero != null && heroAnimator == null)
        {
            heroAnimator = hero.GetComponentInChildren<Animator>(true);
        }
        if (!forestAttackDialogueFinished && !enemiesActivated)
        {
            SetHeroVisible(false);
        }

        if (!portalUnlocked)
        {
            SetPortalVisible(false);
        }

        if (!rescueApplied)
        {
            SetAudioSourcesPlayingInHierarchy(cage, true);
        }

        SetIndicatorVisible(redIndicator, false);
        SetIndicatorVisible(greenIndicator, false);
        PrepareDelayedEnemies();
        if (center == null)
        {
            center = puzzleRoot;
        }

        RefreshPushReferences();

        BuildRuntimeSolvedLocalPositions();
        EnsureSolidCollider(center);
        int expectedPushCount = pushSteps != null ? pushSteps.Length : 0;
        referencesReady = player != null && puzzleRoot != null && center != null && pushBlocks.Count == expectedPushCount;
    }

    private static float GetHorizontalDistanceToObject(Vector3 flatPoint, Transform target)
    {
        if (TryGetWorldBounds(target, out Bounds bounds))
        {
            Vector3 closest = bounds.ClosestPoint(new Vector3(flatPoint.x, bounds.center.y, flatPoint.z));
            return Vector3.Distance(flatPoint, Flatten(closest));
        }

        return Vector3.Distance(flatPoint, Flatten(target.position));
    }

    public static void SavePersistentStateForActiveScene()
    {
        if (SceneManager.GetActiveScene().name != SaveSceneName)
        {
            return;
        }

        ChapterOnePuzzle instance = FindObjectOfType<ChapterOnePuzzle>();
        if (instance == null)
        {
            return;
        }

        instance.SavePersistentState();
    }

    public static void ClearPersistentState()
    {
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "CurrentIndex");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "RecognizeHelpShown");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "InitialHelpDialogueFinished");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "RescueApplied");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "PageRewardFinished");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "ForestAttackDialogueFinished");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "EnemiesActivated");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "HeroWarningShown");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "HeroCombatFinished");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "HeroPostCombatDialogueFinished");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "PortalUnlocked");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "FirstPageAddedToBackpack");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "CompletedPushes");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "BlockPositions");
        PlayerPrefs.DeleteKey(SaveKeyPrefix + "EnemyActiveStates");
    }

    private void SavePersistentState()
    {
        PlayerPrefs.SetInt(SaveKeyPrefix + "CurrentIndex", currentIndex);
        PlayerPrefs.SetInt(SaveKeyPrefix + "RecognizeHelpShown", recognizeHelpShown ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "InitialHelpDialogueFinished", initialHelpDialogueFinished ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "RescueApplied", rescueApplied ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "PageRewardFinished", pageRewardFinished ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "ForestAttackDialogueFinished", forestAttackDialogueFinished ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "EnemiesActivated", enemiesActivated ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "HeroWarningShown", heroWarningShown ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "HeroCombatFinished", heroCombatFinished ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "HeroPostCombatDialogueFinished", heroPostCombatDialogueFinished ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "PortalUnlocked", portalUnlocked ? 1 : 0);
        PlayerPrefs.SetInt(SaveKeyPrefix + "FirstPageAddedToBackpack", firstPageAddedToBackpack ? 1 : 0);
        PlayerPrefs.SetString(SaveKeyPrefix + "CompletedPushes", SerializeBoolArray(completedPushes));
        PlayerPrefs.SetString(SaveKeyPrefix + "BlockPositions", SerializeBlockPositions());
        PlayerPrefs.SetString(SaveKeyPrefix + "EnemyActiveStates", SerializeEnemyStates());
    }

    private void LoadPersistentState()
    {
        currentIndex = PlayerPrefs.GetInt(SaveKeyPrefix + "CurrentIndex", currentIndex);
        recognizeHelpShown = PlayerPrefs.GetInt(SaveKeyPrefix + "RecognizeHelpShown", recognizeHelpShown ? 1 : 0) == 1;
        initialHelpDialogueFinished = PlayerPrefs.GetInt(SaveKeyPrefix + "InitialHelpDialogueFinished", initialHelpDialogueFinished ? 1 : 0) == 1;
        rescueApplied = PlayerPrefs.GetInt(SaveKeyPrefix + "RescueApplied", rescueApplied ? 1 : 0) == 1;
        pageRewardFinished = PlayerPrefs.GetInt(SaveKeyPrefix + "PageRewardFinished", pageRewardFinished ? 1 : 0) == 1;
        forestAttackDialogueFinished = PlayerPrefs.GetInt(SaveKeyPrefix + "ForestAttackDialogueFinished", forestAttackDialogueFinished ? 1 : 0) == 1;
        enemiesActivated = PlayerPrefs.GetInt(SaveKeyPrefix + "EnemiesActivated", enemiesActivated ? 1 : 0) == 1;
        heroWarningShown = PlayerPrefs.GetInt(SaveKeyPrefix + "HeroWarningShown", heroWarningShown ? 1 : 0) == 1;
        heroCombatFinished = PlayerPrefs.GetInt(SaveKeyPrefix + "HeroCombatFinished", heroCombatFinished ? 1 : 0) == 1;
        heroPostCombatDialogueFinished = PlayerPrefs.GetInt(SaveKeyPrefix + "HeroPostCombatDialogueFinished", heroPostCombatDialogueFinished ? 1 : 0) == 1;
        portalUnlocked = PlayerPrefs.GetInt(SaveKeyPrefix + "PortalUnlocked", portalUnlocked ? 1 : 0) == 1;
        firstPageAddedToBackpack = PlayerPrefs.GetInt(SaveKeyPrefix + "FirstPageAddedToBackpack", firstPageAddedToBackpack ? 1 : 0) == 1;
    }

    private void ApplyRuntimeStateToScene()
    {
        ApplySavedPushState();

        if (rescueApplied)
        {
            ApplyRescueSceneState();
            SetIndicatorVisible(greenIndicator, currentIndex >= requiredOrderedPushCount);
            SetIndicatorVisible(redIndicator, false);
        }

        if (forestAttackDialogueFinished && fairy != null)
        {
            fairy.gameObject.SetActive(false);
        }

        if (enemiesActivated)
        {
            if (!heroCombatFinished)
            {
                GameAudioManager.StartRoarLoop();
            }

            ApplyEnemySceneState();
        }
        else if (!forestAttackDialogueFinished)
        {
            SetHeroVisible(false);
        }

        if (heroCombatFinished && hero != null)
        {
            SetHeroVisible(true);
        }

        if (portalUnlocked)
        {
            SetPortalVisible(true);
        }
    }

    private void ApplySavedPushState()
    {
        if (pushBlocks.Count == 0)
        {
            return;
        }

        string completedPushesValue = PlayerPrefs.GetString(SaveKeyPrefix + "CompletedPushes", string.Empty);
        string blockPositionsValue = PlayerPrefs.GetString(SaveKeyPrefix + "BlockPositions", string.Empty);

        if (!string.IsNullOrEmpty(completedPushesValue))
        {
            ApplySerializedBoolArray(completedPushesValue);
        }

        if (!string.IsNullOrEmpty(blockPositionsValue))
        {
            ApplySerializedBlockPositions(blockPositionsValue);
        }
    }

    private string SerializeBoolArray(bool[] values)
    {
        if (values == null || values.Length == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            parts[i] = values[i] ? "1" : "0";
        }

        return string.Join(",", parts);
    }

    private void ApplySerializedBoolArray(string serialized)
    {
        if (completedPushes == null || string.IsNullOrEmpty(serialized))
        {
            return;
        }

        string[] parts = serialized.Split(',');
        for (int i = 0; i < completedPushes.Length && i < parts.Length; i++)
        {
            completedPushes[i] = parts[i] == "1";
        }
    }

    private string SerializeBlockPositions()
    {
        if (pushBlocks.Count == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[pushBlocks.Count];
        for (int i = 0; i < pushBlocks.Count; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null)
            {
                parts[i] = string.Empty;
                continue;
            }

            Vector3 position = block.localPosition;
            parts[i] = position.x + "|" + position.y + "|" + position.z;
        }

        return string.Join(";", parts);
    }

    private void ApplySerializedBlockPositions(string serialized)
    {
        if (string.IsNullOrEmpty(serialized))
        {
            return;
        }

        string[] blockEntries = serialized.Split(';');
        for (int i = 0; i < pushBlocks.Count && i < blockEntries.Length; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null || string.IsNullOrEmpty(blockEntries[i]))
            {
                continue;
            }

            string[] parts = blockEntries[i].Split('|');
            if (parts.Length != 3)
            {
                continue;
            }

            float x;
            float y;
            float z;
            if (!float.TryParse(parts[0], out x) ||
                !float.TryParse(parts[1], out y) ||
                !float.TryParse(parts[2], out z))
            {
                continue;
            }

            block.localPosition = new Vector3(x, y, z);
        }
    }

    private string SerializeEnemyStates()
    {
        if (delayedEnemyObjects == null || delayedEnemyObjects.Length == 0)
        {
            return string.Empty;
        }

        string[] parts = new string[delayedEnemyObjects.Length];
        for (int i = 0; i < delayedEnemyObjects.Length; i++)
        {
            GameObject enemy = delayedEnemyObjects[i];
            parts[i] = enemy != null && enemy.activeSelf ? "1" : "0";
        }

        return string.Join(",", parts);
    }

    private void ApplyRescueSceneState()
    {
        if (cage != null && cage.childCount > 0)
        {
            cage.GetChild(0).localPosition = cageChildSolvedLocalPosition;
            StopAudioSourcesInHierarchy(cage);
        }

        if (fairy != null)
        {
            fairy.position = fairySolvedWorldPosition;
            fairy.rotation *= Quaternion.Euler(fairySolvedEulerOffset);
        }
    }

    private void ApplyEnemySceneState()
    {
        PrepareDelayedEnemies();

        string savedEnemyStates = PlayerPrefs.GetString(SaveKeyPrefix + "EnemyActiveStates", string.Empty);
        string[] enemyStateParts = string.IsNullOrEmpty(savedEnemyStates) ? null : savedEnemyStates.Split(',');
        bool anyEnemyActive = false;

        for (int i = 0; i < delayedEnemies.Count; i++)
        {
            GameObject enemy = delayedEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            bool shouldBeActive = enemyStateParts == null || i >= enemyStateParts.Length || enemyStateParts[i] == "1";
            enemy.SetActive(shouldBeActive);
            if (!shouldBeActive)
            {
                continue;
            }

            anyEnemyActive = true;
            SetAudioSourcesPlayingInHierarchy(enemy.transform, true);
        }

        for (int i = 0; i < delayedEnemyRenderers.Count; i++)
        {
            if (delayedEnemyRenderers[i] != null)
            {
                delayedEnemyRenderers[i].enabled = delayedEnemyRenderers[i].gameObject.activeInHierarchy;
            }
        }

        for (int i = 0; i < delayedEnemyColliders.Count; i++)
        {
            if (delayedEnemyColliders[i] != null)
            {
                delayedEnemyColliders[i].enabled = delayedEnemyColliders[i].gameObject.activeInHierarchy;
            }
        }

        for (int i = 0; i < delayedEnemyWalkers.Count; i++)
        {
            if (delayedEnemyWalkers[i] != null)
            {
                delayedEnemyWalkers[i].enabled = delayedEnemyWalkers[i].gameObject.activeInHierarchy;
            }
        }

        for (int i = 0; i < delayedEnemyAnimators.Count; i++)
        {
            if (delayedEnemyAnimators[i] != null)
            {
                delayedEnemyAnimators[i].enabled = delayedEnemyAnimators[i].gameObject.activeInHierarchy;
            }
        }

        if (hero == null)
        {
            return;
        }

        SetHeroVisible(true);
        heroCombatY = hero.position.y;

        if (heroCombatFinished)
        {
            heroCombatActive = false;
            heroAttacking = false;
            if (heroAnimator != null)
            {
                heroAnimator.speed = 0f;
            }

            GameAudioManager.StopEnemyLoop();
            return;
        }

        if (!anyEnemyActive)
        {
            GameAudioManager.StopEnemyLoop();
            FinishHeroCombat();
            return;
        }

        GameAudioManager.StartEnemyLoop();
        heroCombatActive = true;
        heroAttacking = false;
        heroTargetEnemy = FindNextHeroTarget();
        if (heroTargetEnemy != null)
        {
            PlayHeroAnimation(heroWalkController, heroWalkStateName);
        }
    }
}
