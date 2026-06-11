public partial class ChapterTwoPuzzle
{
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
