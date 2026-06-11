// Runs the old man card shuffle challenge and gives the green key on success.
using System.Collections;
using System.Collections.Generic;
using AquariusMax.Fae.demo;
using UnityEngine;
using UnityEngine.UI;

public class OldManCardChallenge : MonoBehaviour
{
    public static bool hasGreenKey;

    [Header("Scene")]
    public Transform oldMan;
    public Transform player;
    public float interactDistance = 4f;

    [Header("Cards")]
    public GameObject card_bird;
    public GameObject card_castle;
    public GameObject card_clean;
    public GameObject card_dragon;
    public Vector3 flipEuler = new Vector3(0f, 180f, 0f);
    public float flipSeconds = 0.45f;
    public float stepSeconds = 0.68f;

    [Header("Camera And Control")]
    public Camera playerCamera;
    public MonoBehaviour[] playerControllers;
    public Vector3 challengeCameraPosition = new Vector3(446.11f, 36.815f, 652.58f);
    public Vector3 challengeCameraEuler = new Vector3(46.816f, -1.604f, -4.523f);

    [Header("UI")]
    public Text dialogueText;
    public Text hintText;

    private enum State
    {
        Exploring,
        Dialogue,
        Choice,
        Shuffling,
        AwaitingCardChoice
    }

    private enum CardType
    {
        Bird,
        Castle,
        Clean,
        Dragon
    }

    private enum StepKind
    {
        Swap,
        DoubleSwap,
        RotateRight,
        RotateLeft
    }

    private sealed class CardData
    {
        public readonly CardType type;
        public readonly string displayName;
        public readonly GameObject obj;
        public int slotIndex;

        public CardData(CardType type, string displayName, GameObject obj, int slotIndex)
        {
            this.type = type;
            this.displayName = displayName;
            this.obj = obj;
            this.slotIndex = slotIndex;
        }
    }

    private struct ShuffleStep
    {
        public readonly StepKind kind;
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public readonly int d;

        private ShuffleStep(StepKind kind, int a, int b, int c, int d)
        {
            this.kind = kind;
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public static ShuffleStep Swap(int a, int b)
        {
            return new ShuffleStep(StepKind.Swap, a, b, -1, -1);
        }

        public static ShuffleStep DoubleSwap(int a, int b, int c, int d)
        {
            return new ShuffleStep(StepKind.DoubleSwap, a, b, c, d);
        }

        public static ShuffleStep RotateRight()
        {
            return new ShuffleStep(StepKind.RotateRight, -1, -1, -1, -1);
        }

        public static ShuffleStep RotateLeft()
        {
            return new ShuffleStep(StepKind.RotateLeft, -1, -1, -1, -1);
        }
    }

    private static readonly ShuffleStep[][] PresetSequences =
    {
        new[]
        {
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.Swap(1, 2)
        },
        new[]
        {
            ShuffleStep.Swap(0, 3),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.Swap(0, 2),
            ShuffleStep.Swap(1, 3),
            ShuffleStep.Swap(0, 3),
            ShuffleStep.Swap(1, 2)
        },
        new[]
        {
            ShuffleStep.RotateRight(),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.RotateRight(),
            ShuffleStep.Swap(0, 3),
            ShuffleStep.RotateRight()
        },
        new[]
        {
            ShuffleStep.RotateLeft(),
            ShuffleStep.Swap(0, 1),
            ShuffleStep.RotateLeft(),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.RotateLeft()
        },
        new[]
        {
            ShuffleStep.Swap(0, 2),
            ShuffleStep.Swap(1, 3),
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.Swap(0, 3),
            ShuffleStep.Swap(0, 2)
        },
        new[]
        {
            ShuffleStep.Swap(0, 3),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.Swap(0, 3)
        },
        new[]
        {
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(0, 2),
            ShuffleStep.Swap(0, 3),
            ShuffleStep.Swap(1, 2),
            ShuffleStep.Swap(1, 3),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.Swap(0, 1)
        },
        new[]
        {
            ShuffleStep.DoubleSwap(0, 1, 2, 3),
            ShuffleStep.DoubleSwap(0, 3, 1, 2),
            ShuffleStep.DoubleSwap(0, 2, 1, 3),
            ShuffleStep.Swap(0, 1),
            ShuffleStep.Swap(2, 3),
            ShuffleStep.Swap(0, 3)
        }
    };

    private readonly List<CardData> cards = new List<CardData>();
    private readonly Vector3[] slotPositions = new Vector3[4];
    private readonly Quaternion[] slotRotations = new Quaternion[4];
    private readonly List<MonoBehaviour> disabledControllers = new List<MonoBehaviour>();

    private State state = State.Exploring;
    private string[] activeLines;
    private int lineIndex;
    private System.Action dialogueFinished;
    private System.Action acceptChoice;
    private System.Action declineChoice;
    private bool hasFailedChallenge;
    private bool initializedCards;
    private bool cameraWasSaved;
    private bool hasSubmittedChoice;
    private CardType targetCardType;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private Coroutine runningChallenge;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        if (state != State.Exploring)
        {
            UnlockPlayerControl();
            RestoreCamera();
        }
    }

    private void Update()
    {
        EnsureCards();

        switch (state)
        {
            case State.Exploring:
                UpdateExploring();
                break;
            case State.Dialogue:
                if (Input.GetKeyDown(KeyCode.C))
                {
                    AdvanceDialogue();
                }
                break;
            case State.Choice:
                UpdateChoice();
                break;
            case State.AwaitingCardChoice:
                UpdateCardChoice();
                break;
        }
    }

    private void UpdateExploring()
    {
        bool nearOldMan = IsNearOldMan();
        SetUi(nearOldMan ? "Press E to talk" : string.Empty, string.Empty);

        if (!nearOldMan || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (hasGreenKey)
        {
            StartDialogue(new[] { "Old Man: I already gave you the key. Keep going." }, EndConversation);
        }
        else if (hasFailedChallenge)
        {
            StartRetryDialogue();
        }
        else
        {
            StartIntroDialogue();
        }
    }

    private void StartIntroDialogue()
    {
        StartDialogue(new[]
        {
            "Old Man: Looking for a key?",
            "Player: Do you know where it is?",
            "Old Man: I do, but you must pass my card test first.",
            "Player: What test?",
            "Old Man: Watch the four cards. I will shuffle them, then you pick the one I name.",
            "Player: Sounds simple.",
            "Old Man: Wait until they move."
        }, () =>
        {
            StartChoice("Press A: Accept", "Press B: Leave", StartAcceptedChallenge, () =>
            {
                StartDialogue(new[] { "Old Man: Come back when you are ready." }, EndConversation);
            });
        });
    }

    private void StartRetryDialogue()
    {
        StartDialogue(new[] { "Old Man: Try the card test again?" }, () =>
        {
            StartChoice("Press A: Try again", "Press B: Leave", StartAcceptedChallenge, () =>
            {
                StartDialogue(new[] { "Old Man: Practice your eyes and come back." }, EndConversation);
            });
        });
    }

    private void StartAcceptedChallenge()
    {
        StartDialogue(new[] { "Old Man: Good. Watch carefully." }, () =>
        {
            if (runningChallenge != null)
            {
                StopCoroutine(runningChallenge);
            }

            runningChallenge = StartCoroutine(RunChallenge());
        });
    }

    private IEnumerator RunChallenge()
    {
        // The shuffle is preset on purpose, so the round is fair and readable.
        state = State.Shuffling;
        hasSubmittedChoice = false;
        EnsureCards();
        if (cards.Count < 4)
        {
            StartDialogue(new[] { "Old Man: The cards are not ready yet." }, EndConversation);
            yield break;
        }

        LockPlayerControl();
        SwitchToChallengeCamera();
        ResetCardsToDefault();
        SetUi("Watch the four card positions.", string.Empty);

        yield return new WaitForSeconds(0.5f);
        yield return FlipCards(true);
        yield return RunPresetSequence(PresetSequences[Random.Range(0, PresetSequences.Length)]);

        CardData targetCard = cards[Random.Range(0, cards.Count)];
        targetCardType = targetCard.type;
        StartDialogue(new[] { "Old Man: Now pick " + targetCard.displayName + "." }, () =>
        {
            hasSubmittedChoice = false;
            state = State.AwaitingCardChoice;
            SetUi("Choose a card: A left, B left middle, C right middle, D right.", string.Empty);
        });
    }

    private void UpdateChoice()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            System.Action action = acceptChoice;
            ClearChoice();
            action?.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            System.Action action = declineChoice;
            ClearChoice();
            action?.Invoke();
        }
    }

    private void UpdateCardChoice()
    {
        if (hasSubmittedChoice)
        {
            return;
        }

        int chosenSlot = -1;
        if (Input.GetKeyDown(KeyCode.A)) chosenSlot = 0;
        if (Input.GetKeyDown(KeyCode.B)) chosenSlot = 1;
        if (Input.GetKeyDown(KeyCode.C)) chosenSlot = 2;
        if (Input.GetKeyDown(KeyCode.D)) chosenSlot = 3;

        if (chosenSlot < 0)
        {
            return;
        }

        CardData chosenCard = GetCardAtSlot(chosenSlot);
        if (chosenCard == null)
        {
            return;
        }

        hasSubmittedChoice = true;
        StartCoroutine(RevealAndFinish(chosenCard));
    }

    private IEnumerator RevealAndFinish(CardData chosenCard)
    {
        state = State.Shuffling;
        yield return FlipCards(false);

        string targetName = GetDisplayName(targetCardType);
        if (chosenCard.type == targetCardType)
        {
            hasGreenKey = true;
            hasFailedChallenge = false;
            GameAudioManager.PlaySuccess();
            ChapterTwoPuzzle.AddItemToInventory("Green Key");
            StartDialogue(new[] { "Old Man: Well done. Take the green key." }, EndChallenge);
        }
        else
        {
            hasFailedChallenge = true;
            GameAudioManager.PlayFail();
            ResetCardsToDefault();
            StartDialogue(new[] { "Old Man: You chose " + chosenCard.displayName + ", not " + targetName + "." }, EndChallenge);
        }
    }

    private void EndChallenge()
    {
        runningChallenge = null;
        RestoreCamera();
        UnlockPlayerControl();
        EndConversation();
    }

    private void EndConversation()
    {
        activeLines = null;
        dialogueFinished = null;
        acceptChoice = null;
        declineChoice = null;
        hasSubmittedChoice = false;
        state = State.Exploring;
        SetUi(string.Empty, string.Empty);
    }

    private void StartDialogue(string[] lines, System.Action onFinished)
    {
        activeLines = lines;
        lineIndex = 0;
        dialogueFinished = onFinished;
        state = State.Dialogue;
        DrawCurrentDialogueLine();
    }

    private void AdvanceDialogue()
    {
        lineIndex++;
        if (activeLines != null && lineIndex < activeLines.Length)
        {
            DrawCurrentDialogueLine();
            return;
        }

        System.Action finished = dialogueFinished;
        activeLines = null;
        dialogueFinished = null;
        lineIndex = 0;
        SetUi(string.Empty, string.Empty);
        finished?.Invoke();
    }

    private void DrawCurrentDialogueLine()
    {
        string line = activeLines != null && lineIndex >= 0 && lineIndex < activeLines.Length ? activeLines[lineIndex] : string.Empty;
        SetUi(line, "Press C to continue");
    }

    private void StartChoice(string accept, string decline, System.Action onAccept, System.Action onDecline)
    {
        acceptChoice = onAccept;
        declineChoice = onDecline;
        state = State.Choice;
        SetUi(accept + "\n" + decline, string.Empty);
    }

    private void ClearChoice()
    {
        acceptChoice = null;
        declineChoice = null;
        SetUi(string.Empty, string.Empty);
    }

    private IEnumerator FlipCards(bool toBack)
    {
        if (cards.Count < 4)
        {
            yield break;
        }

        Quaternion[] starts = new Quaternion[cards.Count];
        Quaternion[] targets = new Quaternion[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            starts[i] = cards[i].obj.transform.rotation;
            targets[i] = toBack ? slotRotations[cards[i].slotIndex] * Quaternion.Euler(flipEuler) : slotRotations[cards[i].slotIndex];
        }

        float elapsed = 0f;
        while (elapsed < flipSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / flipSeconds));
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].obj.transform.rotation = Quaternion.Slerp(starts[i], targets[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].obj.transform.rotation = targets[i];
        }
    }

    private IEnumerator RunPresetSequence(ShuffleStep[] sequence)
    {
        for (int i = 0; i < sequence.Length; i++)
        {
            yield return RunShuffleStep(sequence[i]);
        }
    }

    private IEnumerator RunShuffleStep(ShuffleStep step)
    {
        switch (step.kind)
        {
            case StepKind.Swap:
                yield return SwapSlots(step.a, step.b, stepSeconds);
                break;
            case StepKind.DoubleSwap:
                yield return DoubleSwapSlots(step.a, step.b, step.c, step.d, stepSeconds);
                break;
            case StepKind.RotateRight:
                yield return RotateSlots(true, stepSeconds);
                break;
            case StepKind.RotateLeft:
                yield return RotateSlots(false, stepSeconds);
                break;
        }
    }

    private IEnumerator SwapSlots(int firstSlot, int secondSlot, float duration)
    {
        CardData first = GetCardAtSlot(firstSlot);
        CardData second = GetCardAtSlot(secondSlot);
        if (first == null || second == null)
        {
            yield break;
        }

        Vector3 firstStart = first.obj.transform.position;
        Vector3 secondStart = second.obj.transform.position;
        Vector3 firstTarget = slotPositions[secondSlot];
        Vector3 secondTarget = slotPositions[firstSlot];

        yield return AnimateCards(new[] { first, second }, new[] { firstStart, secondStart }, new[] { firstTarget, secondTarget }, duration);

        first.slotIndex = secondSlot;
        second.slotIndex = firstSlot;
        SnapAllCardsToSlots(true);
    }

    private IEnumerator DoubleSwapSlots(int firstA, int firstB, int secondA, int secondB, float duration)
    {
        CardData cardOne = GetCardAtSlot(firstA);
        CardData cardTwo = GetCardAtSlot(firstB);
        CardData cardThree = GetCardAtSlot(secondA);
        CardData cardFour = GetCardAtSlot(secondB);
        if (cardOne == null || cardTwo == null || cardThree == null || cardFour == null)
        {
            yield break;
        }

        CardData[] movingCards = { cardOne, cardTwo, cardThree, cardFour };
        Vector3[] starts =
        {
            cardOne.obj.transform.position,
            cardTwo.obj.transform.position,
            cardThree.obj.transform.position,
            cardFour.obj.transform.position
        };
        Vector3[] targets =
        {
            slotPositions[firstB],
            slotPositions[firstA],
            slotPositions[secondB],
            slotPositions[secondA]
        };

        yield return AnimateCards(movingCards, starts, targets, duration);

        cardOne.slotIndex = firstB;
        cardTwo.slotIndex = firstA;
        cardThree.slotIndex = secondB;
        cardFour.slotIndex = secondA;
        SnapAllCardsToSlots(true);
    }

    private IEnumerator RotateSlots(bool right, float duration)
    {
        CardData[] slotCards =
        {
            GetCardAtSlot(0),
            GetCardAtSlot(1),
            GetCardAtSlot(2),
            GetCardAtSlot(3)
        };

        for (int i = 0; i < slotCards.Length; i++)
        {
            if (slotCards[i] == null)
            {
                yield break;
            }
        }

        Vector3[] starts =
        {
            slotCards[0].obj.transform.position,
            slotCards[1].obj.transform.position,
            slotCards[2].obj.transform.position,
            slotCards[3].obj.transform.position
        };
        Vector3[] targets;
        if (right)
        {
            targets = new[] { slotPositions[1], slotPositions[2], slotPositions[3], slotPositions[0] };
        }
        else
        {
            targets = new[] { slotPositions[3], slotPositions[0], slotPositions[1], slotPositions[2] };
        }

        yield return AnimateCards(slotCards, starts, targets, duration);

        if (right)
        {
            slotCards[0].slotIndex = 1;
            slotCards[1].slotIndex = 2;
            slotCards[2].slotIndex = 3;
            slotCards[3].slotIndex = 0;
        }
        else
        {
            slotCards[0].slotIndex = 3;
            slotCards[1].slotIndex = 0;
            slotCards[2].slotIndex = 1;
            slotCards[3].slotIndex = 2;
        }

        SnapAllCardsToSlots(true);
    }

    private IEnumerator AnimateCards(CardData[] movingCards, Vector3[] starts, Vector3[] targets, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < movingCards.Length; i++)
            {
                movingCards[i].obj.transform.position = Vector3.Lerp(starts[i], targets[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < movingCards.Length; i++)
        {
            movingCards[i].obj.transform.position = targets[i];
        }
    }

    private void ResetCardsToDefault()
    {
        EnsureCards();
        if (cards.Count < 4)
        {
            return;
        }

        SetCardToSlot(CardType.Bird, 0);
        SetCardToSlot(CardType.Castle, 1);
        SetCardToSlot(CardType.Clean, 2);
        SetCardToSlot(CardType.Dragon, 3);
        SnapAllCardsToSlots(false);
    }

    private void SetCardToSlot(CardType type, int slot)
    {
        CardData card = GetCardByType(type);
        if (card == null)
        {
            return;
        }

        card.slotIndex = slot;
        card.obj.SetActive(true);
        card.obj.transform.position = slotPositions[slot];
        card.obj.transform.rotation = slotRotations[slot];
    }

    private void SnapAllCardsToSlots(bool backSide)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            card.obj.transform.position = slotPositions[card.slotIndex];
            card.obj.transform.rotation = backSide ? slotRotations[card.slotIndex] * Quaternion.Euler(flipEuler) : slotRotations[card.slotIndex];
        }
    }

    private CardData GetCardAtSlot(int slot)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].slotIndex == slot)
            {
                return cards[i];
            }
        }

        return null;
    }

    private CardData GetCardByType(CardType type)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].type == type)
            {
                return cards[i];
            }
        }

        return null;
    }

    private string GetDisplayName(CardType type)
    {
        switch (type)
        {
            case CardType.Bird:
                return "Bird";
            case CardType.Castle:
                return "Castle";
            case CardType.Clean:
                return "Broom";
            case CardType.Dragon:
                return "Dragon";
            default:
                return string.Empty;
        }
    }

    private void SwitchToChallengeCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            return;
        }

        savedCameraPosition = playerCamera.transform.position;
        savedCameraRotation = playerCamera.transform.rotation;
        cameraWasSaved = true;
        playerCamera.transform.SetPositionAndRotation(challengeCameraPosition, Quaternion.Euler(challengeCameraEuler));
    }

    private void RestoreCamera()
    {
        if (playerCamera != null && cameraWasSaved)
        {
            playerCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
        }

        cameraWasSaved = false;
    }

    private void LockPlayerControl()
    {
        disabledControllers.Clear();
        if (playerControllers != null)
        {
            for (int i = 0; i < playerControllers.Length; i++)
            {
                DisableController(playerControllers[i]);
            }
        }

        if (disabledControllers.Count == 0 && player != null)
        {
            MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && (behaviour.GetType().Name == "PlayerCharacterController" || behaviour is DemoCharacter))
                {
                    DisableController(behaviour);
                }
            }
        }

        DemoCharacter.LockPlayerInput = true;
        OnLockPlayerControl();
    }

    private void DisableController(MonoBehaviour behaviour)
    {
        if (behaviour == null || behaviour == this || !behaviour.enabled)
        {
            return;
        }

        behaviour.enabled = false;
        disabledControllers.Add(behaviour);
    }

    private void UnlockPlayerControl()
    {
        for (int i = 0; i < disabledControllers.Count; i++)
        {
            if (disabledControllers[i] != null)
            {
                disabledControllers[i].enabled = true;
            }
        }

        disabledControllers.Clear();
        DemoCharacter.LockPlayerInput = false;
        OnUnlockPlayerControl();
    }

    protected virtual void OnLockPlayerControl()
    {
    }

    protected virtual void OnUnlockPlayerControl()
    {
    }

    private void EnsureCards()
    {
        if (initializedCards)
        {
            return;
        }

        if (card_bird == null || card_castle == null || card_clean == null || card_dragon == null)
        {
            return;
        }

        GameObject[] objects = { card_bird, card_castle, card_clean, card_dragon };
        System.Array.Sort(objects, (left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        for (int i = 0; i < objects.Length; i++)
        {
            slotPositions[i] = objects[i].transform.position;
            slotRotations[i] = objects[i].transform.rotation;
        }

        cards.Clear();
        cards.Add(new CardData(CardType.Bird, "Bird", card_bird, 0));
        cards.Add(new CardData(CardType.Castle, "Castle", card_castle, 1));
        cards.Add(new CardData(CardType.Clean, "Broom", card_clean, 2));
        cards.Add(new CardData(CardType.Dragon, "Dragon", card_dragon, 3));
        initializedCards = true;
        ResetCardsToDefault();
    }

    private bool IsNearOldMan()
    {
        return player != null && oldMan != null && Vector3.Distance(player.position, oldMan.position) <= interactDistance;
    }

    private void SetUi(string dialogue, string hint)
    {
        SetTextVisible(dialogueText, dialogue);
        SetTextVisible(hintText, hint);
    }

    private void SetTextVisible(Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        bool visible = !string.IsNullOrEmpty(value);
        text.text = value;
        text.enabled = visible;
        if (text.transform.parent != null)
        {
            text.transform.parent.gameObject.SetActive(visible);
        }
    }
}
