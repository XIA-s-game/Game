using UnityEngine;

public static class GameAudioSettings
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    private const string VolumeKey = "GameAudioSettings.MasterVolume";
    private const string MutedKey = "GameAudioSettings.Muted";

    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(VolumeKey, 1f); }
    }

    public static bool Muted
    {
        get { return PlayerPrefs.GetInt(MutedKey, 0) == 1; }
    }
    // Set the master volume and muted state, saving them to PlayerPrefs so they persist across sessions and can be applied on scene load.
    public static void SetAudioSettings(float volume, bool muted)
    {
        // Settings panel and in-game menu both write to the same PlayerPrefs values.
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, clampedVolume);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplySavedAudioSettings();
    }
    // Apply the saved audio settings to the AudioListener, setting the volume to 0 if muted or to the saved master volume if not muted. This should be called after a scene loads to ensure the settings take effect.
    public static void ApplySavedAudioSettings()
    {
        // Called after scene loads so the saved volume applies everywhere.
        AudioListener.volume = Muted ? 0f : MasterVolume;
    }
}
