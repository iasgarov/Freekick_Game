using System.Diagnostics;
using UnityEngine;

public class CameraFollowGoalFocus : MonoBehaviour
{
    [Header("Targets")]
    public Transform ball;
    public Transform goal;

    public bool focusEnabled = false;
    public bool freezeCamera = false;
    private Camera cam;

    [Header("Focus Rotation")]
    public float rotateSpeed = 2f;

    [Header("Pan to keep ball visible (NO zoom)")]
    [Range(0f, 0.49f)] public float safeMargin = 0.10f; // kənarlardan təhlükəsiz zona
    public float panSpeed = 6f;                         // kameranın sürüşmə sürəti
    public float maxPanStep = 0.50f;                    // 1 frame-də max nə qədər hərəkət etsin (limit)

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!focusEnabled || ball == null || goal == null || freezeCamera) return;

        // 1) Həmişə qapıya fokus (rotation)
        Quaternion targetRot = Quaternion.LookRotation(goal.position - cam.transform.position, Vector3.up);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRot, Time.deltaTime * rotateSpeed);

        // 2) Top viewport-da haradadır?
        Vector3 vp = cam.WorldToViewportPoint(ball.position);

        // Kamera arxasındadırsa (vp.z < 0) - bu hal üçün də pan edək:
        if (vp.z < 0f)
        {
            // Sadə fix: kameranı bir az geri/yan çəkərək topu qabağa gətirməyə çalış
            // (əgər bu hal səndə olursa, deməli kamera/top arxaya düşür)
            cam.transform.position = Vector3.Lerp(
                cam.transform.position,
                cam.transform.position - cam.transform.forward * 0.5f,
                Time.deltaTime * panSpeed
            );
            return;
        }

        // 3) Top safe zonadan çıxıb?
        float clampedX = Mathf.Clamp(vp.x, safeMargin, 1f - safeMargin);
        float clampedY = Mathf.Clamp(vp.y, safeMargin, 1f - safeMargin);

        bool offScreen =
            vp.x < safeMargin || vp.x > 1f - safeMargin ||
            vp.y < safeMargin || vp.y > 1f - safeMargin;

        if (!offScreen) return;

        // 4) Topun “istədiyimiz” viewport nöqtəsində (clampedX, clampedY) görünməsi üçün
        // həmin depth-də world point hesablayırıq
        Vector3 desiredWorldAtDepth = cam.ViewportToWorldPoint(new Vector3(clampedX, clampedY, vp.z));

        // 5) Kameranı elə sürüşdür ki, ball həmin nöqtəyə “gəlsin”
        Vector3 offset = ball.position - desiredWorldAtDepth;

        // Çox böyük sıçrayış olmasın deyə limitləyirik
        if (offset.magnitude > maxPanStep)
            offset = offset.normalized * maxPanStep;

        cam.transform.position = Vector3.Lerp(cam.transform.position, cam.transform.position + offset, Time.deltaTime * panSpeed);
    }
}
