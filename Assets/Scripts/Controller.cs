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
    public float arcThrowPower = 0.5f;

    public float peakSwipeSpeed;

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

    void OnTouchStarted(InputAction.CallbackContext context)
    {
        var cam = Camera.main; 
        if (!cam) return;
        
        var pos = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        startPos = pos;
        swipeStartTime = Time.time;
        peakSwipeSpeed = 0f; // NEW

        var ray = cam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.cyan, rayDur);
            Debug.DrawRay(hit.point, hit.normal * 0.25f, Color.magenta, rayDur);
            if (hit.transform.CompareTag("ThrowObject")) 
                Marker.Instance.BeginPickup(hit, cam);
        }
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayMax, Color.yellow, rayDur);
    }

    private void OnTouchMoved(InputAction.CallbackContext context)
    {
        var mainCam = Camera.main; 
        if (!mainCam) return;
        
        Vector2 pos = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        Marker.Instance.WhilePickedUp(pos, mainCam, Time.deltaTime);

        // Show trajectory preview while dragging
        if (Marker.Instance.isHeld)
        {
            Vector2 currentSwipe = pos - startPos;
            
            // Only show trajectory if there's meaningful swipe distance
            if (currentSwipe.magnitude > 30f)
            {
                // float currentSwipeTime = Mathf.Max(Time.time - swipeStartTime, 0.01f);
                // float currentSwipeSpeed = currentSwipe.magnitude / currentSwipeTime;
                
                // Vector3 throwDir = TryGetWorldThrow(currentSwipe, mainCam, currentSwipeSpeed);
                // float throwPower = SwipePower(currentSwipeSpeed);
                
                // Marker.Instance.ShowLine(throwDir, throwPower);

                float currentSwipeTime = Mathf.Max(Time.time - swipeStartTime, 0.01f);
                float currentSwipeSpeed = currentSwipe.magnitude / currentSwipeTime;

                // store peak speed
                peakSwipeSpeed = Mathf.Max(peakSwipeSpeed, currentSwipeSpeed);

                Vector3 throwDir = TryGetWorldThrow(currentSwipe, mainCam, peakSwipeSpeed);
                float throwPower = SwipePower(peakSwipeSpeed);

                Marker.Instance.ShowLine(throwDir, throwPower);
            }
            else
            {
                Marker.Instance.HideLine();
            }
        }
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
        
        // Minimum distance threshold
        if (swipe.magnitude < 30f && swipeTime < 0.2f)
        {
            Marker.Instance.EndPickup(); // release the marker and exit
            return;
        }

        // Calculate throw direction and power
        // Vector3 dir = TryGetWorldThrow(swipe, mainCam, swipeSpeed);
        // float power = SwipePower(swipeSpeed);

        float power = SwipePower(peakSwipeSpeed);
        Vector3 dir = TryGetWorldThrow(swipe, mainCam, peakSwipeSpeed);
        
        SFXManager.Instance.PlayActionThrow();
        Marker.Instance.Throw(dir, power); // finally throw the marker
        ObjectThrown.Invoke();
    }

    private Vector3 TryGetWorldThrow(Vector2 swipe, Camera cam, float swipeSpeed)
    {
        // base forward
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;

        // normalized swipe direction (screen space)
        Vector2 swipeDir = swipe.normalized;

        // add a bit of upward and sideways aim
        Vector3 throwDir =
            forward + up * arcThrowPower // arc throw
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