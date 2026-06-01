using System;

public sealed class OldTreeDialogue
{
    private string[] lines;
    private int lineIndex;
    private Action onComplete;
    private bool autoCompleteOnLastLine;

    public string CurrentLine { get; private set; }

    public void Begin(string[] dialogueLines, Action completeCallback, bool autoCompleteLastLine = false)
    {
        lines = dialogueLines;
        lineIndex = 0;
        onComplete = completeCallback;
        this.autoCompleteOnLastLine = autoCompleteLastLine;
        CurrentLine = lines != null && lines.Length > 0 ? lines[0] : string.Empty;
    }

    public bool ShouldCompleteImmediately()
    {
        return autoCompleteOnLastLine && lines != null && lines.Length == 1;
    }

    public Action Advance(Action<string> onLineChanged)
    {
        lineIndex++;
        if (lines != null && lineIndex < lines.Length)
        {
            CurrentLine = lines[lineIndex];
            onLineChanged?.Invoke(CurrentLine);

            if (autoCompleteOnLastLine && lineIndex == lines.Length - 1)
            {
                return Finish();
            }

            return null;
        }

        return Finish();
    }

    public void Cancel()
    {
        lines = null;
        onComplete = null;
        autoCompleteOnLastLine = false;
        CurrentLine = null;
    }

    private Action Finish()
    {
        Action completeCallback = onComplete;
        lines = null;
        onComplete = null;
        autoCompleteOnLastLine = false;
        CurrentLine = null;
        return completeCallback;
    }
}
