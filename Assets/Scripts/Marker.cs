using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Marker : MonoBehaviour
{
    Rigidbody rb;
    public static Marker Instance;
    Vector3 originalLoc;

    void Awake()
    {
        Instance = this;
        originalLoc = transform.position;
        Debug.Log($"position {originalLoc}");
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void ResetMarker()
    {
        transform.position = originalLoc;
        Debug.Log($"position {originalLoc}");
        
        rb.useGravity = false;
    }

    public void Throw(Vector3 dir, float power)
    {
        Debug.Log($"Throwing {dir} {power}");
        rb.AddForce(dir * power * 30, ForceMode.Impulse);
        rb.useGravity = true;
    }
    
}
