using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterOnePuzzle : MonoBehaviour
{
    [System.Serializable]
    private class PushStep
    {
        public Transform block;
        public Transform marker;
        public Vector3 solvedLocalPosition;
    }

    [SerializeField] private bool requireForestAttackDialogueBeforeEnemies = true;
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

    private void OnGUI()
    {
        if (helpDialogueActive)
        {
            DrawDialogueBox();
            return;
        }

        if (heroPromptVisible)
        {
            DrawPromptBox(heroInteractPrompt, Screen.height * 0.72f);
        }

        if (portalPromptVisible)
        {
            DrawPromptBox(portalInteractPrompt, Screen.height * 0.72f);
        }

        if (askHelpPromptVisible)
        {
            DrawPromptBox(askHelpPrompt, Screen.height * 0.72f);
        }

        if (!string.IsNullOrEmpty(storyPrompt) && Time.time < storyPromptEndsAt)
        {
            DrawPromptBox(storyPrompt, 36f);
        }

        bool hasResultPrompt = !string.IsNullOrEmpty(resultPrompt) && Time.time < resultPromptEndsAt;
        if ((!promptVisible || string.IsNullOrEmpty(pushPrompt)) && !hasResultPrompt)
        {
            return;
        }

        DrawPromptBox(hasResultPrompt ? resultPrompt : pushPrompt, Screen.height * 0.72f);
    }

    private void DrawPromptBox(string text, float y)
    {
        Rect rect = y <= 60f
            ? GameUiStyle.SystemPromptRect(760f, 92f)
            : GameUiStyle.InteractionPromptRect(520f, 64f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = y <= 60f ? 30 : 28
        };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, rect.height - 16f), text, style);
    }

    private void DrawDialogueBox()
    {
        Rect rect = GameUiStyle.DialogueRect(220f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 30,
            wordWrap = true
        };
        textStyle.normal.textColor = Color.white;

        string line = activeDialogueLines != null && helpDialogueIndex >= 0 && helpDialogueIndex < activeDialogueLines.Length
            ? activeDialogueLines[helpDialogueIndex]
            : string.Empty;
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 148f), line, textStyle);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = 22
        };
        hintStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 24f, rect.y + rect.height - 42f, rect.width - 48f, 24f), activeDialogueContinueHint, hintStyle);
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

    private void StartPushingBlock(int index)
    {
        if (index < 0 || index >= pushBlocks.Count || pushBlocks[index] == null)
        {
            return;
        }

        SetIndicatorVisible(redIndicator, false);
        resultPrompt = null;
        movingBlockIndex = index;
        movingWrongBlock = index != currentIndex || index >= requiredOrderedPushCount;
        promptVisible = false;
    }

    private void UpdateHelpStory()
    {
        if (helpDialogueActive)
        {
            if (Input.GetKeyDown(activeDialogueContinueKey))
            {
                helpDialogueIndex++;
                if (activeDialogueLines == null || helpDialogueIndex >= activeDialogueLines.Length)
                {
                    FinishActiveDialogue();
                }
            }

            return;
        }

        if (!recognizeHelpShown && IsPlayerNearTransform(recognizeHelp, storyAreaReachDistance))
        {
            recognizeHelpShown = true;
            storyPrompt = recognizeHelpPrompt;
            storyPromptEndsAt = Time.time + 3f;
        }

        Transform interactionTarget = rescueApplied ? fairy : askHelp;
        askHelpPromptVisible = IsPlayerNearTransform(interactionTarget, storyAreaReachDistance) &&
            !forestAttackDialogueFinished &&
            (!rescueApplied || !pageRewardFinished);
        if (askHelpPromptVisible && Input.GetKeyDown(KeyCode.E))
        {
            if (rescueApplied && !pageRewardFinished)
            {
                StartDialogue(pageRewardDialogueLines, KeyCode.E, "Press E to continue");
            }
            else if (initialHelpDialogueFinished)
            {
                StartDialogue(clueDialogueLines, KeyCode.C, "Press C to continue");
            }
            else
            {
                StartDialogue(helpDialogueLines, KeyCode.C, "Press C to continue");
            }
        }
    }

    private void StartDialogue(string[] lines, KeyCode continueKey, string continueHint)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        activeDialogueLines = lines;
        activeDialogueContinueKey = continueKey;
        activeDialogueContinueHint = continueHint;
        helpDialogueActive = true;
        askHelpPromptVisible = false;
        helpDialogueIndex = 0;
    }

    private void FinishActiveDialogue()
    {
        string[] finishedLines = activeDialogueLines;
        helpDialogueActive = false;
        helpDialogueIndex = 0;

        if (finishedLines == helpDialogueLines)
        {
            initialHelpDialogueFinished = true;
        }
        else if (finishedLines == pageRewardDialogueLines)
        {
            pageRewardFinished = true;
            AddFirstPageToBackpack();
            StartDialogue(forestAttackDialogueLines, KeyCode.C, "Press C to continue");
        }
        else if (finishedLines == forestAttackDialogueLines)
        {
            forestAttackDialogueFinished = true;
            if (fairy != null)
            {
                fairy.gameObject.SetActive(false);
            }
        }
        else if (finishedLines == heroAfterCombatDialogueLines)
        {
            heroPostCombatDialogueFinished = true;
            UnlockPortal();
        }
    }

    private void AddFirstPageToBackpack()
    {
        if (firstPageAddedToBackpack)
        {
            return;
        }

        firstPageAddedToBackpack = true;
        GlobalBackpackUI.AddItem(firstPageItemName);
    }

    private void MoveActiveBlock()
    {
        if (movingBlockIndex < 0 || movingBlockIndex >= pushBlocks.Count || pushBlocks[movingBlockIndex] == null)
        {
            movingBlockIndex = -1;
            movingWrongBlock = false;
            return;
        }

        bool arrived = MoveBlockTowardLocalTarget(pushBlocks[movingBlockIndex], GetSolvedLocalPosition(pushBlocks[movingBlockIndex], movingBlockIndex));
        if (!arrived)
        {
            return;
        }

        if (movingWrongBlock)
        {
            FailAndReset();
            return;
        }

        completedPushes[movingBlockIndex] = true;
        currentIndex++;
        movingBlockIndex = -1;
        movingWrongBlock = false;

        if (currentIndex >= requiredOrderedPushCount)
        {
            ShowResult(successPrompt);
            SetIndicatorVisible(greenIndicator, true);
            ApplyRescueResult();
        }
    }

    private void ApplyRescueResult()
    {
        if (rescueApplied)
        {
            return;
        }

        rescueApplied = true;
        if (cage != null && cage.childCount > 0)
        {
            cage.GetChild(0).localPosition = cageChildSolvedLocalPosition;
        }

        if (fairy != null)
        {
            fairy.position = fairySolvedWorldPosition;
            fairy.rotation *= Quaternion.Euler(fairySolvedEulerOffset);
        }
    }

    private void PrepareDelayedEnemies()
    {
        if (enemiesPrepared)
        {
            return;
        }

        delayedEnemies.Clear();
        delayedEnemyRenderers.Clear();
        delayedEnemyColliders.Clear();
        delayedEnemyWalkers.Clear();
        delayedEnemyAnimators.Clear();
        if (delayedEnemyObjects == null)
        {
            enemiesPrepared = true;
            return;
        }

        for (int i = 0; i < delayedEnemyObjects.Length; i++)
        {
            GameObject enemy = delayedEnemyObjects[i];
            if (enemy == null || delayedEnemies.Contains(enemy))
            {
                continue;
            }

            delayedEnemies.Add(enemy);
            HideDelayedEnemy(enemy);
        }

        enemiesPrepared = true;
    }

    private void HideDelayedEnemy(GameObject enemy)
    {
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                delayedEnemyRenderers.Add(renderer);
                renderer.enabled = false;
            }
        }

        Collider[] colliders = enemy.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null && collider.enabled)
            {
                delayedEnemyColliders.Add(collider);
                collider.enabled = false;
            }
        }

        RouteWaypointWalker[] walkers = enemy.GetComponentsInChildren<RouteWaypointWalker>(true);
        foreach (RouteWaypointWalker walker in walkers)
        {
            if (walker != null && walker.enabled)
            {
                delayedEnemyWalkers.Add(walker);
                walker.enabled = false;
            }
        }

        Animator[] animators = enemy.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.enabled)
            {
                delayedEnemyAnimators.Add(animator);
                animator.enabled = false;
            }
        }
    }

    private void UpdateEnemyAmbush()
    {
        if (enemiesActivated || !forestAttackDialogueFinished)
        {
            return;
        }

        if (enemyTrigger == null || !IsPlayerOnTrigger(enemyTrigger, enemyTriggerDistance))
        {
            return;
        }

        ActivateDelayedEnemies();
    }

    private void ActivateDelayedEnemies()
    {
        PrepareDelayedEnemies();
        enemiesActivated = true;

        for (int i = 0; i < delayedEnemies.Count; i++)
        {
            GameObject enemy = delayedEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            RouteWaypointWalker walker = enemy.GetComponent<RouteWaypointWalker>();
            if (walker == null)
            {
                walker = enemy.AddComponent<RouteWaypointWalker>();
            }

            if (!delayedEnemyWalkers.Contains(walker))
            {
                delayedEnemyWalkers.Add(walker);
            }
        }

        for (int i = 0; i < delayedEnemyRenderers.Count; i++)
        {
            if (delayedEnemyRenderers[i] != null)
            {
                delayedEnemyRenderers[i].enabled = true;
            }
        }

        for (int i = 0; i < delayedEnemyColliders.Count; i++)
        {
            if (delayedEnemyColliders[i] != null)
            {
                delayedEnemyColliders[i].enabled = true;
            }
        }

        for (int i = 0; i < delayedEnemyWalkers.Count; i++)
        {
            if (delayedEnemyWalkers[i] != null)
            {
                delayedEnemyWalkers[i].enabled = true;
            }
        }

        for (int i = 0; i < delayedEnemyAnimators.Count; i++)
        {
            if (delayedEnemyAnimators[i] != null)
            {
                delayedEnemyAnimators[i].enabled = true;
            }
        }

        StartHeroCombat();
    }

    private void StartHeroCombat()
    {
        if (hero == null)
        {
            return;
        }

        SetHeroVisible(true);

        if (heroAnimator == null)
        {
            heroAnimator = hero.GetComponentInChildren<Animator>(true);
        }

        if (heroAnimator != null)
        {
            heroAnimator.applyRootMotion = false;
        }

        heroCombatY = hero.position.y;
        if (heroAnimator != null)
        {
            heroAnimator.speed = 1f;
        }

        heroCombatActive = true;
        heroCombatFinished = false;
        heroAttacking = false;
        defeatedEnemies.Clear();
        heroTargetEnemy = FindNextHeroTarget();
        PlayHeroAnimation(heroWalkController, heroWalkStateName);
    }

    private void SetHeroVisible(bool visible)
    {
        if (hero == null)
        {
            return;
        }

        if (heroAnimator == null)
        {
            heroAnimator = hero.GetComponentInChildren<Animator>(true);
        }

        if (heroAnimator != null)
        {
            heroAnimator.enabled = visible;
        }

        Renderer[] renderers = hero.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }

        Collider[] colliders = hero.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = visible;
            }
        }
    }

    private void FinishHeroCombat()
    {
        heroCombatActive = false;
        heroAttacking = false;
        heroCombatFinished = true;
        heroPromptVisible = false;

        if (heroAnimator != null)
        {
            heroAnimator.speed = 0f;
        }
    }

    private void UpdateHeroStory()
    {
        heroPromptVisible = false;
        if (helpDialogueActive || hero == null || !enemiesActivated)
        {
            return;
        }

        if (heroCombatActive)
        {
            if (!heroWarningShown && IsPlayerNearTransform(hero, heroInteractDistance))
            {
                heroWarningShown = true;
                StartDialogue(heroWarningDialogueLines, KeyCode.C, "Press C to continue");
            }

            return;
        }

        if (!heroCombatFinished || heroPostCombatDialogueFinished || !IsPlayerNearTransform(hero, heroInteractDistance))
        {
            return;
        }

        heroPromptVisible = true;
        if (Input.GetKeyDown(KeyCode.E))
        {
            heroPromptVisible = false;
            StartDialogue(heroAfterCombatDialogueLines, KeyCode.C, "Press C to continue");
        }
    }

    private void UnlockPortal()
    {
        portalUnlocked = true;
        SetPortalVisible(true);
    }

    private void UpdatePortalInteraction()
    {
        portalPromptVisible = false;
        if (!portalUnlocked || helpDialogueActive)
        {
            return;
        }

        SetPortalVisible(true);

        if (!IsPlayerOnTrigger(portalTrigger, portalInteractDistance))
        {
            return;
        }

        portalPromptVisible = true;
        if (Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void SetPortalVisible(bool visible)
    {
        if (portalTrigger != null && portalTrigger.gameObject.activeSelf != visible)
        {
            portalTrigger.gameObject.SetActive(visible);
        }

        if (portalDoor != null && portalDoor.activeSelf != visible)
        {
            portalDoor.SetActive(visible);
        }
    }

    private void UpdateHeroCombat()
    {
        if (!heroCombatActive || hero == null)
        {
            return;
        }

        KeepHeroAtCombatHeight();

        if (heroTargetEnemy == null || !heroTargetEnemy.activeInHierarchy)
        {
            heroTargetEnemy = FindNextHeroTarget();
            if (heroTargetEnemy == null)
            {
                FinishHeroCombat();
                return;
            }

            PlayHeroAnimation(heroWalkController, heroWalkStateName);
        }

        if (heroAttacking)
        {
            FaceHeroToward(heroTargetEnemy.transform.position);
            if (Time.time >= heroAttackHitsAt)
            {
                DefeatEnemy(heroTargetEnemy);
                heroTargetEnemy = FindNextHeroTarget();
                heroAttacking = false;

                if (heroTargetEnemy == null)
                {
                    KeepHeroAtCombatHeight();
                    FinishHeroCombat();
                    return;
                }

                PlayHeroAnimation(heroWalkController, heroWalkStateName);
            }

            return;
        }

        Vector3 targetPosition = heroTargetEnemy.transform.position;
        Vector3 toTarget = targetPosition - hero.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= heroAttackDistance)
        {
            heroAttacking = true;
            heroAttackHitsAt = Time.time + heroAttackHitDelay;
            FaceHeroToward(targetPosition);
            PlayHeroAnimation(heroAttackController, heroAttackStateName);
            return;
        }

        FaceHeroToward(targetPosition);
        Vector3 moveTarget = new Vector3(targetPosition.x, heroCombatY, targetPosition.z);
        hero.position = Vector3.MoveTowards(hero.position, moveTarget, heroMoveSpeed * Time.deltaTime);
        KeepHeroAtCombatHeight();
    }

    private void KeepHeroAtCombatHeight()
    {
        Vector3 position = hero.position;
        if (Mathf.Abs(position.y - heroCombatY) <= 0.001f)
        {
            return;
        }

        position.y = heroCombatY;
        hero.position = position;
    }

    private GameObject FindNextHeroTarget()
    {
        GameObject bestEnemy = null;
        float bestSqrDistance = float.PositiveInfinity;
        Vector3 heroPosition = hero != null ? hero.position : Vector3.zero;

        for (int i = 0; i < delayedEnemies.Count; i++)
        {
            GameObject enemy = delayedEnemies[i];
            if (enemy == null || defeatedEnemies.Contains(enemy) || !enemy.activeInHierarchy)
            {
                continue;
            }

            float sqrDistance = (enemy.transform.position - heroPosition).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private void FaceHeroToward(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - hero.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        hero.rotation = Quaternion.RotateTowards(hero.rotation, targetRotation, heroTurnSpeed * Time.deltaTime);
    }

    private void DefeatEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        defeatedEnemies.Add(enemy);
        RouteWaypointWalker walker = enemy.GetComponent<RouteWaypointWalker>();
        if (walker != null)
        {
            walker.enabled = false;
        }

        enemy.SetActive(false);
    }

    private void PlayHeroAnimation(RuntimeAnimatorController controller, string stateName)
    {
        if (heroAnimator == null)
        {
            return;
        }

        if (controller != null)
        {
            heroAnimator.runtimeAnimatorController = controller;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int fullStateHash = Animator.StringToHash("Base Layer." + stateName);
        int shortStateHash = Animator.StringToHash(stateName);
        if (heroAnimator.HasState(0, fullStateHash))
        {
            heroAnimator.CrossFade(fullStateHash, 0.08f);
        }
        else if (heroAnimator.HasState(0, shortStateHash))
        {
            heroAnimator.CrossFade(shortStateHash, 0.08f);
        }
    }

    private int GetHoveredPushIndex()
    {
        for (int i = 0; i < pushMarkers.Count; i++)
        {
            if (completedPushes != null && i < completedPushes.Length && completedPushes[i])
            {
                continue;
            }

            if (IsPlayerInPushArea(i))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsPlayerInPushArea(int index)
    {
        Vector3 playerPosition = Flatten(player.position);
        Transform marker = index >= 0 && index < pushMarkers.Count ? pushMarkers[index] : null;
        if (marker != null)
        {
            float markerDistance = GetHorizontalDistanceToObject(playerPosition, marker);
            if (markerDistance > markerReachDistance)
            {
                return false;
            }
        }
        else
        {
            Transform block = index >= 0 && index < pushBlocks.Count ? pushBlocks[index] : null;
            if (block == null)
            {
                return false;
            }

            float distance = GetHorizontalDistanceToObject(playerPosition, block);
            if (distance > playerPushDistance)
            {
                return false;
            }
        }

        return true;
    }

    private Transform GetCurrentPushMarker()
    {
        return currentIndex >= 0 && currentIndex < pushMarkers.Count ? pushMarkers[currentIndex] : null;
    }

    private void RefreshPushReferences()
    {
        pushBlocks.Clear();
        pushMarkers.Clear();

        if (pushSteps == null)
        {
            return;
        }

        for (int i = 0; i < pushSteps.Length; i++)
        {
            PushStep step = pushSteps[i];
            if (step == null || step.block == null)
            {
                continue;
            }

            pushBlocks.Add(step.block);
            pushMarkers.Add(step.marker);
            EnsureSolidCollider(step.block);
        }
    }

    private bool MoveBlockTowardLocalTarget(Transform block, Vector3 targetLocalPosition)
    {
        block.localPosition = Vector3.MoveTowards(
            block.localPosition,
            targetLocalPosition,
            pushSpeed * Time.deltaTime);

        if (Vector3.Distance(block.localPosition, targetLocalPosition) <= solvedDistance)
        {
            block.localPosition = targetLocalPosition;
            return true;
        }

        return false;
    }

    private Vector3 GetSolvedLocalPosition(Transform block, int index)
    {
        if (runtimeSolvedLocalPositions != null &&
            index >= 0 &&
            index < runtimeSolvedLocalPositions.Length)
        {
            return runtimeSolvedLocalPositions[index];
        }

        return block.localPosition;
    }

    private void BuildRuntimeSolvedLocalPositions()
    {
        runtimeSolvedLocalPositions = new Vector3[pushBlocks.Count];
        initialLocalPositions = new Vector3[pushBlocks.Count];
        completedPushes = new bool[pushBlocks.Count];

        for (int i = 0; i < pushBlocks.Count; i++)
        {
            Transform block = pushBlocks[i];
            if (block == null)
            {
                continue;
            }

            initialLocalPositions[i] = block.localPosition;
            runtimeSolvedLocalPositions[i] = GetConfiguredOrGeneratedSolvedLocalPosition(block, i);
        }
    }

    private Vector3 GetConfiguredOrGeneratedSolvedLocalPosition(Transform block, int index)
    {
        if (pushSteps != null &&
            index >= 0 &&
            index < pushSteps.Length &&
            pushSteps[index] != null &&
            pushSteps[index].solvedLocalPosition != Vector3.zero)
        {
            return pushSteps[index].solvedLocalPosition;
        }

        return block.localPosition;
    }
    private void FailAndReset()
    {
        for (int i = 0; i < pushBlocks.Count; i++)
        {
            if (pushBlocks[i] != null && initialLocalPositions != null && i < initialLocalPositions.Length)
            {
                pushBlocks[i].localPosition = initialLocalPositions[i];
            }
        }

        currentIndex = 0;
        if (completedPushes != null)
        {
            for (int i = 0; i < completedPushes.Length; i++)
            {
                completedPushes[i] = false;
            }
        }

        movingBlockIndex = -1;
        movingWrongBlock = false;
        promptVisible = false;
        ShowResult(failurePrompt);
        SetIndicatorVisible(redIndicator, true);
        SetIndicatorVisible(greenIndicator, false);
    }

    private void ShowResult(string text)
    {
        resultPrompt = text;
        resultPromptEndsAt = Time.time + 3f;
    }

    private void RotateResultIndicators()
    {
        RotateIndicator(redIndicator);
        RotateIndicator(greenIndicator);
    }

    private void RotateIndicator(Transform indicator)
    {
        if (indicator != null && indicator.gameObject.activeSelf)
        {
            indicator.Rotate(indicator.forward, resultRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private static void SetIndicatorVisible(Transform indicator, bool visible)
    {
        if (indicator != null && indicator.gameObject.activeSelf != visible)
        {
            indicator.gameObject.SetActive(visible);
        }
    }
    private Vector3 GetMoveInputDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 localInput = new Vector3(horizontal, 0f, vertical);
        if (localInput.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Transform basis = Camera.main != null ? Camera.main.transform : player;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (right * horizontal + forward * vertical).normalized;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
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

    private bool IsPlayerNearTransform(Transform target, float distance)
    {
        return player != null &&
            target != null &&
            GetHorizontalDistanceToObject(Flatten(player.position), target) <= distance;
    }

    private bool IsPlayerOnTrigger(Transform target, float fallbackDistance)
    {
        if (player == null || target == null)
        {
            return false;
        }

        if (TryGetDetectionBounds(target, out Bounds bounds))
        {
            Vector3 position = player.position;
            bool insideX = position.x >= bounds.min.x - 0.15f && position.x <= bounds.max.x + 0.15f;
            bool insideZ = position.z >= bounds.min.z - 0.15f && position.z <= bounds.max.z + 0.15f;
            bool nearY = Mathf.Abs(position.y - bounds.center.y) <= Mathf.Max(4f, bounds.extents.y + 4f);
            return insideX && insideZ && nearY;
        }

        return GetHorizontalDistanceToObject(Flatten(player.position), target) <= fallbackDistance;
    }

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

    private static bool AddMeshColliders(Transform target)
    {
        bool addedAny = false;
        MeshFilter[] meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshCollider collider = meshFilter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            addedAny = true;
        }

        return addedAny;
    }

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
            collider.center = renderer.transform.InverseTransformPoint(renderer.bounds.center);
            collider.size = DivideByLossyScale(renderer.bounds.size, renderer.transform.lossyScale);
            addedAny = true;
        }

        return addedAny;
    }

    private static float GetClosestDistance(Vector3 point, Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        float closestSqrDistance = float.PositiveInfinity;
        bool found = false;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            Vector3 closestPoint = collider.ClosestPoint(point);
            float sqrDistance = (point - closestPoint).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                found = true;
            }
        }

        if (found)
        {
            return Mathf.Sqrt(closestSqrDistance);
        }

        if (TryGetWorldBounds(target, out Bounds bounds))
        {
            return Vector3.Distance(point, bounds.ClosestPoint(point));
        }

        return Vector3.Distance(point, target.position);
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

    private static Vector3 DivideByLossyScale(Vector3 size, Vector3 lossyScale)
    {
        return new Vector3(
            DivideByScale(size.x, lossyScale.x),
            DivideByScale(size.y, lossyScale.y),
            DivideByScale(size.z, lossyScale.z));
    }

    private static float DivideByScale(float value, float scale)
    {
        return Mathf.Abs(scale) > 0.0001f ? value / Mathf.Abs(scale) : value;
    }
}
