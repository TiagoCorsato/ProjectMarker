using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject menuUI;
    public GameObject settingsUI;
    public GameObject gameHUD;
    public TextMeshProUGUI attemptCountText;
    public Slider musicSlider;
    public Slider sfxSlider;
    [SerializeField] RectTransform spawnAnchor;    
    [SerializeField] TextMeshProUGUI textPrefab; 
    [SerializeField] List<string> lines;   

    [Header("pool & tuning")]
    [SerializeField, Min(1)] int poolSize = 32;
    [SerializeField] Vector2 initialSpeedX = new Vector2(-120f, 120f);
    [SerializeField] Vector2 initialSpeedY = new Vector2(200f, 350f);
    [SerializeField] float gravity = -600f;
    [SerializeField] Vector2 lifetime = new Vector2(0.8f, 1.4f);

    readonly Queue<UIFountainFlyer> pool = new Queue<UIFountainFlyer>();
    int nextLine;

    public static UIController Instance;

    void Awake()
    {
        Instance = this;

        if (!spawnAnchor) throw new System.InvalidOperationException("spawnAnchor not assigned");
        if (!textPrefab) throw new System.InvalidOperationException("textPrefab not assigned");
        for (int i = 0; i < poolSize; i++) pool.Enqueue(CreateFlyer());
    }

    void Start()
    {
        OnMenuClicked();
        AudioManager.Instance.PlayBgm(AudioManager.Instance.bgmClips[0], .1f);  
        Marker.Instance.successfulStack.AddListener(OnSuccess);
    }

    void OnSuccess()
    {
        Burst(50);
        AudioManager.Instance.StopBgm();
        SFXManager.Instance.PlaySuccessClip();
    }

    UIFountainFlyer CreateFlyer()
    {
        var go = Instantiate(textPrefab, spawnAnchor).gameObject; 
        var flyer = go.GetComponent<UIFountainFlyer>() ?? go.AddComponent<UIFountainFlyer>();
        flyer.Initialize(ReturnToPool);
        go.SetActive(false);
        return flyer;
    }

    UIFountainFlyer Borrow()
    {
        if (pool.Count == 0) pool.Enqueue(CreateFlyer());
        return pool.Dequeue();
    }

    void ReturnToPool(UIFountainFlyer f)
    {
        if (!f) return;
        f.gameObject.SetActive(false);
        pool.Enqueue(f);
    }

    public void Burst(int count)
    {
        if (lines == null || lines.Count == 0) throw new System.InvalidOperationException("no lines to display");
        if (count <= 0) return;

        var rect = (RectTransform)spawnAnchor;
        for (int i = 0; i < count; i++)
        {
            var f = Borrow();
            var txt = NextLine();
            var startPos = Vector2.zero;
            var vx = Random.Range(initialSpeedX.x, initialSpeedX.y);
            var vy = Random.Range(initialSpeedY.x, initialSpeedY.y);
            var life = Random.Range(lifetime.x, lifetime.y);
            f.Launch(rect, txt, startPos, new Vector2(vx, vy), gravity, life);
        }
    }

    string NextLine()
    {
        if (nextLine >= lines.Count) nextLine = 0;
        return lines[nextLine++];
    }

    public void OnStartClicked()
    {
        GameManager.Instance.PauseState(false);
        menuUI.SetActive(false);
        gameHUD.SetActive(true);
    }

    public void OnMenuClicked()
    {
        GameManager.Instance.PauseState(true);
        menuUI.SetActive(true);
        gameHUD.SetActive(false);
        settingsUI.SetActive(false);
    }

    public void OnSettingsClicked()
    {
        settingsUI.SetActive(!settingsUI.activeSelf);
        menuUI.SetActive(!settingsUI.activeSelf);
    }

    public void UpdateAttemptCounter(int NumOfAttempts)
    {
        attemptCountText.text = $"Attempts: \n{NumOfAttempts}";
    }

    public void OnMusicVolumeChanged()
    {
        AudioManager.Instance.MusicVolume(musicSlider.value);
    }

    public void OnSFXVolumeChanged()
    {
        AudioManager.Instance.SFXVolume(sfxSlider.value);
    }
}
