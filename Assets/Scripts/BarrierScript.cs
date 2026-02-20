using UnityEngine;

public class BarrierScript : MonoBehaviour
{
    public Transform ball;
    public float requiredDistance = 9.15f;

    public void PlaceOnce()
    {
        Vector3 dir = (transform.position - ball.position).normalized;

        UnityEngine.Debug.Log($"Barrier: Placing at {dir}");

        var position = ball.position + dir * 9.15f;
        transform.position = Vector3.Lerp(transform.position, position, requiredDistance);
    }

    void Update()
    {
    }
}
