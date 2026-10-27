using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCustomizeController : NetworkBehaviour
{
    // Variable de red para almacenar el color del jugador
    private readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    // Variable de red para almacenar el nombre del jugador
    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "Jugador X",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> playerAmmo = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );  

    // Lista para guardar renderers
    [SerializeField] private Renderer[] bodyRenderers;

    [SerializeField] private GameObject[] ammoVisuals;

    [HideInInspector] public bool reloading = false;

    private void Update()
    {
        if (!IsOwner) return;

        // Lectura del input local en el cliente dueño
        if (Keyboard.current != null)
        {
            // Solicitamos un cambio de color al servidor
            if (Keyboard.current.cKey.wasPressedThisFrame) SubmitColorChangeServerRpc();
        }
    }

    private void OnPlayerColorChanged(Color previousValue, Color newValue)
    {
        // Aplicamos el nuevo color al jugador cuando cambie la variable
        Debug.Log($"El {playerName.Value} ha cambiado de color de {previousValue} a {newValue}");
        ApplyColor(newValue);
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        Debug.Log($"El nombre del cliente {OwnerClientId} ha cambiado a: {newValue}");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1. SUSCRIPCIÓN A EVENTOS DE RED:
        // Suscribimos nuestros métodos para reaccionar cada vez que la variable cambie en red.
        playerColor.OnValueChanged += OnPlayerColorChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;

        // Solo el servidor modifica el nombre y el color del jugador al hacer spawn
        if (IsServer)
        {
            playerName.Value = $"Jugador {OwnerClientId}";
            playerAmmo.Value = ammoVisuals.Length;
            RandomizeColor();
        }

        playerAmmo.OnValueChanged += OnAmmoChanged;
        UpdateAmmoVisuals(playerAmmo.Value);

        // Tanto el servidor como el cliente aplican el nuevo color
        ApplyColor(playerColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // BUENA PRÁCTICA: Desuscribirse de eventos al destruirse para evitar fugas de memoria (Memory Leaks).
        playerColor.OnValueChanged -= OnPlayerColorChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;
        playerAmmo.OnValueChanged -= OnAmmoChanged;
    }

    private void ApplyColor(Color color)
    {
        // Aplicamos el color a todos los renderers del jugador (cuerpo y nariz)
        foreach (Renderer renderer in bodyRenderers)
        {
            renderer.material.color = color;
        }
    }

    private void RandomizeColor()
    {
        // Convertimos el color en un nuevo color aleatorio
        playerColor.Value = new Color(Random.Range(0F, 1F), Random.Range(0, 1F), Random.Range(0, 1F));
    }

    public void WasHit()
    {
        if (IsServer)
            FlashRedClientRpc();
    }

    [ClientRpc]
    private void FlashRedClientRpc()
    {
        StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        Color originalColor = playerColor.Value;

        ApplyColor(Color.red);

        yield return new WaitForSeconds(0.5f);

        ApplyColor(originalColor);
    }

    [ServerRpc]
    public void SubmitColorChangeServerRpc()
    {
        // El servidor cambia el color del jugador que lo solicita
        RandomizeColor();
    }

    private void OnAmmoChanged(int previousValue, int newValue)
    {
        UpdateAmmoVisuals(newValue);
    }

    private void UpdateAmmoVisuals(int ammoLeft)
    {
        for (int i = 0; i < ammoVisuals.Length; i++)
        {
            bool active = i < ammoLeft;

            foreach (Transform child in ammoVisuals[i].transform)
            {
                child.gameObject.SetActive(active);
            }
        }
    }

    [ClientRpc]
    public void ConsumeAmmoVisualClientRpc()
    {
        Debug.Log($"ConsumeAmmoVisual -> Owner:{OwnerClientId} Local:{NetworkManager.Singleton.LocalClientId}");

        if (!IsOwner)
            return;

        ConsumeAmmoServerRpc();
    }

    [ServerRpc]
    private void ConsumeAmmoServerRpc()
    {
        if (reloading)
            return;

        playerAmmo.Value--;

        if (playerAmmo.Value <= 0)
        {
            StartCoroutine(ReloadVisuals());
        }
    }

    private IEnumerator ReloadVisuals()
    {
        reloading = true;

        for (int i = 0; i < ammoVisuals.Length - 1; i++)
        {
            yield return new WaitForSeconds(0.1f);

            playerAmmo.Value++;
        }

        playerAmmo.Value = ammoVisuals.Length;

        reloading = false;
    }
}
