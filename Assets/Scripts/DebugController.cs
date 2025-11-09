using TMPro;
using UnityEngine;
using System.Globalization;
using UnityEngine.UI;

public class DebugController : MonoBehaviour
{
    [SerializeField] TMP_InputField impulseForce;
    [SerializeField] TMP_InputField curveForce;
    [SerializeField] TMP_InputField curveDuration;
    [SerializeField] TMP_InputField flipForce;
    [SerializeField] Button landMarkerButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!impulseForce) return;
        impulseForce.contentType = TMP_InputField.ContentType.DecimalNumber;
        impulseForce.onValueChanged.AddListener(OnImpulseChanged);

        if (!curveForce) return;
        curveForce.contentType = TMP_InputField.ContentType.DecimalNumber;
        curveForce.onValueChanged.AddListener(OnCurveForceChanged);

        if (!curveDuration) return;
        curveDuration.contentType = TMP_InputField.ContentType.DecimalNumber;
        curveDuration.onValueChanged.AddListener(OnCurveDurationChanged);

        if (!flipForce) return;
        flipForce.contentType = TMP_InputField.ContentType.DecimalNumber;
        flipForce.onValueChanged.AddListener(OnFlipForceChanged);

        impulseForce.text = Marker.Instance.impulseScale.ToString();
        curveForce.text = Marker.Instance.curveForce.ToString();
        curveDuration.text = Marker.Instance.curveDuration.ToString();
        flipForce.text = Marker.Instance.flipForce.ToString();

        if (!landMarkerButton) return;
        landMarkerButton.onClick.AddListener(OnLandMarkerClicked);
    }

    void OnDestroy()
    {
        if (impulseForce) impulseForce.onValueChanged.RemoveListener(OnImpulseChanged);
    }

    void OnImpulseChanged(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!Marker.Instance) return;

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return;
        Marker.Instance.SetImpulseForce(f); // comment: public setter
    }

    void OnCurveForceChanged(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!Marker.Instance) return;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return;
        Marker.Instance.SetCurveForce(f);
    }

    void OnCurveDurationChanged(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!Marker.Instance) return;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return;
        Marker.Instance.SetCurveDuration(f);
    }

    void OnFlipForceChanged(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!Marker.Instance) return;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return;
        Marker.Instance.SetFlipForce(f);
    }

    void OnLandMarkerClicked()
    {
        Marker.Instance.DebugLand();
    }
}
