using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Marker : MonoBehaviour
{
    Rigidbody rb;
    public static Marker Instance;
    Vector3 originalPos;
    Quaternion originalRot;
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

}
