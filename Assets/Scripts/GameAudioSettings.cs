using UnityEngine;

public static class GameAudioSettings
{
    // Built with AI assistance to keep shared menu layout consistent across scenes.
    // PlayerPrefs key for the saved master volume.
    private const string VolumeKey = "GameAudioSettings.MasterVolume";
    // PlayerPrefs key for the saved mute toggle.
    private const string MutedKey = "GameAudioSettings.Muted";

    // Saved master volume, defaulting to full volume.
    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(VolumeKey, 1f); }
    }

    // Saved mute state.
    public static bool Muted
    {
        get { return PlayerPrefs.GetInt(MutedKey, 0) == 1; }
    }
    // Save and apply the current audio settings.
    public static void SetAudioSettings(float volume, bool muted)
    {
        // Settings panel and in-game menu both write to the same PlayerPrefs values.
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, clampedVolume);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplySavedAudioSettings();
    }
    // Push saved audio settings onto the global AudioListener.
    public static void ApplySavedAudioSettings()
    {
        // Called after scene loads so the saved volume applies everywhere.
        AudioListener.volume = Muted ? 0f : MasterVolume;
    }
}
