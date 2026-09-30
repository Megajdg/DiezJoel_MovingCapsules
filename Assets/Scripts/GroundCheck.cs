using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    // Variable para comprobar si estamos tocando el suelo o no
    public bool isGrounded;

    // Al tocar suelo, la ponemos en true
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
            isGrounded = true;
    }

    // Al dejar de tocar suelo, la ponemos en false
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
            isGrounded = false;
    }
}
