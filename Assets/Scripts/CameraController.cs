using UnityEngine;

public class CameraController : MonoBehaviour
{
  
    void Start()
    {
        Controller.Instance.ObjectThrown.AddListener(OnObjectThrown);    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnObjectThrown()
    {
        Vector3 position = Marker.Instance.transform.position;
    }
    
    // void InitializeCamera()
    // {
    //     camInstance = Instantiate(mainCam, transform.position, Quaternion.identity);
    //     cinemachineInstance = Instantiate(cinemachineCam, transform.position, Quaternion.identity);

    //     camInstance.transform.SetParent(transform, true);
    //     cinemachineInstance.transform.SetParent(transform, true);

    //     CameraTarget target = new CameraTarget();
    //     target.TrackingTarget = lookAtTarget;
    //     cinemachineInstance.GetComponent<CinemachineCamera>().Target = target;

    //     cameraMainTransform = camInstance.transform;

    //     // Radar Camera
    //     GameObject radarCamInstance = Instantiate(radarCam, transform.position, radarCam.transform.rotation);
    //     radarCamInstance.transform.SetParent(transform, true);
    //     radarCamInstance.transform.position = new Vector3(0f, 22f, 0f);
    // }
}
