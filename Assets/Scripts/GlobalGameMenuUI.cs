using UnityEngine;

public static class GlobalGameMenuUI
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    private const string VolumeKey = "GlobalGameMenuUI.MasterVolume";
    private const string MutedKey = "GlobalGameMenuUI.Muted";

    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(VolumeKey, 1f); }
    }

    public static bool Muted
    {
        get { return PlayerPrefs.GetInt(MutedKey, 0) == 1; }
    }

    public static void SetAudioSettings(float volume, bool muted)
    {
        // Settings panel and in-game menu both write to the same PlayerPrefs values.
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, clampedVolume);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplySavedAudioSettings();
    }

    public static void ApplySavedAudioSettings()
    {
        // Called after scene loads so the saved volume applies everywhere.
        AudioListener.volume = Muted ? 0f : MasterVolume;
    }
}
