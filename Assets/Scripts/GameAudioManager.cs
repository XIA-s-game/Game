using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    private const float OneShotCooldown = 0.06f;

    private static GameAudioManager instance;

    [Header("Sources")]
    [SerializeField] private AudioSource oneShotSource;

    [Header("One Shot Clips")]
    [SerializeField] private AudioClip fetchClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip knobClip;

    private float lastFetchTime = -1000f;
    private float lastFailTime = -1000f;
    private float lastSuccessTime = -1000f;
    private float lastKnobTime = -1000f;
    // This script manages the playback of one-shot sound effects for game actions such as fetching, failing, succeeding, and interacting with knobs. It uses a single AudioSource to play clips and implements a cooldown to prevent rapid overlapping sounds. The static methods allow other scripts to trigger these sound effects without needing a direct reference to the GameAudioManager instance.
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
