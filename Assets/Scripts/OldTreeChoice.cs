using System;
using UnityEngine;

public class OldTreeChoice : MonoBehaviour
{
    public enum EggChoice
    {
        TakeEgg,
        DestroyEgg,
        LeaveIt,
        MoveIt
    }

    [Serializable]
    public struct Option
    {
        public EggChoice choice;
        public KeyCode key;
        public KeyCode alternateKey;
        public string label;
    }

    [SerializeField] private string prompt = "What would you choose?";
    [SerializeField]
    private Option[] options =
    {
        new Option { choice = EggChoice.TakeEgg, key = KeyCode.A, alternateKey = KeyCode.Alpha1, label = "A: Take the egg" },
        new Option { choice = EggChoice.DestroyEgg, key = KeyCode.B, alternateKey = KeyCode.Alpha2, label = "B: Destroy the egg" },
        new Option { choice = EggChoice.LeaveIt, key = KeyCode.C, alternateKey = KeyCode.Alpha3, label = "C: Leave it there" },
        new Option { choice = EggChoice.MoveIt, key = KeyCode.D, alternateKey = KeyCode.Alpha4, label = "D: Move it somewhere safer" }
    };

    public bool TryReadInput(out EggChoice choice)
    {
        for (int i = 0; i < options.Length; i++)
        {
            Option option = options[i];
            if (Input.GetKeyDown(option.key) || Input.GetKeyDown(option.alternateKey))
            {
                choice = option.choice;
                return true;
            }
        }

        choice = EggChoice.LeaveIt;
        return false;
    }

    public void Draw(Font font, Action<EggChoice> onChosen)
    {
        float width = Mathf.Min(900f, Screen.width - 80f);
        float height = 120f + options.Length * 50f;
        Rect rect = GameUiStyle.DialogueRect(height);

        GameUiStyle.DrawPanel(rect);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        ApplyFont(textStyle, font);
        textStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 60f), prompt, textStyle);

        for (int i = 0; i < options.Length; i++)
        {
            Option option = options[i];
            if (DrawButton(rect, 92f + i * 50f, option.label, font))
            {
                onChosen?.Invoke(option.choice);
            }
        }
    }

    private static bool DrawButton(Rect parent, float yOffset, string text, Font font)
    {
        Rect rect = new Rect(parent.x + 24f, parent.y + yOffset, parent.width - 48f, 38f);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 18,
            wordWrap = true
        };
        ApplyFont(style, font);

        return GUI.Button(rect, text, style);
    }

    private static void ApplyFont(GUIStyle style, Font font)
    {
        if (font != null)
        {
            style.font = font;
        }
    }
}
