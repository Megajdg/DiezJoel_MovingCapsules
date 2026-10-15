using UnityEngine;
using Unity.Netcode;

public class Bumper : NetworkBehaviour
{
    [SerializeField] private float force = 15f;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();

        if (player != null)
        {
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0f;
            direction.Normalize();

            player.ApplyKnockback(direction * force + Vector3.up * 2f);
        }
    }
}