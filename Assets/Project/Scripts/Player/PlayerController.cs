using UnityEngine;
using System.Collections; 

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform modelTransform;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 25f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private int maxJumps = 2;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Ball Mode")]
    [SerializeField] private GameObject humanVisuals;
    [SerializeField] private GameObject ballObject;
    [SerializeField] private float ballMoveSpeed = 20f;
    [SerializeField] private KeyCode ballToggleKey = KeyCode.LeftShift;

    [Header("Transformation Effects")]
    [SerializeField] private ParticleSystem transformationEffect; 
    [SerializeField] private float visualDelay = 0.15f; 

    private Rigidbody ballRb;
    private Vector3 velocity;
    private int jumpCount;
    private float pitch;
    private bool isTransforming = false; 

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

        if (ballObject != null)
        {
            ballRb = ballObject.GetComponent<Rigidbody>();
            ballObject.SetActive(false);
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

        if (Input.GetKeyDown(ballToggleKey) && !isTransforming)
        {
            StartCoroutine(ToggleBallModeRoutine());
        }

        HandleMouseLook();

        if (IsBallMode)
        {
            HandleBallMovement();
        }
        else if (controller.enabled) 
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
        }
    }



    private IEnumerator ToggleBallModeRoutine()
    {
        isTransforming = true; 

       
        if (transformationEffect != null)
        {
            Vector3 effectPos = IsBallMode ? ballObject.transform.position : transform.position + Vector3.up * 0.5f;
            transformationEffect.transform.position = effectPos;
            transformationEffect.Play();
        }

        IsBallMode = !IsBallMode;

        Renderer[] ballRenderers = ballObject.GetComponentsInChildren<Renderer>();

        if (IsBallMode)
        {
           
            humanVisuals.SetActive(false);
            controller.enabled = false;

            ballObject.transform.position = transform.position + Vector3.up * 0.5f;
            ballObject.SetActive(true); 

            
            foreach (var r in ballRenderers) r.enabled = false;

            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }

            yield return new WaitForSeconds(visualDelay);

            foreach (var r in ballRenderers) r.enabled = true;
        }
        else
        {
           
            foreach (var r in ballRenderers) r.enabled = false; 

            
            yield return new WaitForSeconds(visualDelay);

            transform.position = ballObject.transform.position;
            ballObject.SetActive(false);
            controller.enabled = true;
            humanVisuals.SetActive(true); 

           
            foreach (var r in ballRenderers) r.enabled = true;
        }

        isTransforming = false; 
    }

    private void HandleBallMovement()
    {
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

        if (ballRb != null)
        {
            ballRb.AddForce(moveDirection * ballMoveSpeed * Time.deltaTime, ForceMode.VelocityChange);
        }

        CurrentMoveAmount = 0f;
    }

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
            if (velocity.y < 0f)
                velocity.y = -2f;

            jumpCount = 0;
        }

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (jumpCount == 0)
            {
                animationController?.PlayJump();
            }

            jumpCount++;
        }
    }

    private void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
