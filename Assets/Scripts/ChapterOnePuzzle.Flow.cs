using UnityEngine;

public partial class ChapterOnePuzzle
{
    private void Awake()
    {
        activeInstance = this;
        RefreshReferences();
        LoadPersistentState();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
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

        if (promptVisible && Input.GetKeyDown(KeyCode.E))
        {
            StartPushingBlock(hoveredIndex);
        }
    }
}
