using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Orbit Camera")]
    public CinemachineCamera orbitVcam; 
    
    [Header("Tuning")]
    public float orbitSpeedDegreesPerSec = 45f;
    public int focusPriority = 20;

    [Header("CameraDefaults")]
    [SerializeField] Vector3 defaultCameraPos;
    [SerializeField] Vector3 defaultCameraRot;

    [SerializeField] CinemachineBrain brain;
    [SerializeField] GameObject followCam;
    // [SerializeField] CinemachineOrbitalFollow orbital;
    // [SerializeField] CinemachineRotationComposer composer;
    ICinemachineCamera prevActive;
    int prevPriority;
    bool focusing;
    
    [ExecuteInEditMode]
    void OnEnable() 
    {
        if(brain)
        {
            Debug.Log("Resetting Camera");
            brain.enabled = false;
            brain.gameObject.transform.position = defaultCameraPos;
            Camera.main.transform.rotation = Quaternion.Euler(defaultCameraRot);
        }
    }
    void Awake()
    {
        // if (!orbital) Debug.LogError("orbitVcam needs CinemachineOrbitalTransposer");
        // if (!composer) Debug.LogError("orbitVcam needs CinemachineComposer");
        
        // disable input so we can drive rotation ourselves
        // orbital.xAxis.m_InputAxisName = string.Empty;
        // orbital.m_XAxis.m_MaxSpeed = 0f; // we set Value directly
    }
    void Start()
    {
        Controller.Instance.ObjectThrown.AddListener(OnObjectThrown);    
    }

    void OnObjectThrown()
    {
        StartCoroutine(Delay(.1f, ActivateFollowCam));   
    }
    
    void ActivateFollowCam()
    {
        followCam.SetActive(true);
    }
    static IEnumerator Delay(float seconds, System.Action action)
    {
        if (seconds < 0f) seconds = 0f;           // comment: defensive clamp
        yield return new WaitForSeconds(seconds); // comment: uses scaled time
        action.Invoke();
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
