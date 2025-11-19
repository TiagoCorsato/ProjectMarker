using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    [Header("Orbit Camera")]
    public CinemachineCamera orbitVcam; 
    
    [Header("Tuning")]
    public float orbitSpeedDegreesPerSec = 45f;
    public int focusPriority = 20;

    public float rotationSpeed = 120f; // degrees per second
    public int numOfRoations = 3;

    [Header("CameraDefaults")]
    [SerializeField] Vector3 defaultCameraPos;
    [SerializeField] Vector3 defaultCameraRot;

    [SerializeField] CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] CinemachineBrain brain;
    [SerializeField] GameObject followCam;
    [SerializeField] GameObject virtualCam;
    [SerializeField] GameObject closeUpCam;
    
    private void Awake() {
        Instance = this;
    }

    [ExecuteInEditMode]
    void OnEnable() 
    {
        if(brain)
        {
            Debug.Log("Resetting Camera");
            brain.gameObject.transform.position = defaultCameraPos;
            Camera.main.transform.rotation = Quaternion.Euler(defaultCameraRot);
        }
    }

    void Start()
    {
        Controller.Instance.ObjectThrown.AddListener(OnObjectThrown);    
        Marker.Instance.successfulStack.AddListener(OnObjectLanded);

    }

    void OnObjectThrown()
    {
        StartCoroutine(Delay(.1f, ActivateFollowCam));
    }

    void OnObjectLanded()
    {
        StartCoroutine(SpinAround());
    }
    

    void ActivateFollowCam()
    {
        //virtualCam.SetActive(false);
        followCam.SetActive(true);
    }
    
    static IEnumerator Delay(float seconds, System.Action action)
    {
        if (seconds < 0f) seconds = 0f;           // comment: defensive clamp
        yield return new WaitForSeconds(seconds); // comment: uses scaled time
        action.Invoke();
    }

    public float totalRotation = 0f;
    private IEnumerator SpinAround()
    {
        //Debug.Log(totalRotation);
        closeUpCam.SetActive(false);
        followCam.SetActive(true);
        totalRotation = 0f;
        
        while (totalRotation < 360f * numOfRoations && Marker.Instance.isLanded) // full circle
        {
            float delta = rotationSpeed * Time.deltaTime;
            totalRotation += delta;
            orbitalFollow.HorizontalAxis.Value += delta;
            yield return null;
        }
        // Optional: end spin or switch back to normal cam
    }

    public void StopRotation()
    {
        totalRotation = 360f * numOfRoations;
        StopCoroutine(SpinAround());
        orbitalFollow.HorizontalAxis.Value = 0f;
        Debug.Log("Stopped rotation.");
    }

    public void EnableCloseUp()
    {
        Debug.Log("EnableCloseUp");
        closeUpCam.SetActive(true);
    }
    
    public void DisableCloseUp()
    {
        closeUpCam.SetActive(false);
    }
}
