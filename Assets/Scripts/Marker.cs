using System.Collections.Generic;
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
        originalPos = transform.position;
        originalRot = transform.rotation;
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
                Debug.Log("Contact point: " + contact.point);

                // Check if we hit the top
                Vector3 normal = contact.normal;
                if (Vector3.Dot(normal, Vector3.up) > 0.5f)
                {
                    Debug.Log("Hit the top surface!");

                    // Check if target is upright
                     float selfAlignment = Vector3.Dot(transform.up, Vector3.up);

                    if (selfAlignment > selfAlignmentThreshold)
                    {
                        // Check if this marker is moving slowly
                        if (rb.linearVelocity.magnitude < velocityThreshold)
                        {
                            Debug.Log("This marker is upright and slow enough — attaching!");
                            //AttachTo(collision.transform, contact.point);
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
        if (!grabPlane.Raycast(ray, out var dist)) return;

        var planeHit = ray.GetPoint(dist);
        var target = planeHit - grabOffset;
        var next = Vector3.Lerp(transform.position, target, dragLerp * dt);
        rb.MovePosition(next);
    }

    public void EndPickup()
    {
        if (!isHeld) return;
        if (!isThrown)
        {
            isHeld = false;
            ResetMarker();
        }
    }

    public void Throw(Vector3 dir, float power)
    {
        Debug.Log($"Throwing {dir} {power}");
        isThrown = true;
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.AddForce(dir.normalized * power * impulseScale, ForceMode.Impulse);
        rb.useGravity = true;
    }

}
