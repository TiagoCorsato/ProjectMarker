using UnityEngine;

public class TargetMarker : MonoBehaviour
{
    public Transform targetTransform;

    public static TargetMarker Instance;

    void Awake()
    {
        Instance = this;
        GetComponentInChildren<MeshCollider>(); ;
    }
}
