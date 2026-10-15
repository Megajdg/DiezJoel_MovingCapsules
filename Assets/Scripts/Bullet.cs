using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 1f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Invoke(nameof(DestroyBullet), lifeTime);
    }

    private void DestroyBullet()
    {
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerCustomizeController player = collision.gameObject.GetComponent<PlayerCustomizeController>();

        if (player != null)
            player.WasHit();

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}