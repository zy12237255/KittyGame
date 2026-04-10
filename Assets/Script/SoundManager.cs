using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("🎵 Sound Clips")]
    public AudioClip[] dropCatClip;
    public AudioClip backgroundMusic;
    public AudioClip buttonClickClip;
    public AudioClip coinClip;
    public AudioClip winClip;
    public AudioClip loseClip;
    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.clip = backgroundMusic;
        musicSource.volume = 0.5f;
        musicSource.Play();

        LoadSettings();
    }

    private const string KeyMusicVolume = "MusicVolume";
    private const string KeySFXVolume = "SFXVolume";

    /// <summary>
    /// play sfx
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && IsSFXEnabled())
        {
            float finalVolume = GetSFXVolume() * volume;
            sfxSource.PlayOneShot(clip, finalVolume);
        }
    }

    /// <summary>
    /// Plays button click sound.
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickClip);
    }

    /// <summary>
    /// Plays coin sound (e.g. when receiving coin reward on win).
    /// </summary>
    public void PlayCoinSound(float volume = 1f)
    {
        PlaySFX(coinClip, volume);
    }

    /// <summary>
    /// Plays win sound (e.g. when level completed).
    /// </summary>
    public void PlayWin()
    {
        PlaySFX(winClip);
    }

    /// <summary>
    /// Plays lose sound (e.g. when out of time or fail).
    /// </summary>
    public void PlayLose()
    {
        PlaySFX(loseClip);
    }

    /// <summary>
    /// Plays drop cat sound, picks randomly from dropCatClip.
    /// </summary>
    public void PlayDropCatSound(float volume = 1f)
    {
        Debug.Log("PlayDropCatSound");
        if (dropCatClip == null || dropCatClip.Length == 0 || !IsSFXEnabled())
            return;
        int index = UnityEngine.Random.Range(0, dropCatClip.Length);
        AudioClip clip = dropCatClip[index];
        if (clip != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// load sound settings (enabled + volume).
    /// </summary>
    private void LoadSettings()
    {
        SetMusicEnabled(IsMusicEnabled());
        SetSFXEnabled(IsSFXEnabled());
        ApplyMusicVolume(GetMusicVolume());
    }

    /// <summary>
    /// Gets music volume (0-1). Saved in PlayerPrefs.
    /// </summary>
    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(KeyMusicVolume, 1f);
    }

    /// <summary>
    /// Gets SFX volume (0-1). Saved in PlayerPrefs.
    /// </summary>
    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(KeySFXVolume, 1f);
    }

    /// <summary>
    /// Sets music volume (0-1), saves and applies.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KeyMusicVolume, volume);
        PlayerPrefs.Save();
        ApplyMusicVolume(volume);
    }

    /// <summary>
    /// Sets SFX volume (0-1), saves and applies.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KeySFXVolume, volume);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;
    }

    /// <summary>
    /// toggle music
    /// </summary>
    public void ToggleMusic()
    {
        bool isOn = !IsMusicEnabled();
        SetMusicEnabled(isOn);
    }

    /// <summary>
    /// toggle sound
    /// </summary>
    public void ToggleSound()
    {
        bool isOn = !IsSFXEnabled();
        SetSFXEnabled(isOn);
    }

    /// <summary>
    /// toggle vibration
    /// </summary>
    public void ToggleVibration()
    {
        bool isOn = !IsVibrationEnabled();
        SetVibrationEnabled(isOn);
    }

    /// <summary>
    /// check music status
    /// </summary>
    public bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
    }

    /// <summary>
    /// check sound status
    /// </summary>
    public bool IsSFXEnabled()
    {
        return PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
    }

    /// <summary>
    /// check vibration status
    /// </summary>
    public bool IsVibrationEnabled()
    {
        return PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
    }

    /// <summary>
    /// toggle music
    /// </summary>
    private void SetMusic(bool isOn)
    {
        if (musicSource != null)
        {
            musicSource.mute = !isOn;
        }
    }

    /// <summary>
    /// toggle sound
    /// </summary>
    private void SetSound(bool isOn)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = !isOn;
        }
    }

    /// <summary>
    /// toggle vibration
    /// </summary>
    private void SetVibration(bool isOn)
    {
        if (!isOn)
        {
            Handheld.Vibrate();
        }
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        PlayerPrefs.SetInt("MusicEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (musicSource != null)
        {
            musicSource.mute = !isEnabled;
            if (isEnabled)
                musicSource.volume = GetMusicVolume();
        }
    }

    public void SetSFXEnabled(bool isEnabled)
    {
        PlayerPrefs.SetInt("SFXEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (sfxSource != null)
        {
            sfxSource.mute = !isEnabled;
        }
    }

    public void SetVibrationEnabled(bool isEnabled)
    {
        PlayerPrefs.SetInt("VibrationEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (isEnabled)
        {
            Handheld.Vibrate();
        }
    }
}