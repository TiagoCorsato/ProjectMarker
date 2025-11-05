using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class Controller : MonoBehaviour
{
    [SerializeField] public InputSystem_Actions inputActions;
    public static Controller Instance;

    Vector2 startPos;

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
        var cam = Camera.main; if (!cam) return;
        var pos = inputActions.Player.TouchPosition.ReadValue<Vector2>();
        var ray = cam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.cyan,  rayDur);
            Debug.DrawRay(hit.point, hit.normal * 0.25f, Color.magenta, rayDur);
            if (hit.transform.CompareTag("ThrowObject")) Marker.Instance.BeginPickup(hit, cam);
            return;
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

        const float minPixels = 50f; // probably threshold we want to use as minimum so theres no ugly swipes
        float powerMaxPx = minPixels * 5f; // max power based on pixel swipe

        if (!IsForwardSwipe(swipe, mainCam, minPixels, 0.5f)) // if the swipe is not going forward (player assist)
        {
            Marker.Instance.EndPickup(); // release the marker and exit
            return;
        }
        if (!IsSwipeStrongEnough()) //  check how strong the swipe was (time treshold from when they swiped, how fast they swiped, etc)
        {

            return; // if it was not a very strong swipe, per the treshold, we should release the marker and exit
        }

        // if we get here, we have a forward and strong enough swipe, we should throw the marker
        var dir = TryGetWorldThrow(swipe, mainCam); 
        var power = SwipePower(new Vector2(), new Vector2()); // this function should take into account the swipe time, speed, etc so it can calculate the power of the throw
        power = Mathf.Pow(power, 0.75f); 

        Marker.Instance.Throw(dir, power); // finally throw the marker
    }

    private static bool IsForwardSwipe(Vector2 swipe, Camera cam, float minPixels, float minCosAngle)
    {
        return false;
    }

    private static Vector3 TryGetWorldThrow(Vector2 swipe, Camera mainCam)
    {
        return new Vector3();
    }

    private static float SwipePower(Vector2 swipe, Vector2 swipeTime)
    {
        return 0f;
    }

    private static bool IsSwipeStrongEnough()
    {
        return false;
    }

    void OnDrawGizmos()
    {
       
    }
}
