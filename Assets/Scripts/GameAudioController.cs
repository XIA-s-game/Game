using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class GameAudioController : MonoBehaviour
{
    private const string ControllerName = "GameAudioController";
    private const string MenuSceneName = "Mainmenu";
    private const string CoverPath = "Assets/Audio/cover.mp3";
    private const string BackgroundPath = "Assets/Audio/background.mp3";

    [SerializeField] private AudioClip coverMusic;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;

    private AudioSource musicSource;
    private AudioClip currentClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindObjectOfType<GameAudioController>() != null)
        {
            return;
        }

        GameObject host = new GameObject(ControllerName);
        host.AddComponent<GameAudioController>();
    }

    private void Awake()
    {
        GameAudioController[] controllers = FindObjectsOfType<GameAudioController>();
        if (controllers.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        LoadDefaultClipsInEditor();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        if (IsMenuScene(sceneName))
        {
            PlayCoverMusic();
            return;
        }

        PlayMusic(backgroundMusic);
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        currentClip = null;
    }

    private void PlayCoverMusic()
    {
        if (coverMusic != null)
        {
            PlayMusic(coverMusic);
            return;
        }

        PlayMusic(backgroundMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || currentClip == clip)
        {
            return;
        }

        currentClip = clip;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private static bool IsMenuScene(string sceneName)
    {
        return string.Equals(sceneName, MenuSceneName, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sceneName, "MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }

    private void LoadDefaultClipsInEditor()
    {
#if UNITY_EDITOR
        if (coverMusic == null)
        {
            coverMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(CoverPath);
            if (coverMusic == null)
            {
                coverMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/cover.wav");
            }

            if (coverMusic == null)
            {
                coverMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/cover.ogg");
            }
        }

        if (backgroundMusic == null)
        {
            backgroundMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(BackgroundPath);
        }
#endif
    }
}
