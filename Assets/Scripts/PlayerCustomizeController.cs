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

    // Lista para guardar renderers
    private Renderer[] playerRenderer;

    private void Awake()
    {
        // Asignación de referencia a la lista de componentes de renderer
        playerRenderer = GetComponentsInChildren<Renderer>();  
    }

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
            RandomizeColor();
        }

        // Tanto el servidor como el cliente aplican el nuevo color
        ApplyColor(playerColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // BUENA PRÁCTICA: Desuscribirse de eventos al destruirse para evitar fugas de memoria (Memory Leaks).
        playerColor.OnValueChanged -= OnPlayerColorChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;
    }

    private void ApplyColor(Color color)
    {
        // Aplicamos el color a todos los renderers del jugador (cuerpo y nariz)
        foreach (Renderer renderer in playerRenderer)
        {
            renderer.material.color = color;
        }
    }

    private void RandomizeColor()
    {
        // Convertimos el color en un nuevo color aleatorio
        playerColor.Value = new Color(Random.Range(0F, 1F), Random.Range(0, 1F), Random.Range(0, 1F));
    }

    [ServerRpc]
    public void SubmitColorChangeServerRpc()
    {
        // El servidor cambia el color del jugador que lo solicita
        RandomizeColor();
    }
}
