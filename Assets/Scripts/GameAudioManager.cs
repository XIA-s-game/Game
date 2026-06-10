using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    private const float OneShotCooldown = 0.06f;

    private static GameAudioManager instance;

    [Header("Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource enemyLoopSource;
    [SerializeField] private AudioSource roarLoopSource;

    [Header("One Shot Clips")]
    [SerializeField] private AudioClip fetchClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip knobClip;

    [Header("Loop Clips")]
    [SerializeField] private AudioClip enemyClip;
    [SerializeField] private AudioClip roarClip;

    private float lastFetchTime = -1000f;
    private float lastFailTime = -1000f;
    private float lastSuccessTime = -1000f;
    private float lastKnobTime = -1000f;
    private bool enemyLoopRequested;
    private bool roarLoopRequested;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ConfigureLoopSource(enemyLoopSource);
        ConfigureLoopSource(roarLoopSource);
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

    public static void StartEnemyLoop()
    {
        if (instance == null)
        {
            return;
        }

        instance.enemyLoopRequested = true;
        MainMenuController.PauseBackgroundMusicForSceneAudio();
        instance.PlayLoop(instance.enemyLoopSource, instance.enemyClip, 1f, instance.enemyLoopRequested);
    }

    public static void StartRoarLoop()
    {
        if (instance == null)
        {
            return;
        }

        instance.roarLoopRequested = true;
        instance.PlayLoop(instance.roarLoopSource, instance.roarClip, 1f, instance.roarLoopRequested);
    }

    public static void StopEnemyLoop()
    {
        if (instance == null)
        {
            return;
        }

        instance.enemyLoopRequested = false;
        if (instance.enemyLoopSource != null)
        {
            instance.enemyLoopSource.Stop();
        }

        MainMenuController.ResumeBackgroundMusicAfterSceneAudio();
    }

    public static void StopRoarLoop()
    {
        if (instance == null)
        {
            return;
        }

        instance.roarLoopRequested = false;
        if (instance.roarLoopSource != null)
        {
            instance.roarLoopSource.Stop();
        }
    }

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

    private void PlayLoop(AudioSource source, AudioClip clip, float volume, bool requested)
    {
        if (!requested || source == null || clip == null)
        {
            return;
        }

        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.spatialBlend = 0f;

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private static void ConfigureLoopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
