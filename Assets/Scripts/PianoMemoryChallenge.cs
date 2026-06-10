using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PianoMemoryChallenge : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform piano;
    [SerializeField] private AudioSource audioSource;

    [Header("Rules")]
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float noteSeconds = 0.42f;
    [SerializeField] private float noteGapSeconds = 0.16f;
    [SerializeField] private float noteVolume = 1f;

    private static readonly string[] KeyNames = { "1 Do", "2 Re", "3 Mi", "4 Fa", "5 Sol", "6 La", "7 Si", "8 Do" };
    private static readonly float[] NoteFrequencies = { 261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f, 493.88f, 523.25f };
    private static readonly KeyCode[] NumberKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8
    };
    private static readonly KeyCode[] KeypadKeys =
    {
        KeyCode.Keypad1,
        KeyCode.Keypad2,
        KeyCode.Keypad3,
        KeyCode.Keypad4,
        KeyCode.Keypad5,
        KeyCode.Keypad6,
        KeyCode.Keypad7,
        KeyCode.Keypad8
    };
    private static readonly int[][] Rounds =
    {
        new[] { 0, 2 },
        new[] { 0, 2, 4 },
        new[] { 0, 2, 4, 7 },
        new[] { 0, 2, 4, 7, 5 },
        new[] { 0, 2, 4, 7, 5, 6 }
    };

    private readonly Dictionary<int, AudioClip> noteClips = new Dictionary<int, AudioClip>();
    private bool active;
    private bool playingSequence;
    private bool failed;
    private bool won;
    private bool rewardGiven;
    private int roundIndex;
    private int inputIndex;
    private GUIStyle promptStyle;
    private GUIStyle titleStyle;
    private GUIStyle keyButtonStyle;
    private bool loggedFirstNote;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void Update()
    {
        if (!active)
        {
            if (IsNearPiano() && Input.GetKeyDown(interactKey))
            {
                StartChallenge();
            }

            return;
        }

        if (failed)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                RestartChallenge();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                ExitChallenge();
            }

            return;
        }

        if (won)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                ExitChallenge();
            }

            return;
        }

        if (playingSequence)
        {
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            if (Input.GetKeyDown(NumberKeys[i]) || Input.GetKeyDown(KeypadKeys[i]))
            {
                HandlePlayerNote(i);
                return;
            }
        }
    }

    private void OnGUI()
    {
        if (!active)
        {
            if (IsNearPiano())
            {
                DrawPrompt("Press E to play");
            }

            return;
        }

        DrawPianoPanel();
    }

    private void StartChallenge()
    {
        active = true;
        PlayFromBeginning();
    }

    private void RestartChallenge()
    {
        StopAllCoroutines();
        PlayFromBeginning();
    }

    private void PlayFromBeginning()
    {
        failed = false;
        won = false;
        roundIndex = 0;
        inputIndex = 0;
        StartCoroutine(PlayCurrentRound());
    }

    private void ExitChallenge()
    {
        StopAllCoroutines();
        active = false;
        failed = false;
        won = false;
        playingSequence = false;
    }

    private IEnumerator PlayCurrentRound()
    {
        playingSequence = true;
        inputIndex = 0;
        yield return new WaitForSeconds(0.5f);

        int[] sequence = Rounds[roundIndex];
        for (int i = 0; i < sequence.Length; i++)
        {
            PlayNote(sequence[i]);
            yield return new WaitForSeconds(noteSeconds + noteGapSeconds);
        }

        playingSequence = false;
    }

    private void HandlePlayerNote(int noteIndex)
    {
        PlayNote(noteIndex);
        int[] sequence = Rounds[roundIndex];
        if (noteIndex != sequence[inputIndex])
        {
            failed = true;
            GameAudioManager.PlayFail();
            return;
        }

        inputIndex++;
        if (inputIndex < sequence.Length)
        {
            return;
        }

        roundIndex++;
        if (roundIndex >= Rounds.Length)
        {
            won = true;
            GameAudioManager.PlaySuccess();
            if (!rewardGiven)
            {
                rewardGiven = true;
                ChapterTwoPuzzle.AddItemToInventory("Red Key");
            }
            return;
        }

        StartCoroutine(PlayCurrentRound());
    }

    private void DrawPianoPanel()
    {
        Rect panel = GameUiStyle.DialogueRect(280f);
        GameUiStyle.DrawDialoguePanel(panel);

        string title;
        if (failed)
        {
            title = "Performance failed";
        }
        else if (won)
        {
            title = "Performance complete. Red Key received.";
        }
        else if (playingSequence)
        {
            title = "Round " + (roundIndex + 1) + ": listen";
        }
        else
        {
            title = "Round " + (roundIndex + 1) + ": repeat";
        }

        GUI.Label(new Rect(panel.x + 20f, panel.y + 128f, panel.width - 40f, 34f), title, GameUiStyle.LabelStyle(ref titleStyle, 26, TextAnchor.MiddleCenter, FontStyle.Bold));

        if (failed)
        {
            GUI.Label(new Rect(panel.x + 20f, panel.y + 106f, panel.width - 40f, 30f), "Press A to restart    Press B to exit", GameUiStyle.LabelStyle(ref promptStyle, 22, TextAnchor.MiddleCenter, FontStyle.Bold));
        }
        else if (won)
        {
            GUI.Label(new Rect(panel.x + 20f, panel.y + 106f, panel.width - 40f, 30f), "Press B to exit", GameUiStyle.LabelStyle(ref promptStyle, 22, TextAnchor.MiddleCenter, FontStyle.Bold));
        }

        float keyWidth = (panel.width - 64f) / 8f;
        GUIStyle keyStyle = GameUiStyle.ButtonStyle(ref keyButtonStyle, 20);
        for (int i = 0; i < 8; i++)
        {
            Rect keyRect = new Rect(panel.x + 32f + i * keyWidth, panel.y + 200f, keyWidth - 8f, 64f);
            if (GUI.Button(keyRect, KeyNames[i], keyStyle) && !playingSequence && !failed && !won)
            {
                HandlePlayerNote(i);
            }
        }
    }

    private void DrawPrompt(string text)
    {
        Rect rect = GameUiStyle.InteractionPromptRect();
        GameUiStyle.DrawPanel(rect);
        GUI.Label(rect, text, GameUiStyle.LabelStyle(ref promptStyle, 30, TextAnchor.MiddleCenter, FontStyle.Bold));
    }

    private void PlayNote(int noteIndex)
    {
        EnsureAudioSource();
        AudioClip clip = GetOrCreateNoteClip(noteIndex);
        if (clip != null && audioSource != null)
        {
            if (!loggedFirstNote)
            {
                Debug.Log("[PianoMemoryChallenge] Playing piano note audio.");
                loggedFirstNote = true;
            }

            audioSource.PlayOneShot(clip, Mathf.Max(1f, noteVolume));
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.mute = false;
        audioSource.enabled = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.ignoreListenerVolume = true;
        audioSource.ignoreListenerPause = true;
    }

    private AudioClip GetOrCreateNoteClip(int noteIndex)
    {
        if (noteClips.TryGetValue(noteIndex, out AudioClip clip))
        {
            return clip;
        }

        int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * noteSeconds);
        float[] samples = new float[sampleCount];
        float frequency = NoteFrequencies[noteIndex];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-3.2f * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.95f;
        }

        clip = AudioClip.Create("PianoMemory_" + noteIndex, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        noteClips.Add(noteIndex, clip);
        return clip;
    }

    private bool IsNearPiano()
    {
        return player != null && piano != null && GetClosestDistance(player.position, piano) <= interactDistance;
    }

    private float GetClosestDistance(Vector3 point, Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        float closestSqrDistance = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            Vector3 closest = collider.ClosestPoint(point);
            float sqrDistance = (point - closest).sqrMagnitude;
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

        return Vector3.Distance(point, target.position);
    }
}
