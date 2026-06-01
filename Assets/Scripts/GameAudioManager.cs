// Small shared audio helper for UI clicks, wins, fails, pickups, and enemy loops.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class GameAudioManager : MonoBehaviour
{
    private const float OneShotCooldown = 0.06f;

    private static GameAudioManager instance;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>();
    private readonly HashSet<string> loadingClips = new HashSet<string>();

    private AudioSource oneShotSource;
    private AudioSource enemyLoopSource;
    private bool enemyLoopRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void PlayFetch()
    {
        PlayOneShot("fetch", "Audio/fetch.mp3", 0.9f);
    }

    public static void PlayFail()
    {
        PlayOneShot("fail", "Audio/fail.mp3", 1f);
    }

    public static void PlaySuccess()
    {
        PlayOneShot("success", "Audio/success.mp3", 1f);
    }

    public static void PlayKnob()
    {
        PlayOneShot("knob", "Audio/knob.mp3", 0.85f);
    }

    public static void StartEnemyLoop()
    {
        GameAudioManager manager = EnsureInstance();
        manager.enemyLoopRequested = true;
        manager.StartCoroutine(manager.PlayLoopWhenLoaded("enemy", "Audio/enemy.mp3", 0.8f));
    }

    public static void StopEnemyLoop()
    {
        GameAudioManager manager = EnsureInstance();
        manager.enemyLoopRequested = false;
        if (manager.enemyLoopSource != null)
        {
            manager.enemyLoopSource.Stop();
        }
    }

    private static void PlayOneShot(string key, string relativePath, float volume)
    {
        GameAudioManager manager = EnsureInstance();
        manager.StartCoroutine(manager.PlayOneShotWhenLoaded(key, relativePath, volume));
    }

    private static GameAudioManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject host = new GameObject("GameAudioManager");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<GameAudioManager>();
        instance.oneShotSource = host.AddComponent<AudioSource>();
        instance.oneShotSource.playOnAwake = false;
        instance.oneShotSource.spatialBlend = 0f;
        instance.enemyLoopSource = host.AddComponent<AudioSource>();
        instance.enemyLoopSource.playOnAwake = false;
        instance.enemyLoopSource.loop = true;
        instance.enemyLoopSource.spatialBlend = 0f;
        return instance;
    }

    private IEnumerator PlayOneShotWhenLoaded(string key, string relativePath, float volume)
    {
        yield return LoadClip(key, relativePath);

        if (!clips.TryGetValue(key, out AudioClip clip) || clip == null || oneShotSource == null)
        {
            yield break;
        }

        if (lastPlayTimes.TryGetValue(key, out float lastTime) && Time.unscaledTime - lastTime < OneShotCooldown)
        {
            yield break;
        }

        lastPlayTimes[key] = Time.unscaledTime;
        oneShotSource.PlayOneShot(clip, volume);
    }

    private IEnumerator PlayLoopWhenLoaded(string key, string relativePath, float volume)
    {
        yield return LoadClip(key, relativePath);

        if (!enemyLoopRequested ||
            !clips.TryGetValue(key, out AudioClip clip) ||
            clip == null ||
            enemyLoopSource == null)
        {
            yield break;
        }

        enemyLoopSource.clip = clip;
        enemyLoopSource.volume = volume;
        enemyLoopSource.loop = true;
        if (!enemyLoopSource.isPlaying)
        {
            enemyLoopSource.Play();
        }
    }

    private IEnumerator LoadClip(string key, string relativePath)
    {
        if (clips.ContainsKey(key))
        {
            yield break;
        }

        while (loadingClips.Contains(key))
        {
            yield return null;
        }

        if (clips.ContainsKey(key))
        {
            yield break;
        }

        loadingClips.Add(key);
        string path = Path.Combine(Application.dataPath, relativePath);
        if (File.Exists(path))
        {
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri, AudioType.MPEG))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                    {
                        clips[key] = clip;
                    }
                }
            }
        }

        loadingClips.Remove(key);
    }
}
