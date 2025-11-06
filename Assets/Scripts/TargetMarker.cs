using UnityEngine;

public class TargetMarker : MonoBehaviour
{
    public Transform targetTransform;

    void Awake()
    {
        
        GetComponentInChildren<MeshCollider>(); ;
    }
}
