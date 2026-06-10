using System.Collections;
using UnityEngine;

public partial class ChapterTwoPuzzle
{
    private void StartWelcomeIfNeeded()
    {
        if (welcomeStarted || player == null)
        {
            return;
        }

        welcomeStarted = true;
        ShowSystemPrompt(welcomePrompt, 3f);
        StartDialogue(welcomeDialogue, null);
    }

    private void StartQuizIntro()
    {
        if (quizStarted)
        {
            state = FlowState.Quiz;
            return;
        }

        quizStarted = true;
        StartDialogue(quizIntroDialogue, StartQuiz);
    }

    private void StartQuiz()
    {
        BuildQuizQuestions();
        ResetQuizProgress();
        state = FlowState.Quiz;
    }

    private void BuildQuizQuestions()
    {
        quizQuestions.Clear();
        AddQuestionsForVirtue("Courage", 2);
        AddQuestionsForVirtue("Kindness", 2);
        AddQuestionsForVirtue("Wisdom", 2);
        AddQuestionsForVirtue("Resolve", 2);
        AddQuestionsForVirtue("Patience", 2);
    }

    private void AddQuestionsForVirtue(string virtue, int count)
    {
        int added = 0;
        foreach (Question question in questions)
        {
            if (question.virtue != virtue)
            {
                continue;
            }

            quizQuestions.Add(question);
            added++;
            if (added >= count)
            {
                return;
            }
        }
    }

    private void UpdateQuizInput()
    {
        if (showingQuizFeedback)
        {
            if (Input.GetKeyDown(continueKey))
            {
                showingQuizFeedback = false;

                if (quizFailedAfterFeedback)
                {
                    ResetMazeAfterWrongAnswer();
                    return;
                }

                if (quizPassedAfterFeedback)
                {
                    CompleteSecondPageReward();
                    return;
                }

                currentQuestionIndex++;
                if (currentQuestionIndex >= quizQuestions.Count)
                {
                    if (correctAnswerCount >= 8)
                    {
                        CompleteSecondPageReward();
                    }
                    else
                    {
                        ResetMazeAfterWrongAnswer();
                    }
                }
            }

            return;
        }

        int selected = -1;
        if (Input.GetKeyDown(KeyCode.A)) selected = 0;
        if (Input.GetKeyDown(KeyCode.B)) selected = 1;
        if (Input.GetKeyDown(KeyCode.C)) selected = 2;
        if (Input.GetKeyDown(KeyCode.D)) selected = 3;

        if (selected < 0 || currentQuestionIndex < 0 || currentQuestionIndex >= quizQuestions.Count)
        {
            return;
        }

        quizPassedAfterFeedback = false;
        quizFailedAfterFeedback = false;
        Question question = quizQuestions[currentQuestionIndex];
        if (selected == question.correctIndex)
        {
            correctAnswerCount++;
            quizFeedback = "Correct.\n" + question.reason;
            if (correctAnswerCount >= 8)
            {
                quizPassedAfterFeedback = true;
                quizFeedback += "\n\nYou have eight correct answers. You may continue.";
            }
        }
        else
        {
            wrongAnswerCount++;
            string correct = question.options[question.correctIndex];
            quizFeedback = "Wrong.\nCorrect answer: " + correct + "\nReason: " + question.reason;
            if (wrongAnswerCount >= 2)
            {
                quizFailedAfterFeedback = true;
                quizFeedback += "\n\n" + quizFailedFeedback;
            }
        }

        showingQuizFeedback = true;
    }

    private void ResetMazeAfterWrongAnswer()
    {
        state = FlowState.Exploring;
        quizStarted = false;
        exitedMaze = false;
        mazeOpened = true;
        ResetQuizProgress();

        if (mazeBlock != null)
        {
            mazeBlock.SetActive(true);
            mazeBlock.transform.position = openedMazeBlockPosition;
        }

        if (guard != null && guardOriginalPositionReady)
        {
            guard.position = guardOriginalPosition + Vector3.right * guardMoveRightDistance;
        }

        MovePlayerToMazeStart();
        ShowSystemPrompt(quizFailedPrompt, 3f);
    }

    private void CompleteSecondPageReward()
    {
        quizCompleted = true;
        state = FlowState.Exploring;
        ResetQuizProgress();
        AddInventoryItem(secondPageItemName);
        ShowSystemPrompt(quizPassedPrompt, 3f);
        DropAirWallTwo();
    }

    private void DropAirWallTwoOnStartIfNeeded()
    {
        if (!dropAirWallTwoOnStart)
        {
            return;
        }

        DropAirWallTwo();
    }

    private void DropAirWallTwo()
    {
        if (airWallTwoDropped || airWallTwo == null)
        {
            return;
        }

        airWallTwoDropped = true;
        if (airWallTwoRoutine != null)
        {
            StopCoroutine(airWallTwoRoutine);
        }

        airWallTwoRoutine = StartCoroutine(MoveAirWallTwoDown());
    }

    private IEnumerator MoveAirWallTwoDown()
    {
        Vector3 start = airWallTwo.position;
        Vector3 target = start + Vector3.down * airWallTwoDropDistance;

        while (airWallTwo != null && Vector3.Distance(airWallTwo.position, target) > 0.01f)
        {
            airWallTwo.position = Vector3.MoveTowards(airWallTwo.position, target, airWallTwoDropSpeed * Time.deltaTime);
            yield return null;
        }

        if (airWallTwo != null)
        {
            airWallTwo.position = target;
        }

        airWallTwoRoutine = null;
    }

    private void MovePlayerToMazeStart()
    {
        if (player == null || guardInteract == null)
        {
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null)
        {
            controller.enabled = false;
        }

        Vector3 target = guardInteract.position;
        target.y = player.position.y;
        player.position = target;

        if (controller != null)
        {
            controller.enabled = controllerWasEnabled;
        }
    }

    private void OpenMaze()
    {
        if (mazeOpened)
        {
            return;
        }

        mazeOpened = true;
        if (guard != null)
        {
            guard.position += Vector3.right * guardMoveRightDistance;
        }

        RemoveInventoryItem(mazePassItemName);

        if (mazeBlock != null)
        {
            mazeBlock.SetActive(true);
            mazeBlock.transform.position = openedMazeBlockPosition;
        }
    }

    private void HideMazeBlock()
    {
        if (mazeBlock != null)
        {
            mazeBlock.SetActive(false);
        }
    }

    private void BuildQuestions()
    {
        questions.Clear();
        questions.Add(new Question("Courage", "A dark path is shorter, but a bright path is safer. Which do you choose?", new[] { "Dark path", "Bright path", "Wait here" }, 0, "Courage means facing the unknown when the goal matters."));
        questions.Add(new Question("Courage", "You hear someone crying in a dangerous part of the forest. What do you do?", new[] { "Go help", "Ignore it", "Hide" }, 0, "Courage is helping even when it is difficult."));
        questions.Add(new Question("Courage", "A deep cave may hold the clue you need. What is the best choice?", new[] { "Jump in", "Prepare first, then enter", "Leave forever" }, 1, "Courage is not recklessness. Preparation matters."));
        questions.Add(new Question("Kindness", "A hungry creature looks at your magic fruit. What do you do?", new[] { "Share it", "Keep it", "Hide it" }, 0, "Kindness values life over pride."));
        questions.Add(new Question("Kindness", "A small fairy stole from you. What should you do first?", new[] { "Punish it", "Ask why", "Run away" }, 1, "Kindness tries to understand before judging."));
        questions.Add(new Question("Kindness", "You find a key that may belong to someone else. What do you do?", new[] { "Use it", "Look for the owner", "Throw it away" }, 1, "A useful item can still belong to another person."));
        questions.Add(new Question("Wisdom", "Three doors stand before you. What helps most?", new[] { "Guess", "Ask a careful question", "Walk away" }, 1, "Wisdom uses logic instead of panic."));
        questions.Add(new Question("Wisdom", "You have few tools to cross a river. What do you do?", new[] { "Give up", "Use the tools carefully", "Break them" }, 1, "Wisdom makes the best use of limited resources."));
        questions.Add(new Question("Wisdom", "A magic lamp answers only a few questions. What is best?", new[] { "Ask directly", "Use elimination", "Ask silly questions" }, 1, "Good questions save time and effort."));
        questions.Add(new Question("Resolve", "A weak bridge leads to treasure. What do you do?", new[] { "Rush across", "Test it first", "Give up" }, 1, "Resolve means acting after judging the risk."));
        questions.Add(new Question("Resolve", "Another apprentice also wants the page. What is fair?", new[] { "Share or take turns", "Steal it", "Quit" }, 0, "Resolve can be firm without being cruel."));
        questions.Add(new Question("Resolve", "A spell needs a small sacrifice. What is wise?", new[] { "Accept if safe", "Refuse every cost", "Use someone else" }, 0, "Resolve includes responsibility."));
        questions.Add(new Question("Patience", "You fail to learn a spell after many tries. What now?", new[] { "Try a new method", "Quit", "Argue" }, 0, "Patience also means adjusting your method."));
        questions.Add(new Question("Patience", "A seed has not grown for a month. What do you do?", new[] { "Give up", "Check the conditions", "Throw away the soil" }, 1, "Patience looks for the reason before quitting."));
        questions.Add(new Question("Patience", "You are lost and keep returning to the same place. What helps?", new[] { "Cry", "Mark the path", "Run blindly" }, 1, "Patience learns from failure."));
    }

    private void ResetQuizProgress()
    {
        currentQuestionIndex = 0;
        correctAnswerCount = 0;
        wrongAnswerCount = 0;
        quizFeedback = null;
        quizPassedAfterFeedback = false;
        quizFailedAfterFeedback = false;
        showingQuizFeedback = false;
    }
}
