using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    [SerializeField] public InputSystem_Actions playerInputActions;

    public static Controller Instance;

    [SerializeField] private float swipeThreshold = 50f;
    Vector2 startPos;
    public bool bShouldDetectSwipe;

    private void Awake()
    {
        playerInputActions ??= new InputSystem_Actions();
        Instance = this;
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
        playerInputActions.Player.TouchPress.started += OnTouchStarted;
        playerInputActions.Player.TouchPress.canceled += OnTouchEnded;
    }

    private void OnDisable()
    {
        playerInputActions.Player.TouchPress.started -= OnTouchStarted;
        playerInputActions.Player.TouchPress.canceled -= OnTouchEnded;
        playerInputActions.Disable();
    }

    void OnTouchPress(InputAction.CallbackContext context)
    {
        Debug.Log("Touch Pressed");
    }

    void OnTouchStarted(InputAction.CallbackContext context)
    {
        startPos = playerInputActions.Player.TouchPosition.ReadValue<Vector2>();
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!bShouldDetectSwipe) return;
        Debug.Log("Calculating swipe");
        Vector2 endPos = playerInputActions.Player.TouchPosition.ReadValue<Vector2>();
        Vector2 swipe = endPos - startPos;

        if (TryGetWorldThrow(swipe, swipeThreshold, out var dirWorld, out var power))
        {
            
            Marker.Instance.Throw(dirWorld, power);
        }
    }

    private static readonly Vector2[] Dir8Planar =
    {
        new( 1,  0), // Right
        new( 1,  1), // UpRight
        new( 0,  1), // Up
        new(-1,  1), // UpLeft
        new(-1,  0), // Left
        new(-1, -1), // DownLeft
        new( 0, -1), // Down
        new( 1, -1), // DownRight
    };

    private static bool TryGetWorldThrow(Vector2 swipe, float minPixels, out Vector3 dir, out float power01)
    {
        dir = Vector3.zero; power01 = 0f;
        if (swipe.sqrMagnitude < minPixels * minPixels) return false;

        var planar = swipe.normalized;                      // continuous 2d
        dir = PlanarToWorldDir(planar);                    // project to world
        if (dir == Vector3.zero) return false;

        var maxPixels = minPixels * 5f;                    // tune sensitivity
        var clamped = Mathf.Clamp(swipe.magnitude, minPixels, maxPixels);
        power01 = Mathf.InverseLerp(minPixels, maxPixels, clamped);
        return true;
    }

    private static Vector3 PlanarToWorldDir(Vector2 planar)
    {
        var cam = Camera.main;
        if (!cam) return Vector3.zero; // no camera, bail safely

        var right   = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up).normalized;
        var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;

        var v = right * planar.x + forward * planar.y;
        return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.zero;
    }
}
