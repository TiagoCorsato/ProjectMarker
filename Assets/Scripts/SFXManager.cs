using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SFXManager : MonoBehaviour
{
    [Header("assign in inspector")]
    public AudioMixerGroup sfxGroup;
    public AudioMixer mixer;
    public string musicVolumeParam = "MusicVolume";
    [SerializeField] List<AudioClip> failClips;
    [SerializeField] List<AudioClip> actionThrowClips;
    [SerializeField] List<AudioClip> throwBGMClips;
    [SerializeField] List<AudioClip> markerDropClips;
    //[SerializeField] AudioClip buttonClip;
    [SerializeField] List<float> throwBGMIdxs;
    [Header("pool")]
    public int poolSize = 8;

    public static SFXManager Instance { get; private set; }

    readonly Queue<AudioSource> pool = new Queue<AudioSource>();
    float defaultMusicDb;
    
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);

        // cache default music db (fallback 0dB if missing)
        defaultMusicDb = 0f;
        if (mixer) mixer.GetFloat(musicVolumeParam, out defaultMusicDb);

        // build pool
        for (int i = 0; i < Mathf.Max(1, poolSize); i++)
        {
            var go = new GameObject($"sfx_{i}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.outputAudioMixerGroup = sfxGroup;
            pool.Enqueue(src);
        }
    }

    public void PlayThrowBGM()
    {
        int clipIdx = UnityEngine.Random.Range(0, throwBGMClips.Count);
        if (clipIdx == 1)
        {
            int startAtIdx = UnityEngine.Random.Range(0, throwBGMIdxs.Count);
            PlayOneShot(throwBGMClips[clipIdx], throwBGMIdxs[startAtIdx]);
            return;
        } 
        PlayOneShot(throwBGMClips[clipIdx]);        
    }

    public void PlayActionThrow()
    {
        PlayOneShot(actionThrowClips[UnityEngine.Random.Range(0, actionThrowClips.Count)]);

    }
    
    public void PlayButtonClip(AudioClip audioClip)
    {
        PlayOneShot(audioClip);        
        
    }

    public void PlayFailClip()
    {
        PlayOneShot(failClips[UnityEngine.Random.Range(0, failClips.Count)]);
    }

    public void PlayMarkerDropClip(float volume01)
    {
        PlayOneShot(markerDropClips[UnityEngine.Random.Range(0, markerDropClips.Count)], 0, volume01);
    }

    public void PlaySuccessClip()
    {
        PlayOneShot(throwBGMClips[1], throwBGMIdxs[1]); 
    }
    public void PlayOneShot(AudioClip clip, bool duckMusic = false, float volume01 = .4f, float pitch = 1f,  float duckDb = -6f, float duckSec = 0.25f)
    {
        if (!clip) throw new System.ArgumentNullException(nameof(clip), "sfx clip is null");
        if (volume01 < 0f || volume01 > 1f) throw new System.ArgumentOutOfRangeException(nameof(volume01));
        var src = Borrow();
        src.transform.position = Vector3.zero;
        ConfigureAndPlay(src, clip, volume01, pitch, duckMusic, duckDb, duckSec);

    }
    public void PlayOneShot(AudioClip clip, float startAtSeconds, float volume01 = .4f, float pitch = 1f, bool duckMusic = false, float duckDb = -6f, float duckSec = 0.25f)
    {
        if (!clip) throw new System.ArgumentNullException(nameof(clip), "sfx clip is null");
        if (volume01 < 0f || volume01 > 1f) throw new System.ArgumentOutOfRangeException(nameof(volume01));
        var src = Borrow();
        src.transform.position = Vector3.zero;
        ConfigureAndPlayAt(src, clip, startAtSeconds, volume01, pitch, duckMusic, duckDb, duckSec);
    }

    public void PlayAt(AudioClip clip, Vector3 worldPos, float volume01 = .4f, float spatialBlend01 = 1f, bool duckMusic = false, float duckDb = -6f, float duckSec = 0.25f)
    {
        if (!clip) throw new System.ArgumentNullException(nameof(clip), "sfx clip is null");
        var src = Borrow();
        src.transform.position = worldPos;
        src.spatialBlend = Mathf.Clamp01(spatialBlend01);
        ConfigureAndPlay(src, clip, volume01, 1f, duckMusic, duckDb, duckSec);
    }

    void ConfigureAndPlayAt(AudioSource src, AudioClip clip, float startAtSeconds, float vol, float pitch, bool duck, float duckDb, float duckSec)
    {
        src.volume = vol;
        src.pitch = pitch;
        src.spatialBlend = 0f;
        src.clip = clip;

        float epsilon = 0.005f;
        float start = Mathf.Clamp(startAtSeconds, 0f, Mathf.Max(0f, clip.length - epsilon));

        src.time = start;
        src.Play();

        StartCoroutine(ReturnWhenDone(src));

        if (duck)
        {
            float remaining = Mathf.Max(0f, clip.length - start);
            StartCoroutine(DuckMusicFor(remaining, duckDb, duckSec));
        }
    }
    
    AudioSource Borrow()
    {
        if (pool.Count == 0) pool.Enqueue(CreateExtra());
        var src = pool.Dequeue();
        return src;
    }

    AudioSource CreateExtra()
    {
        var go = new GameObject("sfx_extra");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.outputAudioMixerGroup = sfxGroup;
        return src;
    }

    void ConfigureAndPlay(AudioSource src, AudioClip clip, float vol, float pitch, bool duck, float duckDb, float duckSec)
    {
        src.volume = vol;
        src.pitch  = pitch;
        src.spatialBlend = 0f; 
        src.clip = clip;
        src.Play();
        StartCoroutine(ReturnWhenDone(src));

        if (duck) StartCoroutine(DuckMusicFor(src.clip.length, duckDb, duckSec));
    }

    IEnumerator ReturnWhenDone(AudioSource src)
    {
        while (src && src.isPlaying) yield return null;
        if (src)
        {
            src.Stop();
            src.clip = null;
            src.spatialBlend = 0f;
            pool.Enqueue(src);
        }
    }

    IEnumerator DuckMusicFor(float duration, float duckDb, float fadeSec)
    {
        if (!mixer) yield break;

        yield return StartCoroutine(FadeMixer(musicVolumeParam, duckDb, fadeSec));
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration - fadeSec * 2f));
        yield return StartCoroutine(FadeMixer(musicVolumeParam, defaultMusicDb, fadeSec));
    }

    IEnumerator FadeMixer(string param, float targetDb, float seconds)
    {
        if (!mixer) yield break;
        mixer.GetFloat(param, out float startDb);
        float t = 0f;
        seconds = Mathf.Max(0.0001f, seconds);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / seconds;
            float db = Mathf.Lerp(startDb, targetDb, t);
            mixer.SetFloat(param, db);
            yield return null;
        }
        mixer.SetFloat(param, targetDb);
    }

    public void StopAllSfx()
    {
        foreach (Transform child in transform)
        {
            var src = child.GetComponent<AudioSource>();
            if (!src) continue;
            src.Stop();
            src.clip = null;
        } 
        pool.Clear();
        foreach (Transform child in transform)
        {
            var src = child.GetComponent<AudioSource>();
            if (!src) continue;
            pool.Enqueue(src);
        }
    }
}
