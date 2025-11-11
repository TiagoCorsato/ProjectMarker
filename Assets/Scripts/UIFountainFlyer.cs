using UnityEngine;
using TMPro;

public class UIFountainFlyer : MonoBehaviour
{
    RectTransform rt;
    TextMeshProUGUI tmp;
    System.Action<UIFountainFlyer> onDone;

    Vector2 vel;
    float grav;
    float life;
    float t;
    bool ready;
    
    void Awake()
    {
        rt  = GetComponent<RectTransform>() ?? GetComponentInParent<RectTransform>();
        tmp = GetComponent<TextMeshProUGUI>() ?? GetComponentInChildren<TextMeshProUGUI>();

        ready = (rt != null && tmp != null);

        if (!ready)
        {
            enabled = false;
            Debug.LogError($"uifountainflyer on {gameObject.name} missing recttransform or textmeshpro");
        }
    }
    public void Initialize(System.Action<UIFountainFlyer> returnToPool)
    {
        onDone = returnToPool;
    }

    public void Launch(RectTransform parentAnchor, string text, Vector2 localStart, Vector2 initialVelocity, float gravity, float lifetime)
    {
        if (!ready) { Debug.LogWarning("launch aborted: flyer not ready"); return; }
        if (!parentAnchor) { Debug.LogWarning("launch aborted: parentAnchor null"); return; }

        transform.SetParent(parentAnchor, false);
        rt.anchoredPosition = localStart;

        tmp.text = string.IsNullOrEmpty(text) ? "..." : text;
        tmp.alpha = 1f;

        vel = initialVelocity;
        grav = gravity;
        life = Mathf.Max(0.05f, lifetime);
        t = 0f;

        gameObject.SetActive(true);
        enabled = true;
    }

    void Update()
    {
        if (!ready) { enabled = false; return; }

        float dt = Time.unscaledDeltaTime;

        vel.y += grav * dt;
        rt.anchoredPosition += vel * dt;

        t += dt;
        tmp.alpha = 1f - Mathf.Clamp01(t / life);

        if (t >= life)
        {
            enabled = false;
            onDone?.Invoke(this);
        }
    }
        void OnDisable()
    {
        if (tmp) tmp.alpha = 0f;
    }
}
