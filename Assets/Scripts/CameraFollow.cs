using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Auto target")]
    public string ballTag = "Ball";
    public Transform target;

    [Header("Follow")]
    public float followSpeed = 10f;
    public bool smoothFollow = true;

    private Vector3 offset;
    private Quaternion rotOffset;
    private bool initialized;

    private bool followEnabled; // ✅ default OFF

    void Start()
    {
        if (target == null)
        {
            var ball = GameObject.FindGameObjectWithTag(ballTag);
            if (ball != null) target = ball.transform;
        }

        if (!target) return;

        // ✅ Oyun startında hazır məsafəni saxla
        offset = transform.position - target.position;
        rotOffset = transform.rotation;
        initialized = true;

        // ✅ başlanğıcda kamera sabit
        followEnabled = false;
    }

    void LateUpdate()
    {
        if (!initialized || !target || !followEnabled) return;

        Vector3 desired = target.position + offset;

        if (smoothFollow)
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        else
            transform.position = desired;

        transform.rotation = rotOffset;
    }

    public void SetFollowEnabled(bool enabled)
    {
        followEnabled = enabled;

        // ✅ drag başlayanda “jump” olmasın deyə dərhal düzgün yerə gətir
        if (enabled && target != null)
            transform.position = target.position + offset;
    }
}
