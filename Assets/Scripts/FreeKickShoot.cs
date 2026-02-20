using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DrawTrajectoryAndShoot : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Rigidbody rb;

    [Header("Raycast")]
    public LayerMask rayMask = ~0;
    public float rayDistance = 500f;

    [Header("Draw")]
    public float minPointDistance = 0.3f;

    [Header("Shot")]
    public float exitVelocity = 18f;      // vuranda sürət
    public float upwardLift = 0.06f;      // azca yuxarı (0.03-0.10 test et)

    [Header("Curve (Spin)")]
    public bool enableCurve = true;
    public float maxCurve = 1.0f;        // 0..1 arası curve miqdarı
    public float spinStrength = 14f;     // curve üçün spin gücü
    public int sampleCurveFromPoints = 8; // curve hesabını neçə nöqtədən götürsün

    [Header("Detection")]
    public Transform goalCenter;
    public Animator animator;

    [Header("Magnus (Curve Physics)")]
    public bool useMagnus = true;
    public float magnusStrength = 0.25f;   // 0.15 - 0.6 arası test et
    public float magnusMax = 25f;          // force limit


    private readonly List<Vector3> rawPath = new();

    private bool drawing;
    private BallDrag ballDrag;

    void Start()
    {
        ballDrag = GetComponent<BallDrag>();
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!rb) rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;

        if (!animator)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (ballDrag != null && ballDrag.PlacementMode)
        {
            if (PointerDown(out var firstPos))
            {
                // ✅ click topun üstündədirsə placementdən çıxma
                if (ballDrag.IsPointerOnBallPublic(firstPos))   // aşağıda necə edəcəyik
                    return;

                // ✅ topun üstündə deyilsə aim mode-a keç
                ballDrag.PlacementMode = false;
            }
        }


        if (ballDrag != null && ballDrag.IsDragging) return;

        // Start draw
        if (PointerDown(out var pos))
        {
            rb.isKinematic = true;
            rb.useGravity = false;

            drawing = true;
            rawPath.Clear();
            AddPoint(pos);
        }

        // Draw
        if (drawing && PointerHeld(out pos))
        {
            AddPoint(pos);
        }

        // Release -> shoot
        if (drawing && PointerUp())
        {
            drawing = false;

            if (rawPath.Count < 2) return;

            Shoot_BaseDir_FirstSegment_AndCurve();
        }
    }

    void FixedUpdate()
    {
        if (!useMagnus) return;
        if (rb == null) return;
        if (rb.isKinematic) return;

        // yalnız havada təsir etsin istəyirsənsə:
        // if (Physics.Raycast(rb.position, Vector3.down, 0.2f)) return;

        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.5f) return;

        // spin vektoru (rad/s)
        Vector3 w = rb.angularVelocity;

        // Magnus force ~ w x v
        Vector3 magnus = Vector3.Cross(w, v) * magnusStrength;

        // limitlə (birdən uçub getməsin)
        if (magnus.magnitude > magnusMax)
            magnus = magnus.normalized * magnusMax;

        rb.AddForce(magnus, ForceMode.Acceleration);
    }


    // ===================== CORE: base dir + curve =====================

    void Shoot_BaseDir_FirstSegment_AndCurve()
    {
        // 1) Base direction = ilk xətt (ilk 2 nöqtə)
        Vector3 baseDir = (rawPath[1] - rawPath[0]);
        baseDir.y = 0f;

        if (baseDir.sqrMagnitude < 0.0001f)
            return;

        baseDir.Normalize();

        // 2) Curve = sonrakı sağ-sol sapma (limitli)
        float curve = 0f;
        if (enableCurve && rawPath.Count >= 3)
            curve = ComputeCurveAmount(baseDir);

        curve = Mathf.Clamp(curve, -maxCurve, maxCurve);

        // 3) Physics
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 shotDir = (baseDir + Vector3.up * upwardLift).normalized;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(shotDir * exitVelocity, ForceMode.VelocityChange);


        // 4) Curve üçün spin (Y oxu ətrafında)
        // curve > 0 => sağ curve, curve < 0 => sol curve
        // Spin magnitude limitlidir -> birdən dönmə olmaz
        if (enableCurve)
        {
            rb.angularVelocity = Vector3.up * (curve * spinStrength);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    float ComputeCurveAmount(Vector3 baseDir)
    {
        // baseDir-ə perpendikulyar right vektor
        Vector3 right = Vector3.Cross(Vector3.up, baseDir).normalized;

        // baseDir boyunca “irəliləyiş” və right boyunca “kənara sapma” ölçülür
        // Bu sapma baseDir boyunca nə qədər getdiyinə bölünür => normallaşmış curve
        int n = rawPath.Count;
        int start = 1;
        int end = Mathf.Min(n - 1, start + sampleCurveFromPoints);

        float sideSum = 0f;
        float forwardSum = 0.0001f;

        for (int i = start; i < end; i++)
        {
            Vector3 d = rawPath[i] - rawPath[i - 1];
            d.y = 0f;

            float side = Vector3.Dot(d, right);
            float forward = Vector3.Dot(d, baseDir);

            sideSum += side;
            forwardSum += Mathf.Abs(forward);
        }

        // side/forward -> curve miqdarı (normalize)
        float curve = sideSum / forwardSum;

        // çox böyük çıxmasın deyə yumşaldırıq
        return Mathf.Clamp(curve, -1.5f, 1.5f);
    }

    // ===================== DRAW =====================

    void AddPoint(Vector2 screenPos)
    {
        if (!TryGetWorldPoint(screenPos, out var world)) return;

        world.y = rb.position.y;

        if (rawPath.Count == 0 || Vector3.Distance(rawPath[^1], world) >= minPointDistance)
            rawPath.Add(world);
    }

    // ===================== INPUT =====================

    bool TryGetWorldPoint(Vector2 screenPos, out Vector3 world)
    {
        world = default;
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, rayMask))
        {
            world = hit.point;
            return true;
        }

        return false;
    }

    bool PointerDown(out Vector2 pos)
    {
        pos = default;

        if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
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

        if (Touchscreen.current?.primaryTouch.press.isPressed == true)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
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
        return Touchscreen.current?.primaryTouch.press.wasReleasedThisFrame == true
            || Mouse.current?.leftButton.wasReleasedThisFrame == true;
    }
}