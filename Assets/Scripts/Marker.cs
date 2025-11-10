using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Marker : MonoBehaviour
{
    public static Marker Instance;
    // Marker pickup/drag properties
    Vector3 originalPos;
    Quaternion originalRot;

    // Events
    public UnityEvent successfulStack;
    public UnityEvent attemptMade;

    public bool isHeld;
    public bool isThrown;
    public bool isGrounded;

    private Vector3 grabOffset;
    Plane grabPlane;
    [SerializeField] float dragLerp = 80f;
    [SerializeField] float pickupRot = 20f;
    public float impulseScale = 30f;
    Rigidbody rb;
    Transform originalParent;

    [SerializeField] Transform markerBottomCenter;
    Vector3 lastAngularVelocity;    
    public float curveForce = 0.05f;      // strength of curve
    public float curveDuration = 0.5f;    // how long the curve effect lasts (seconds)
    [SerializeField] AnimationCurve curveFalloff;   // optional curve editor (0–1)
    public float flipForce = 1.5f;
    float throwTime;                                // when throw started

    public float selfAlignmentThreshold = 0.995f;   // how upright *this* marker must be
    public float alignmentThreshold = 0.995f;     // how upright the target must be
    public float velocityThreshold = 1.0f;      // how slow the marker must be to attach

    [SerializeField] float minImpact = 0.5f; 
    [SerializeField] float maxImpact = 12f;
    [SerializeField] AnimationCurve loudnessCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // time dilation properties
    [SerializeField] float freezeDuration = 0.2f;   // how long completely frozen
    [SerializeField] float recoverDuration = 0.8f;  // how long to ramp back
    [SerializeField] float minTimeScale = 0.0f;     // can make this > 0 for slow-mo instead of full stop
    [SerializeField] float minAudio = 0.3f;         // quietest point
    bool isRecovering;

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

    void Update()
    {
        HitDetector();
    }

    private void HitDetector()
    {
        if (!isThrown) return;

        float selfAlignment = Vector3.Dot(transform.up, Vector3.up);
        bool selfUpright = selfAlignment > selfAlignmentThreshold;

        if (!selfUpright)
        {
            SetTimeAndAudioNormal();
            return;
        }

        if (!Physics.Raycast(markerBottomCenter.position, -transform.up, out var hit, 10f))
        {
            if (!isRecovering)
            {
                SetTimeAndAudioNormal();
                return;
            }
        }

        float targetAlignment = Vector3.Dot(hit.normal, Vector3.up);
        bool targetUpright = targetAlignment > alignmentThreshold;
        if (!hit.collider.CompareTag("MarkerTarget") && targetUpright)
        {
            if (!isRecovering) SetTimeAndAudioNormal();
            Debug.DrawLine(markerBottomCenter.position, markerBottomCenter.position + -transform.up * 10f, Color.white, 0.01f);
            return;
        }
        
        // we hit the target
        Debug.DrawLine(markerBottomCenter.position, markerBottomCenter.position + -transform.up * 10f, Color.red, 0.01f);
        Debug.DrawRay(hit.point, hit.normal * 1f, Color.magenta, 0.01f);
        if (isRecovering) return;
        StartCoroutine(FreezeAndRecover());
    }
    

    private void SetTimeAndAudioNormal()
    {
        Time.timeScale = 1f;
        AudioManager.Instance.musicMixer.SetFloat("MyExposedParam", 1f);
    }

    IEnumerator FreezeAndRecover()
    {
        isRecovering = true;
        Time.timeScale = minTimeScale;
        AudioManager.Instance.musicMixer.SetFloat("MyExposedParam", minAudio);

        float t = 0f;
        while (t < freezeDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
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
    }
    
    void LateUpdate()
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

    void ResetRigidBodyMovement()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
    }

    public void ResetMarker()
    {
        Debug.Log("Resetting Marker");
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
        ResetRigidBodyMovement();
        rb.MovePosition(originalPos);
        rb.MoveRotation(originalRot);
    }

    void OnCollisionEnter(Collision collision)
    {
        bool isTarget = collision.collider.CompareTag("MarkerTarget");
        bool hitTop = false;
        return;
        foreach (var contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                hitTop = true;
                break;
            }
        }

        if (isTarget && hitTop)
        {

            float selfAlignment = Vector3.Dot(transform.up, Vector3.up);   
            bool upright = selfAlignment > selfAlignmentThreshold;
            // bool slow    = rb.linearVelocity.magnitude < velocityThreshold;
            // Debug.Log($"linearVelocity {rb.linearVelocity.magnitude} < {velocityThreshold}");
            Debug.Log($"selfAlignment {upright} {selfAlignment} > {selfAlignmentThreshold}");
            // Debug.Log($@"{selfAlignmentThreshold} {slow})");
            if (upright)// && slow)
            {
                Debug.Log("stack success: upright + slow on target top");
                AttachTo(collision.gameObject); 
                isGrounded = true;

                SFXManager.Instance.StopAllSfx();
                SFXManager.Instance.PlayMarkerDropClip(1);
                successfulStack.Invoke();
                return;
            }

            // failed stack attempt on target (too tilted or too fast)
            if (!upright) Debug.Log("stack fail: marker tilted");
            // if (!slow)    Debug.Log("stack fail: too fast");
            isGrounded = true;
            SFXManager.Instance.StopAllSfx();
            SFXManager.Instance.PlayFailClip();
            return;
        }

        // non-target or not the top surface -> treat as normal ground contact
        if (!isGrounded)
        {
            isGrounded = true;
            AudioManager.Instance.ResumeBgm();
        }
        
        float groundImpact = collision.relativeVelocity.magnitude;
        float tg = Mathf.InverseLerp(minImpact, maxImpact, groundImpact);
        SFXManager.Instance.StopAllSfx();
        SFXManager.Instance.PlayMarkerDropClip(Mathf.Clamp01(loudnessCurve.Evaluate(tg)));
    }

    public void AttachTo(GameObject target)
    {
        transform.position = target.GetComponent<TargetMarker>().targetTransform.position;
        ResetRigidBodyMovement();
        rb.isKinematic = true;
        rb.detectCollisions = false;
        transform.position = target.GetComponent<TargetMarker>().targetTransform.position;
        gameObject.isStatic = true;
    }
    
    public void BeginPickup(RaycastHit hit, Camera mainCam)
    {
        isHeld = true;
        ResetRigidBodyMovement();
        Quaternion newRot = new Quaternion(40f, transform.rotation.y, transform.rotation.z, pickupRot);
        transform.rotation = newRot;
        grabOffset = hit.point - transform.position;
        var camFwd = mainCam ? mainCam.transform.forward : Vector3.forward;
        grabPlane = new Plane(-camFwd, hit.point);
    }

    public void WhilePickedUp(Vector2 screenPos, Camera mainCam, float dt)
    {
        if (!isHeld || !mainCam) return;
        var ray = mainCam.ScreenPointToRay(screenPos);
        if (!grabPlane.Raycast(ray, out float dist)) return;

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
        Debug.Log($"Throwing {dir} {power}");
        isHeld = false;
        isThrown = true;
        throwTime = Time.time;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true; 

        rb.AddForce(dir * power * impulseScale, ForceMode.Impulse);
        // Debug.Log($"dir {dir}, dir.x {dir.x}, power {power}, impulseScale");
        // Flip and curve spins
        rb.AddTorque(transform.right * flipForce, ForceMode.Impulse);  // small flip
        rb.AddTorque(transform.up * dir.x * 1f, ForceMode.Impulse); // subtle curve spin

        rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);

        attemptMade?.Invoke();
    }

    public void DebugThrow()
    {
        Quaternion newRot = new Quaternion(40f, transform.rotation.y, transform.rotation.z, pickupRot);
        transform.rotation = newRot;
        isHeld = false;
        isThrown = true;
        throwTime = Time.time;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        var dir = new Vector3(0.00f, 0.18f, 1f);
        rb.AddForce(dir * 1 * impulseScale, ForceMode.Impulse);

        // Flip and curve spins
        rb.AddTorque(transform.right * flipForce, ForceMode.Impulse);  // small flip
        rb.AddTorque(transform.up * dir.x * 1f, ForceMode.Impulse); // subtle curve spin

        rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
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
