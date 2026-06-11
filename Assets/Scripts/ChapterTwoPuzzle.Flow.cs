using UnityEngine;

public partial class ChapterTwoPuzzle
{
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildQuestions();
        RefreshReferences();
    }

    private void OnDisable()
    {
        ClearPlayerLocks();
    }

    private void Update()
    {
        if (!IsTargetScene())
        {
            ClearPlayerLocks();
            return;
        }

        StartWelcomeIfNeeded();
        ApplyPlayerStateLocks();

        if (state == FlowState.Dialogue)
        {
            if (Input.GetKeyDown(continueKey))
            {
                AdvanceDialogue();
            }

            return;
        }

        if (state == FlowState.Quiz)
        {
            UpdateQuizInput();
            return;
        }

        if (state == FlowState.BoardGame)
        {
            UpdateBoardGameInput();
            return;
        }

        UpdateExplorationInput();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !IsTargetScene())
        {
            return;
        }

        DrawSystemPrompt();
        if (state == FlowState.Dialogue)
        {
            DrawDialogue();
            return;
        }

        if (state == FlowState.Quiz)
        {
            DrawQuiz();
            return;
        }

        if (state == FlowState.BoardGame)
        {
            DrawBoardGame();
            return;
        }

        DrawInteractPrompts();
    }

    private void ClearPlayerLocks()
    {
        AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = false;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
        AquariusMax.Fae.demo.DemoCharacter.UseLookPadInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ApplyPlayerStateLocks()
    {
        bool quizActive = state == FlowState.Quiz;
        bool boardGameActive = state == FlowState.BoardGame;
        AquariusMax.Fae.demo.DemoCharacter.LockPlayerInput = quizActive;
        AquariusMax.Fae.demo.DemoCharacter.LockMovementInput = boardGameActive;
        AquariusMax.Fae.demo.DemoCharacter.UseLookPadInput = false;
        AquariusMax.Fae.demo.DemoCharacter.LookPadInput = Vector2.zero;
        Cursor.visible = quizActive;
        Cursor.lockState = quizActive ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private Animator GetPlayerAnimator()
    {
        if (player == null)
        {
            return null;
        }

        if (playerAnimator == null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>();
        }

        return playerAnimator;
    }
}
