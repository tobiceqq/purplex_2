using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform modelTransform; // Sem dej vizu�l Sonica (�lov�ka)
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform playerCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private int maxJumps = 2;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Ball Mode (Sonic Spin)")]
    [SerializeField] private GameObject humanVisuals; // Cel� model Sonica (aby �el vypnout)
    [SerializeField] private GameObject ballObject;   // Tv�j model koule (mus� m�t Rigidbody a SphereCollider)
    [SerializeField] private float ballMoveSpeed = 20f;
    [SerializeField] private KeyCode ballToggleKey = KeyCode.LeftShift; // Prom�na na Shift

    private Rigidbody ballRb;
    private Vector3 velocity;
    private int jumpCount;
    private float pitch;

    // Prom�nn�, kterou �te anim�tor (p�id�no IsBallMode)
    public float CurrentMoveAmount { get; private set; }
    public bool IsBallMode { get; private set; }

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        // P��prava koule p�i startu
        if (ballObject != null)
        {
            ballRb = ballObject.GetComponent<Rigidbody>();
            ballObject.SetActive(false); // Na za��tku je koule schovan�
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Tla��tko pro prom�nu
        if (Input.GetKeyDown(ballToggleKey))
        {
            ToggleBallMode();
        }

        // Kamera se h�be v�dycky, a� jsi �lov�k nebo koule
        HandleMouseLook();

        if (IsBallMode)
        {
            HandleBallMovement();
        }
        else
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
        }
    }

    private void ToggleBallMode()
    {
        if (ballObject == null || humanVisuals == null) return;

        IsBallMode = !IsBallMode;

        if (IsBallMode)
        {
            // ZM�NA NA KOULI
            humanVisuals.SetActive(false);
            controller.enabled = false; // Vypneme norm�ln� kolize

            ballObject.transform.position = transform.position + Vector3.up * 0.5f; // Posuneme kouli k hr��i
            ballObject.SetActive(true);

            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // N�VRAT NA �LOV�KA
            transform.position = ballObject.transform.position; // P�esuneme hr��e tam, kam dojela koule

            ballObject.SetActive(false);
            controller.enabled = true; // Zapneme norm�ln� kolize
            humanVisuals.SetActive(true);
        }
    }

    private void HandleBallMovement()
    {
        // Hern� objekt hr��e (a t�m i kamera) neust�le pron�sleduje kouli
        transform.position = ballObject.transform.position;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 cameraForward = playerCamera.forward;
        Vector3 cameraRight = playerCamera.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        // Fyzik�ln� pohyb koule
        if (ballRb != null)
        {
            // Pou��v�me AddForce pro to spr�vn� "kut�len�"
            ballRb.AddForce(moveDirection * ballMoveSpeed * Time.deltaTime, ForceMode.VelocityChange);
        }

        // Vypneme animace b�hu, proto�e model nen� vid�t
        CurrentMoveAmount = 0f;
    }

    // P�VODN� FUNKCE PRO NORM�LN� POHYB Z�ST�VAJ� NEZM�N�NY
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        CurrentMoveAmount = inputDirection.magnitude;

        if (inputDirection.magnitude < 0.1f) return;

        Vector3 cameraForward = playerCamera.forward;
        Vector3 cameraRight = playerCamera.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (modelTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (controller.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = -2f;
            jumpCount = 0;
        }

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCount++;
        }
    }

    private void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}