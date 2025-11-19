using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Marker : MonoBehaviour
{
    public static Marker Instance;

    // runtime transforms
    Vector3 originalPos;
    Quaternion originalRot;

    [Header("Events")]
    public UnityEvent successfulStack;
    public UnityEvent attemptMade;

    [Header("States")]
    public bool isHeld;
    public bool isThrown;
    public bool isGrounded;
    public bool selfUpright;
    public bool isRecovering;
    public bool hasResolvedThrow;
    public bool isLanded;

    [Header("Pickup & Drag")]
    [SerializeField] float dragLerp = 80f;
    [SerializeField] float pickupRot = 20f;
    Vector3 grabOffset;
    Plane grabPlane;
    public float impulseScale = 30f;

    [Header("Components & References")]
    [SerializeField] Transform markerBottomCenter;
    Rigidbody rb;
    Transform originalParent;

    [Header("Debug")]
    [SerializeField] Vector3 debugThrow;

    [Header("Throw Properties")]
    [SerializeField] AnimationCurve curveFalloff; // optional curve editor (0–1)
    public float curveForce = 0.05f;             // strength of curve
    public float curveDuration = 0.5f;           // how long the curve effect lasts (seconds)
    public float flipForce = 1.5f;
    float throwTime;                             // when throw started
    float minAirTime = 0.05f;

    [Header("Alignment & Hit Detection")]
    [SerializeField] float rayMaxDistance = 0.3f;
    public float selfAlignmentThreshold = 0.995f; // how upright this marker must be
    public float alignmentThreshold = 0.995f; // how upright the target must be
    public float velocityThreshold = 1.0f;   // how slow the marker must be to attach
    public LayerMask targetLayer;
    [SerializeField] float snapRadius = 0.03f;     // distance for a perfect stack
    [SerializeField] float nearMissRadius = 0.12f; // distance for a near miss (must be > snapRadius)
    [SerializeField] float settleSpeed = 0.5f;
    [SerializeField] float speed = 0.0f;
    float bestCenterDistance;

    [Header("Impact Loudness")]
    [SerializeField] float minImpact = 0.5f;
    [SerializeField] float maxImpact = 12f;
    [SerializeField] AnimationCurve loudnessCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Time Dilation")]
    [SerializeField] float freezeDuration = 0.2f;  // how long completely frozen
    [SerializeField] float recoverDuration = 0.8f; // how long to ramp back
    [SerializeField] float minTimeScale = 0.0f;    // > 0 for slow-mo instead of full stop
    [SerializeField] float minAudio = 0.3f;        // quietest point

    public void SetImpulseForce(float force) => impulseScale = force;
    public void SetCurveForce(float force) => curveForce = force;
    public void SetCurveDuration(float duration) => curveDuration = duration;
    public void SetFlipForce(float force) => flipForce = force;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        originalPos = transform.position;
        originalRot = transform.rotation;

        if (curveFalloff == null || curveFalloff.length == 0)
            curveFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }

    void LateUpdate()
    {
        HitDetector();

        // if (!isThrown || hasResolvedThrow) return;
        // if (Time.time - throwTime < minAirTime) return;

        // speed = rb.linearVelocity.magnitude;
        // // Debug.Log($"speed {speed}, settleSpeed {settleSpeed}, linearVel {rb.linearVelocity}");
        // if (speed > settleSpeed) return;

        // ResolveThrowResult();
    }
    
    void FixedUpdate()
    {
        if (!isThrown) return;
        float elapsed = Time.time - throwTime;
        if (elapsed > curveDuration) return; // stop applying after duration

        // How strong the curve is right now (fades out)
        float t = elapsed / curveDuration;
        float fade = curveFalloff.Evaluate(t); // 1 → 0 over time

        // Cross product simulates lift from spin
        Vector3 liftDir = Vector3.Cross(rb.angularVelocity, rb.linearVelocity);
        rb.AddForce(liftDir * curveForce * fade, ForceMode.Force);

    }
    private void HitDetector()
    {
        RaycastHit hit;
        if (!isThrown) return; 

        float selfAlignment = Vector3.Dot(transform.up, Vector3.up); selfUpright = selfAlignment > selfAlignmentThreshold;
        // if its not vertical 
        if (!selfUpright) { SetTimeAndAudioNormal(); return; }
        
        // if its not hitting anything 
        if (!Physics.Raycast(markerBottomCenter.position, -markerBottomCenter.up, out hit, rayMaxDistance, targetLayer))
        {
            Debug.DrawLine(markerBottomCenter.position, hit.point * rayMaxDistance, Color.blue, 6f);
            if (!isRecovering) SetTimeAndAudioNormal();
            return;
        }

        bool onTargetLayer = (targetLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        if (!onTargetLayer)
        {
            if (!isRecovering) SetTimeAndAudioNormal();
            Debug.Log("hit something, but NOT on targetLayer"); return;
        }
        Debug.Log($"raycast hit {hit.collider.name} on layer {hit.collider.gameObject.layer}, distance {hit.distance}");

        Vector2 markerXZ = new Vector2(transform.position.x, transform.position.z);

        TargetMarker tm = hit.collider.GetComponentInParent<TargetMarker>();
        if (tm == null || tm.targetTransform == null) return;

        Vector2 targetXZ = new Vector2(
            tm.targetTransform.position.x,
            tm.targetTransform.position.z
        );

        float centerDistance = Vector2.Distance(markerXZ, targetXZ);

        Debug.Log($"centerDistance = {centerDistance}, snapRadius = {snapRadius}, nearMissRadius = {nearMissRadius}");
        // perfect stack
        if (centerDistance <= snapRadius)
        {
            Debug.Log("stack success: upright + close enough");
            AttachTo(hit.collider.gameObject);
            isGrounded = true;
            isLanded = true;
            SFXManager.Instance.StopAllSfx();
            SFXManager.Instance.PlayMarkerDropClip(1);
            successfulStack.Invoke();
            Debug.DrawRay(hit.point, hit.normal * 1f, Color.magenta, 15f);
            if (isRecovering) return;

            CameraController.Instance.EnableCloseUp();
            StartCoroutine(FreezeAndRecover()); return;
        }
        
        // near miss -> slow-mo fail (no snap) 
        if (centerDistance <= nearMissRadius)
        {
            Debug.Log("near miss: upright but offset (slow-mo fail)");
            isGrounded = true;
            SFXManager.Instance.StopAllSfx();
            SFXManager.Instance.PlayFailClip();
            if (!isRecovering) 
            { 
                CameraController.Instance.EnableCloseUp(); 
                StartCoroutine(FreezeAndRecover()); 
            }
            return;
        }
        // too far -> normal land, no slow - mo 
        Debug.Log("wide miss: treat as normal ground contact"); isGrounded = true; if (!isRecovering) SetTimeAndAudioNormal();
    }

    public void TestSlowMo()
    {
        CameraController.Instance.EnableCloseUp(); 
        StartCoroutine(FreezeAndRecover()); 
    }

    private void SetTimeAndAudioNormal()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.musicMixer.SetFloat("MyExposedParam", 1f);
    }

    IEnumerator FreezeAndRecover()
    {
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        isRecovering = true;
        Time.timeScale = minTimeScale;
        AudioManager.Instance.musicMixer.SetFloat("MyExposedParam", minAudio);

        float t = 0.01f;
        while (t < freezeDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0.01f;
        while (t < recoverDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / recoverDuration);

            float currentTimeScale = Mathf.Lerp(minTimeScale, 1f, alpha);
            float currentAudio = Mathf.Lerp(minAudio, 1f, alpha);

            Time.timeScale = currentTimeScale;
            AudioManager.Instance.musicMixer.SetFloat("MyExposedParam", currentAudio);

            yield return null;
        }

        SetTimeAndAudioNormal();
        isRecovering = false;
        CameraController.Instance.DisableCloseUp();
    }


    void ResetRigidBodyMovement()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
    }

    public void ResetMarker()
    {
        rb.interpolation = RigidbodyInterpolation.None;
        //Debug.Log("Resetting Marker");
        Time.timeScale = 1f;
        AudioManager.Instance.musicMixer.SetFloat("MyExposedParam", 1f);
        if (!AudioManager.Instance.IsPlaying())
        {
            AudioManager.Instance.PlayBgm(AudioManager.Instance.bgmClips[0], .1f);
        }
        AudioManager.Instance.ResumeBgm();
        SFXManager.Instance.StopAllSfx();
        gameObject.isStatic = false;
        rb.isKinematic = false;
        rb.detectCollisions = true;
        isHeld = false;
        isThrown = false;
        isGrounded = false;
        isLanded = false;
        speed = 0f;
        ResetRigidBodyMovement();
        rb.MovePosition(originalPos);
        rb.MoveRotation(originalRot);
        CameraController.Instance.StopRotation();
        StopCoroutine(FreezeAndRecover());
    }

    void OnCollisionEnter(Collision collision)
    {
        float groundImpact = collision.relativeVelocity.magnitude;
        float tg = Mathf.InverseLerp(minImpact, maxImpact, groundImpact);
        SFXManager.Instance.StopAllSfx();
        SFXManager.Instance.PlayMarkerDropClip(Mathf.Clamp01(loudnessCurve.Evaluate(tg)));
    }

    public void AttachTo(GameObject target)
    {
        SetTimeAndAudioNormal();
        var marker = target.GetComponent<TargetMarker>()
            ?? target.GetComponentInParent<TargetMarker>()
            ?? target.GetComponentInChildren<TargetMarker>();

        if (marker == null)
        {
            Debug.LogError($"attachTo: no TargetMarker found on {target.name} or its hierarchy");
            return;
        }

        if (marker.targetTransform == null)
        {
            Debug.LogError($"attachTo: TargetMarker on {marker.gameObject.name} has no targetTransform assigned");
            return;
        }

        ResetRigidBodyMovement();
        rb.isKinematic = true;
        rb.detectCollisions = false;

        transform.position = marker.targetTransform.position;
        transform.rotation = marker.targetTransform.rotation;
        // transform.SetParent(marker.targetTransform);
        gameObject.isStatic = true;
    }

    public void BeginPickup(RaycastHit hit, Camera mainCam)
    {
        isHeld = true;
        ResetRigidBodyMovement();
        grabOffset = hit.point - transform.position;
        var camFwd = mainCam ? mainCam.transform.forward : Vector3.forward;
        grabPlane = new Plane(-camFwd, hit.point);
    }

    public void WhilePickedUp(Vector2 screenPos, Camera mainCam, float dt)
    {
        if (!isHeld || !mainCam) return;
        var ray = mainCam.ScreenPointToRay(screenPos);
        if (!grabPlane.Raycast(ray, out float dist)) return;

        Quaternion newRot = new Quaternion(40f, transform.rotation.y, transform.rotation.z, pickupRot);
        transform.rotation = newRot;
        Vector3 planeHit = ray.GetPoint(dist);
        Vector3 target = planeHit - grabOffset;
        Vector3 next = Vector3.Lerp(transform.position, target, dragLerp * dt);
        rb.MovePosition(next);
    }

    public void EndPickup()
    {
        if (!isHeld) return;
        isHeld = false;
        ResetMarker();
    }

    public void Throw(Vector3 dir, float power)
    {
        //Debug.Log($"Throwing {dir} {power}");

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.detectCollisions = true;

        isHeld = false;
        isThrown = true;
        hasResolvedThrow = false;
        bestCenterDistance = float.MaxValue;
        throwTime = Time.time;
        attemptMade?.Invoke();

        var forceDir = dir.normalized;
        rb.AddForce(forceDir * power * impulseScale, ForceMode.Impulse);
        rb.AddTorque(transform.right * flipForce, ForceMode.Impulse);
        rb.AddTorque(transform.up * dir.x * 1f, ForceMode.Impulse);
        rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
    }

    public void DebugThrow()
    {
        Quaternion newRot = new Quaternion(40f, transform.rotation.y, transform.rotation.z, pickupRot);
        transform.rotation = newRot;
        Throw(debugThrow, 1f);
        Controller.Instance.ObjectThrown.Invoke();
    }
    public void DebugLand()
    {
        Debug.Log("stack success: upright + slow on target top");
        AttachTo(TargetMarker.Instance.gameObject);
        isGrounded = true;

        SFXManager.Instance.StopAllSfx();
        SFXManager.Instance.PlayMarkerDropClip(1);
        successfulStack.Invoke();
        return;
    }
}
