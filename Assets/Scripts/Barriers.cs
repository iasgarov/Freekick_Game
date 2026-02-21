using UnityEngine;

public class Barriers : MonoBehaviour
{
    [Header("Refs")]
    public Transform ball;
    public Transform goal;                 // goalCenter

    [Header("Placement")]
    public float distanceFromBall = 9.15f;
    public LayerMask groundMask;           // stadion/field layer-ini seç
    public float rayHeight = 10f;          // yuxarıdan atmaq üçün
    public float groundExtraOffset = 0.02f; // yerə “otursun” deyə azca

    [Header("Rotation Fix")]
    public float yawOffset = 90f;          // 0/90/-90/180 test et

    [Header("State")]
    public bool followEnabled = true;      // DRAG zamanı true, SHOOT zamanı false

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        // Fizika baryeri “aşağı salmasın”
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    void LateUpdate()
    {
        if (!followEnabled || !ball || !goal) return;

        // 1) Ball->Goal istiqaməti
        Vector3 dir = goal.position - ball.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // 2) XZ mövqe
        Vector3 targetPos = ball.position + dir * distanceFromBall;

        // 3) Yerə oturt (raycast down)
        float y = transform.position.y; // fallback
        Vector3 rayOrigin = new Vector3(targetPos.x, targetPos.y + rayHeight, targetPos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayHeight * 2f, groundMask))
        {
            float halfHeight = 0.5f;
            if (col != null) halfHeight = col.bounds.extents.y;

            y = hit.point.y + halfHeight + groundExtraOffset;
        }

        transform.position = new Vector3(targetPos.x, y, targetPos.z);

        // 4) Düz dayanma (model axis fərqi üçün offset)
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, yawOffset, 0f);

        transform.rotation = rot;
    }

    // SHOOT zamanı çağır
    public void FreezeBarrier()
    {
        followEnabled = false;
        transform.parent = null; // hər ehtimala qarşı: topun child-i olmasın
    }
}
