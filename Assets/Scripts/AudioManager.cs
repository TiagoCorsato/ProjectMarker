using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    public AudioMixerGroup musicGroup;

    [Header("Audio Mixers")]
    public AudioMixer sfxMixer;
    public AudioMixer musicMixer;   
    
    public string musicParam = "BGMVolume";
    public string sfxParam = "SFXVolume";

    [SerializeField] public List<AudioClip> bgmClips;
    public float defaultFadeSeconds = 0.5f;
    AudioSource backgroundMusic;
    Coroutine fadeCoro;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        backgroundMusic = gameObject.AddComponent<AudioSource>();
        backgroundMusic.playOnAwake = false;
        backgroundMusic.loop = true;
        backgroundMusic.volume = .1f;
        backgroundMusic.outputAudioMixerGroup = musicGroup;
    }

    public bool IsPlaying()
    {
        return backgroundMusic != null && backgroundMusic.isPlaying;
    }
    public void PlayBgm(AudioClip clip, float volume01 = .1f, float fadeSec = -1f)
    {
        // comment: defensive checks
        if (clip == null) throw new System.ArgumentNullException(nameof(clip), "bgm clip is null");
        if (volume01 < 0f || volume01 > 1f) throw new System.ArgumentOutOfRangeException(nameof(volume01), "volume must be 0..1");

        StopFadeIfAny();

        if (backgroundMusic.isPlaying)
        {
            StartCoroutine(CrossFade(clip, volume01, fadeSec < 0 ? defaultFadeSeconds : fadeSec));
        }
        else
        {
            backgroundMusic.clip = clip;
            backgroundMusic.volume = 0f;
            backgroundMusic.Play();
            fadeCoro = StartCoroutine(FadeTo(volume01, fadeSec < 0 ? defaultFadeSeconds : fadeSec));
        }
    }

    public void PauseBgm() 
    {
        if (!backgroundMusic.clip) return;
        backgroundMusic.Pause();
    }

    public void ResumeBgm() 
    {
        if (!backgroundMusic.clip) return;
        backgroundMusic.UnPause();
    }

    public void StopBgm(float fadeSec = -1f) 
    {
        if (!backgroundMusic.clip) return;
        StopFadeIfAny();
        float f = fadeSec < 0 ? defaultFadeSeconds : fadeSec;
        fadeCoro = StartCoroutine(FadeTo(0f, f, stopAtEnd:true));
    }
    void StopFadeIfAny() 
    {
        if (fadeCoro != null) { StopCoroutine(fadeCoro); fadeCoro = null; }
    }

    IEnumerator FadeTo(float target, float seconds, bool stopAtEnd = false) {
        float start = backgroundMusic.volume;
        float t = 0f;
        seconds = Mathf.Max(0.0001f, seconds);
        while (t < 1f) {
            t += Time.unscaledDeltaTime / seconds;
            backgroundMusic.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        backgroundMusic.volume = target;
        if (stopAtEnd && target <= 0f) { backgroundMusic.Stop(); backgroundMusic.clip = null; }
        fadeCoro = null;
    }

    IEnumerator CrossFade(AudioClip next, float nextVol, float seconds)
    {
        yield return FadeTo(0f, seconds * 0.5f);
        backgroundMusic.clip = next;
        backgroundMusic.Play();
        yield return (fadeCoro = StartCoroutine(FadeTo(nextVol, seconds * 0.5f)));
    }

    public void MusicVolume(float volume)
    {
        musicMixer.SetFloat(musicParam, Mathf.Log10(volume) * 20);
    }
    
    public void SFXVolume(float volume)
    {
        sfxMixer.SetFloat(sfxParam, Mathf.Log10(volume) * 20);
    }
}
