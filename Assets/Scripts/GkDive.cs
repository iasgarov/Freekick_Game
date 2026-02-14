using UnityEngine;
using UnityEngine.InputSystem;

public class GkDive : MonoBehaviour
{
    private Animator anim;

    void Awake() => anim = GetComponent<Animator>();

    void Update()
    {
          if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            anim.SetTrigger("DiveLeft");
    }
}
