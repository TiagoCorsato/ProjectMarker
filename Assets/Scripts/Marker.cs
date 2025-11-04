using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Marker : MonoBehaviour
{
    Rigidbody rb;
    public static Marker Instance;
    Vector3 originalPos;
    Quaternion originalRot;
    Transform originalParent;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        originalParent = transform.parent;
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    public void ResetMarker()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        rb.MovePosition(originalPos);
        rb.MoveRotation(originalRot);
        // rb.Sleep();
    }

    public void Throw(Vector3 dir, float power)
    {
        Debug.Log($"Throwing {dir} {power}");
        Vector3 pos = new Vector3(dir.x, .5f, dir.z);
        rb.AddForce(pos * power * 30, ForceMode.Impulse);
        rb.useGravity = true;
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
            // Snap to position
        }
    }
}
