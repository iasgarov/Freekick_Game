using System.Diagnostics;
using UnityEngine;

public class CameraFollowGoalFocus : MonoBehaviour
{
    [Header("Targets")]
    public Transform ball;
    public Transform goal;

    public bool focusEnabled = false;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }


    void LateUpdate()
    {
        if (focusEnabled)
        {
            UnityEngine.Debug.Log($"CameraFocus: Focusing on goal.{cam.transform.position}");
            UnityEngine.Debug.Log($"CameraFocus: Focusing on goal.{ball.transform.position}");
            Quaternion target = Quaternion.LookRotation(goal.position - cam.transform.position);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, target, Time.deltaTime * 2f);
        }
    }
}
