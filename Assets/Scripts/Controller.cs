using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Events;
using System;
public class Controller : MonoBehaviour
{
    [SerializeField] InputSystem_Actions inputActions;
    public static Controller Instance;
    public UnityEvent ObjectThrown;
    Vector2 startPos;
    float swipeStartTime;

    // Debugging properties 
    [SerializeField] float rayMax = 50f;
    [SerializeField] float rayDur = 1f;

    private void Awake()
    {
        inputActions ??= new InputSystem_Actions();
        Instance = this;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        EnhancedTouchSupport.Enable();
        inputActions.Player.TouchPress.started += OnTouchStarted;
        inputActions.Player.TouchPress.canceled += OnTouchEnded;
        inputActions.Player.TouchPosition.performed += OnTouchMoved;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        EnhancedTouchSupport.Disable();
        inputActions.Player.TouchPress.started -= OnTouchStarted;
        inputActions.Player.TouchPress.canceled -= OnTouchEnded;
        inputActions.Player.TouchPosition.performed -= OnTouchMoved;
    }

    private void Start() 
    {
        
    }
    void OnTouchStarted(InputAction.CallbackContext context)
    {
        var cam = Camera.main; if (!cam) return;
        var pos = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        startPos = pos;
        swipeStartTime = Time.time;

        var ray = cam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.cyan,  rayDur);
            Debug.DrawRay(hit.point, hit.normal * 0.25f, Color.magenta, rayDur);
            if (hit.transform.CompareTag("ThrowObject")) Marker.Instance.BeginPickup(hit, cam);
        }
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayMax, Color.yellow, rayDur);
    }

    private void OnTouchMoved(InputAction.CallbackContext context)
    {
        var mainCam = Camera.main; if (!mainCam) return;
        Vector2 pos = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        Marker.Instance.WhilePickedUp(pos, mainCam, Time.deltaTime);
    }

    // When touch is released
    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!Marker.Instance.isHeld) return; // if not held, dont do anything
        
        var mainCam = Camera.main;
        if (!mainCam) return;
        Vector2 end = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        Vector2 swipe = end - startPos; // capture the swipe pos. 


        float swipeTime = Mathf.Max(Time.time - swipeStartTime, 0.01f);

        float swipeSpeed = swipe.magnitude / swipeTime;
        // Debug.Log($"Swipe speed: {swipeSpeed}");

        // minimum distance threshold
        if (swipe.magnitude < 30f)
        {
            Marker.Instance.EndPickup(); // release the marker and exit
            return;
        }

        // calculate throw direction and power
        Vector3 dir = TryGetWorldThrow(swipe, mainCam, swipeSpeed);
        float power = SwipePower(swipeSpeed);
        // AudioManager.Instance.PauseBgm();
        SFXManager.Instance.PlayActionThrow();
        // SFXManager.Instance.PlayThrowBGM();
        Marker.Instance.Throw(dir, power); // finally throw the marker
        ObjectThrown.Invoke();
    }

    private static Vector3 TryGetWorldThrow(Vector2 swipe, Camera cam, float swipeSpeed)
    {
        // base forward
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;

        // normalized swipe direction (screen space)
        Vector2 swipeDir = swipe.normalized;

        // influence factor - faster swipes exaggerate the angle a bit
        float influence = Mathf.Clamp01(swipeSpeed / 2000f);

        // add a bit of upward and sideways aim
        Vector3 throwDir =
            forward + up * 0.25f // lower arc bias
            + right * (swipeDir.x * 0.25f)
            + up * (swipeDir.y * 0.4f);

        return throwDir.normalized;
    }

    private static float SwipePower(float swipeSpeed)
    {
        // normalize swipe speed into 0–1 range for power
        float normalized = Mathf.Clamp01(swipeSpeed / 2500f);
        return normalized;
    }
}
