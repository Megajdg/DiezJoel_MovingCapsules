using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float rotationSpeed = 1000f;
    [SerializeField] private GroundCheck groundCheck;

    // Vector de input enviado por el cliente y almacenado en el Servidor
    private Vector2 inputVector;
    // Booleano de salto enviado por el cliente y almacenado en el Servidor
    private bool jumpInput;

    // Referencia al componente de Físicas
    private Rigidbody rb;

    // Referencia al componente de Audio
    private AudioSource jumpSound;

    private void Awake()
    {
        // Asignación de referencia al componente de físicas y de audio
        rb = GetComponent<Rigidbody>();
        jumpSound = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // REGLA CLAVE DE FÍSICAS EN RED:
        // En el Servidor, el Rigidbody es DINÁMICO (isKinematic = false) para simular físicas.
        // En los Clientes, el Rigidbody es CINEMÁTICO (isKinematic = true) para que la física local del cliente
        // no pelee con las posiciones recibidas vía NetworkTransform desde el Servidor.
        if (IsServer)
            rb.isKinematic = false;
        else
            rb.isKinematic = true;
    }

    private void Update()
    {
        // REGLA DE ORO DE NETCODE: Solo el cliente dueño (IsOwner) lee sus propias entradas del teclado.
        if (!IsOwner) return;

        // 1. Lectura del Input local en el Cliente Dueño

        float h = 0f;
        float v = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.IsPressed() || Keyboard.current.leftArrowKey.IsPressed()) h -= 1f;
            if (Keyboard.current.dKey.IsPressed() || Keyboard.current.rightArrowKey.IsPressed()) h += 1f;
            if (Keyboard.current.sKey.IsPressed() || Keyboard.current.downArrowKey.IsPressed()) v -= 1f;
            if (Keyboard.current.wKey.IsPressed() || Keyboard.current.upArrowKey.IsPressed()) v += 1f;
            // Solicitamos saltar al Servidor
            if (Keyboard.current.spaceKey.wasPressedThisFrame && groundCheck.isGrounded) SubmitJumpServerRpc();
        }

        Vector2 input = new Vector2(h, v).normalized;

        // 2. Enviar la entrada de control al Servidor mediante un ServerRpc
        SubmitInputServerRpc(input);
    }

    private void FixedUpdate()
    {
        // REGLA DE ORO DE NETCODE: Solo el SERVIDOR calcula y aplica cambios de físicas autoritativos.
        if (!IsServer) return;

        // 3. Aplicar la velocidad en el Servidor usando Rigidbody
        Vector3 moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        // Conservar la velocidad vertical existente (gravedad/caídas) y modificar X/Z
        Vector3 targetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // Comprobamos si el jugador se ha movido
        if (moveDirection.sqrMagnitude != 0)
        {
            // Creamos una rotación con la dirección del usuario y la movemos desde la rotación actual hasta la deseada
            Quaternion lookingRotation = Quaternion.LookRotation(moveDirection);
            lookingRotation = Quaternion.RotateTowards(rb.rotation, lookingRotation, rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(lookingRotation);
        }

        // Añadimos un impulso si el jugador lo ha solicitado y si está tocando el suelo
        if (jumpInput && groundCheck.isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            SendJumpSoundClientRpc();
            jumpInput = false;
        }
    }

    /// <summary>
    /// ServerRpc: Método ejecutado EXCLUSIVAMENTE en el SERVIDOR tras ser invocado por el CLIENTE.
    /// El sufijo 'ServerRpc' y el atributo [ServerRpc] son obligatorios en Unity Netcode for GameObjects.
    /// </summary>
    [ServerRpc]
    private void SubmitInputServerRpc(Vector2 input)
    {
        // El Servidor recibe la dirección pedida por el cliente
        inputVector = input;
    }


    [ServerRpc]
    private void SubmitJumpServerRpc()
    {
        // El Servidor hace que el cliente salte
        jumpInput = true;
    }

    [ClientRpc]
    private void SendJumpSoundClientRpc()
    {
        // El Servidor manda a todos los clientes el sonido de salto del cliente que ha saltado
        jumpSound.Play();
    }
}