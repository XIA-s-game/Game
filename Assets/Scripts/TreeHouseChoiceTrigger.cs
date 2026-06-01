// Shows the tree house choice prompt and loads the selected route.
using UnityEngine;

public class TreeHouseChoiceTrigger : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform treeHouse;
    [SerializeField] private FairyMemorySideQuest fairyMemorySideQuest;

    [Header("Choice")]
    [SerializeField] private float enterDistance = 5.5f;
    [SerializeField] private float verticalToleranceBelow = 1.2f;
    [SerializeField] private float verticalToleranceAbove = 4.5f;
    [SerializeField] private string title = "System Choice";
    [SerializeField] private string choiceA = "A: Explore fairy memory fragments";
    [SerializeField] private string choiceB = "B: Skip and keep exploring";
    [SerializeField] private string hint = "Press A / B to choose";

    private bool choiceVisible;
    private bool choiceResolved;

    private void Update()
    {
        if (fairyMemorySideQuest != null && fairyMemorySideQuest.IsCompleted)
        {
            choiceResolved = true;
            choiceVisible = false;
            return;
        }

        if (choiceResolved || player == null || treeHouse == null)
        {
            return;
        }

        if (!choiceVisible && IsPlayerInsideTreeHouse())
        {
            choiceVisible = true;
        }

        if (!choiceVisible)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChooseSideQuest();
        }
        else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            SkipChoice();
        }
    }

    private void OnGUI()
    {
        if (!choiceVisible || choiceResolved)
        {
            return;
        }

        float width = Mathf.Min(720f, Screen.width - 80f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 120f, width, 240f);
        GameUiStyle.DrawPanel(rect);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = Color.white;

        GUIStyle optionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 22,
            wordWrap = true
        };
        optionStyle.normal.textColor = Color.white;

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 18
        };
        hintStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, 36f), title, titleStyle);
        GUI.Label(new Rect(rect.x + 36f, rect.y + 78f, rect.width - 72f, 44f), choiceA, optionStyle);
        GUI.Label(new Rect(rect.x + 36f, rect.y + 132f, rect.width - 72f, 44f), choiceB, optionStyle);
        GUI.Label(new Rect(rect.x + 24f, rect.y + rect.height - 42f, rect.width - 48f, 24f), hint, hintStyle);
    }

    private void ChooseSideQuest()
    {
        if (fairyMemorySideQuest != null)
        {
            fairyMemorySideQuest.Activate();
        }

        choiceResolved = true;
        choiceVisible = false;
    }

    private void SkipChoice()
    {
        choiceResolved = true;
        choiceVisible = false;
    }

    private bool IsPlayerInsideTreeHouse()
    {
        Vector3 toPlayer = player.position - treeHouse.position;
        Vector2 horizontal = new Vector2(toPlayer.x, toPlayer.z);
        bool closeEnough = horizontal.magnitude <= enterDistance;
        bool verticalOk = player.position.y >= treeHouse.position.y - verticalToleranceBelow &&
            player.position.y <= treeHouse.position.y + verticalToleranceAbove;

        return closeEnough && verticalOk;
    }
}
