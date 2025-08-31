using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : SingletonPersistent<MusicManager>
{
    [Header("Audio Mixer Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixer audioMixer;

    [Header("Transition Settings")]
    [Tooltip("Duration of crossfade between clips (used if TrackSO doesn't override).")]
    public float crossfadeDuration = 1.5f;

    private AudioSource musicSource;
    private AudioClip currentClip;

    protected override void Awake()
    {
        base.Awake();
        // Ensure AudioSource
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = false;
        musicSource.playOnAwake = false;
    }

    private void Start()
    {
        // Initialize volumes
        SetVolume("MusicVolume", PrefsManager.Instance.GetFloat(OptionType.MusicVolume));
        SetVolume("SFXVolume", PrefsManager.Instance.GetFloat(OptionType.SfxVolume));
        SetVolume("MasterVolume", PrefsManager.Instance.GetFloat(OptionType.MasterVolume));
    }


    public void SetVolume(string binding, float volumePercentage)
    {
        float decibelValue;

        if (volumePercentage <= 1f)
        {
            float vol = Mathf.Max(volumePercentage, 0.0001f);
            decibelValue = Mathf.Clamp(20f * Mathf.Log10(vol), -80f, 0f);
        }
        else
        {
            decibelValue = Mathf.Lerp(0f, 6f, volumePercentage - 1f);
        }

        audioMixer.SetFloat(binding, decibelValue);
    }
}
