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

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "MarkerTarget")
        {
            Debug.Log("Hit Marker Target");
            // Check angle
            // Check hit point
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            float contactHeight = contactPoint.y - transform.position.y;
            if (contactHeight > 1f)
            {

            }
            // Snap to position
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
