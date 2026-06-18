using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    // Prevents the same short sound from stacking too hard.
    private const float OneShotCooldown = 0.06f;

    // Current shared audio manager.
    private static GameAudioManager instance;

    [Header("Sources")]
    // AudioSource used for all short UI and quest sounds.
    [SerializeField] private AudioSource oneShotSource;

    [Header("One Shot Clips")]
    // Sound for collecting an item.
    [SerializeField] private AudioClip fetchClip;
    // Sound for a failed action or puzzle.
    [SerializeField] private AudioClip failClip;
    // Sound for a completed action or puzzle.
    [SerializeField] private AudioClip successClip;
    // Sound for small clue or interaction feedback.
    [SerializeField] private AudioClip knobClip;

    // Last time the fetch sound played.
    private float lastFetchTime = -1000f;
    // Last time the fail sound played.
    private float lastFailTime = -1000f;
    // Last time the success sound played.
    private float lastSuccessTime = -1000f;
    // Last time the knob sound played.
    private float lastKnobTime = -1000f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public static void PlayFetch()
    {
        if (instance != null)
        {
            instance.PlayOneShot(instance.fetchClip, 0.9f, ref instance.lastFetchTime);
        }
    }

    public static void PlayFail()
    {
        if (instance != null)
        {
            instance.PlayOneShot(instance.failClip, 1f, ref instance.lastFailTime);
        }
    }

    public static void PlaySuccess()
    {
        if (instance != null)
        {
            instance.PlayOneShot(instance.successClip, 1f, ref instance.lastSuccessTime);
        }
    }

    public static void PlayKnob()
    {
        if (instance != null)
        {
            instance.PlayOneShot(instance.knobClip, 0.85f, ref instance.lastKnobTime);
        }
    }
    // Helper method to play a one-shot clip with volume and cooldown management.
    private void PlayOneShot(AudioClip clip, float volume, ref float lastPlayTime)
    {
        if (clip == null || oneShotSource == null)
        {
            return;
        }

        if (Time.unscaledTime - lastPlayTime < OneShotCooldown)
        {
            return;
        }

        lastPlayTime = Time.unscaledTime;
        oneShotSource.PlayOneShot(clip, volume);
    }
    // Clean up the singleton instance reference when the object is destroyed.
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
