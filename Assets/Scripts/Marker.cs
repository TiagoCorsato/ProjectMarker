using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Marker : MonoBehaviour
{
    public static Marker Instance;

    // Marker pickup/drag properties
    Vector3 originalPos;
    Quaternion originalRot;

    public bool isHeld;
    public bool isThrown;
    private Vector3 grabOffset;
    Plane grabPlane;
    [SerializeField] float dragLerp = 80f;

    [SerializeField] float pickupRot = 20f;

    [SerializeField] float impulseScale = 30f;
    Rigidbody rb;
    Transform originalParent;

    Vector3 lastAngularVelocity;    
    [SerializeField] float curveForce = 0.05f;      // strength of curve
    [SerializeField] float curveDuration = 0.5f;    // how long the curve effect lasts (seconds)
    [SerializeField] AnimationCurve curveFalloff;   // optional curve editor (0–1)
    float throwTime;                                // when throw started


    public float selfAlignmentThreshold = 0.9f;   // how upright *this* marker must be
    public float alignmentThreshold = 0.9f;     // how upright the target must be
    public float velocityThreshold = 1.0f;      // how slow the marker must be to attach

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
        gameObject.isStatic = false;
        rb.isKinematic = false;
        rb.detectCollisions = true;        
        isHeld = false;
        isThrown = false;
        ResetRigidBodyMovement();
        rb.MovePosition(originalPos);
        rb.MoveRotation(originalRot);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("MarkerTarget"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                //Debug.Log("Contact point: " + contact.point);

                // Check if we hit the top
                Vector3 normal = contact.normal;
                if (Vector3.Dot(normal, Vector3.up) > 0.5f)
                {
                    Debug.Log("Hit the top surface!");

                    // Check if target is upright
                    float selfAlignment = Vector3.Dot(transform.up, Vector3.up);

                    if (selfAlignment > selfAlignmentThreshold)
                    {
                            AttachTo(collision.gameObject);
                        // Check if this marker is moving slowly
                        if (rb.linearVelocity.magnitude < velocityThreshold)
                        {
                            Debug.Log("This marker is upright and slow enough — attaching!");
                            return;
                        }
                        else
                        {
                            Debug.Log("Too fast to attach!");
                        }
                    }
                    else
                    {
                        Debug.Log("Marker is tilted, not aligned!");
                    }
                }
            }
        }
    }

    void AttachTo(GameObject target)
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

        // Flip and curve spins
        rb.AddTorque(transform.right * flipForce, ForceMode.Impulse);  // small flip
        rb.AddTorque(transform.up * dir.x * 1f, ForceMode.Impulse); // subtle curve spin

        rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
    }

    public float flipForce = 1.5f;
}
