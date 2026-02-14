using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class BallDrag : MonoBehaviour
{
    public LayerMask groundLayer;
    public float yOffset = 0.11f;

    public bool PlacementMode = true;

    public bool IsDragging { get; private set; }
    public bool LastPointerDownOnBall { get; private set; }

    [Header("Camera Follow (Only during drag)")]
    public CameraFollow cameraFollow;

    [Header("Drag Tuning")]
    public float dragSmooth = 14f;
    public float maxDragSpeed = 10f;

    [Header("Shoot Block")]
    public float blockShootAfterDrag = 0.25f;
    public float BlockShootUntil { get; private set; }

    private Camera cam;
    private Rigidbody rb;
    private Collider ballCol;

    private Vector3 desiredPos;
    private bool hasDesired;

    private int pointerDownFrame = -1;

    public CameraFollowGoalFocus cameraFocus; 

    void OnEnable()  { EnhancedTouchSupport.Enable(); }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
        ballCol = GetComponent<Collider>();

        desiredPos = transform.position;

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>(true);

        if (cameraFollow != null)
            cameraFollow.SetFollowEnabled(false);
    }

    void Update()
    {
        if (!PlacementMode) return;
        if (Mouse.current == null && Touchscreen.current == null && Touch.activeTouches.Count == 0) return;

        if (PointerDown(out var screenPos))
        {
            LastPointerDownOnBall = IsPointerOnBall(screenPos);

            if (LastPointerDownOnBall && IsDragging)
            {
                StopDrag();
                pointerDownFrame = Time.frameCount;
                return;
            }

            if (LastPointerDownOnBall && !IsDragging)
            {
                StartDrag();
                UnityEngine.Debug.Log("StartDrag called. Enabling camera focus.");
                cameraFocus.focusEnabled = true;
                pointerDownFrame = Time.frameCount;
                return;
            }

            IsDragging = false;
            hasDesired = false;

            if (cameraFollow != null)
                cameraFollow.SetFollowEnabled(false);

            pointerDownFrame = Time.frameCount;
        }

        if (IsDragging && Time.frameCount == pointerDownFrame)
            return;

        if (PointerHeld(out screenPos) && IsDragging)
        {
            // PC üçün “mouse dayananda dayan”
            if (Mouse.current != null)
            {
                Vector2 md = Mouse.current.delta.ReadValue();
                if (md.sqrMagnitude < 0.05f)
                {
                    hasDesired = false;
                    return;
                }
            }

            Ray ray = cam.ScreenPointToRay(screenPos);

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, yOffset, 0f));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                desiredPos = new Vector3(hitPoint.x, yOffset, hitPoint.z);
                hasDesired = true;
            }
        }

        if (PointerUp() && IsDragging)
            StopDrag();
    }

    void FixedUpdate()
    {
        if (!PlacementMode) return;
        if (!IsDragging) return;
        if (!hasDesired) return;

        Vector3 current = rb.position;
        Vector3 to = desiredPos - current;

        float maxStep = maxDragSpeed * Time.fixedDeltaTime;
        Vector3 step = Vector3.ClampMagnitude(to, maxStep);

        Vector3 next = current + step;
        next = Vector3.Lerp(current, next, dragSmooth * Time.fixedDeltaTime);

        rb.MovePosition(next);
    }

    void StartDrag()
    {
        IsDragging = true;

        if (cameraFollow != null)
            cameraFollow.SetFollowEnabled(true);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
        rb.useGravity = false;

        hasDesired = false;
        desiredPos = rb.position;

        BlockShootUntil = Time.time + blockShootAfterDrag;
    }

    void StopDrag()
    {
        IsDragging = false;
        hasDesired = false;

        if (cameraFollow != null)
            cameraFollow.SetFollowEnabled(false);

        BlockShootUntil = Time.time + blockShootAfterDrag;
    }

    bool IsPointerOnBall(Vector2 screenPos)
    {
        if (cam == null || ballCol == null) return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            return hit.collider == ballCol;

        return false;
    }

    bool PointerDown(out Vector2 pos)
    {
        pos = default;

        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                pos = t.screenPosition;
                return true;
            }
        }

        if (Mouse.current?.leftButton.wasPressedThisFrame == true)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    bool PointerHeld(out Vector2 pos)
    {
        pos = default;

        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                t.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                pos = t.screenPosition;
                return true;
            }
        }

        if (Mouse.current?.leftButton.isPressed == true)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    bool PointerUp()
    {
        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            return t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                   t.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
        }

        return Mouse.current?.leftButton.wasReleasedThisFrame == true;
    }
}
