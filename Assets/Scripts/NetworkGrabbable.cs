using Unity.Netcode;
using UnityEngine;

public class NetworkGrabbable : NetworkBehaviour
{
    public NetworkVariable<bool> IsGrabbed =
    new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;
    private Collider col;

    [SerializeField] private GameObject pickupCanvas;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        col = GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        if (pickupCanvas == null)
            return;

        pickupCanvas.transform.position =
            transform.position + Vector3.up * 0.5f;
    }

    [ClientRpc]
    public void GrabClientRpc()
    {
        if (IsServer)
            IsGrabbed.Value = true;

        rb.isKinematic = true;
        col.enabled = false;
    }

    public void Release()
    {
        if (IsServer)
            IsGrabbed.Value = false;

        NetworkObject.TryRemoveParent();

        rb.isKinematic = false;
        col.enabled = true;
    }

    public void ShowPrompt(bool value)
    {
        pickupCanvas.SetActive(value);
    }
}