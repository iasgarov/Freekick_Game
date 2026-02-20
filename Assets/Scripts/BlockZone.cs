using UnityEngine;

public class BlockZone : MonoBehaviour
{
    private Vector3 lastAllowedPos;
    private Rigidbody rb;

    [Header("Allowed ground layer (optional)")]
    public LayerMask groundMask;       

    private int blockLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        blockLayer = LayerMask.NameToLayer("BlockZone");
        lastAllowedPos = rb.position;
    }

    void FixedUpdate() => lastAllowedPos = rb.position;

    private void OnCollisionEnter(Collision other)
    {
        int hitLayer = other.gameObject.layer;

        if (hitLayer == LayerMask.NameToLayer("BlockZone"))
        {
            rb.position = lastAllowedPos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

}
