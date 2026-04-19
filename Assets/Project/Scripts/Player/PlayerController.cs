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

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private int maxJumps = 1;
    [SerializeField] private float gravity = -20f;
    

    [Header("Climbing")]
    public bool canClimb = false;
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] public float climbSpeed = 4f;
    [SerializeField] public float maxClimbTime = 2.5f;
    [SerializeField] public float detectionLength = 0.7f;
    [SerializeField] public float sphereCastRadius = 0.3f;
    [SerializeField] public float maxWallLookAngle = 35f;
    [SerializeField] public float wallJumpBackForce = 7f;
    [SerializeField] public float wallJumpUpForce = 8f;

    [Header("Ball Mode")]
    [SerializeField] private GameObject humanVisuals;
    [SerializeField] private GameObject ballObject;
    [SerializeField] private float ballMoveSpeed = 20f;
    [SerializeField] private KeyCode ballToggleKey = KeyCode.LeftShift;
    

    [Header("HyperRoll Dash")]
    public float dashForce = 50f;
    public float dashCooldown = 1f;
    private float dashTimer;
    public bool isDashing = false;

    [Header("Transformation Effects")]
    [SerializeField] private ParticleSystem transformationEffect;
    [SerializeField] private float visualDelay = 0.15f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem ballTrailSystem;
    [SerializeField] private GameObject trailRendererObject;
    [SerializeField] private ParticleSystem jumpVFXSystem; 

    private Rigidbody ballRb;
    private Vector3 velocity;
    private int jumpCount;
    private float pitch;
    private bool isTransforming = false;
    private float climbTimer;
    private RaycastHit frontWallHit;
    private bool wallFront;
    private float wallLookAngle;

    public float CurrentMoveAmount { get; private set; }
    public bool IsBallMode { get; private set; }
    public bool IsClimbing { get; private set; }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
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
        climbTimer = maxClimbTime;
    }

    private void Update()
    {
        if (Input.GetKeyDown(ballToggleKey) && !isTransforming)
        {
            StartCoroutine(ToggleBallModeRoutine());
        }

        HandleMouseLook();

        if (IsBallMode)
        {
            HandleBallMovement();
            HandleDash();
            if (Input.GetButtonDown("Jump"))
            {
                if (Mathf.Abs(ballRb.linearVelocity.y) < 0.1f)
                    ballRb.AddForce(Vector3.up * jumpHeight * 2f, ForceMode.Impulse);
            }
        }

        else if (controller.enabled)
        {
            WallCheck();
            if (IsClimbing) HandleClimbingMovement();
            else
            {
                HandleMovement();
                HandleGravity();
            }
            HandleJump();
        }
    }

    private void WallCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        wallFront = Physics.SphereCast(rayOrigin, sphereCastRadius, modelTransform.forward, out frontWallHit, detectionLength, whatIsWall);

        if (wallFront) wallLookAngle = Vector3.Angle(modelTransform.forward, -frontWallHit.normal);

        if (controller.isGrounded) climbTimer = maxClimbTime;

        bool isTryingToClimb = wallFront && Input.GetAxisRaw("Vertical") > 0 && wallLookAngle < maxWallLookAngle && !controller.isGrounded;
        IsClimbing = (isTryingToClimb && climbTimer > 0 && canClimb);
    }

    private void HandleClimbingMovement()
    {
        velocity.y = 0;
        controller.Move(Vector3.up * climbSpeed * Time.deltaTime);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        CurrentMoveAmount = inputDirection.magnitude;

        if (inputDirection.magnitude > 0.1f)
        {
            velocity.x = 0;
            velocity.z = 0;
        }

        Vector3 moveDirection = (playerCamera.forward * vertical + playerCamera.right * horizontal);
        moveDirection.y = 0;
        moveDirection.Normalize();

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

        if (Input.GetButtonDown("Jump"))
        {
            if (IsClimbing)
            {
                IsClimbing = false;
                velocity.y = wallJumpUpForce;
                Vector3 bounceDir = frontWallHit.normal * wallJumpBackForce;
                velocity.x = bounceDir.x;
                velocity.z = bounceDir.z;
                animationController?.PlayJump();

                if (jumpVFXSystem != null)
                {
                    jumpVFXSystem.Play(true);
                }
            }
            else if (jumpCount < maxJumps)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (jumpCount == 0) animationController?.PlayJump();

                if (jumpVFXSystem != null)
                {
                    jumpVFXSystem.Play(true);
                }

                jumpCount++;
            }
        }
    }

    private void HandleGravity()
    {
        if (IsClimbing) return;

        velocity.x = Mathf.MoveTowards(velocity.x, 0, Time.deltaTime * 2f);
        velocity.z = Mathf.MoveTowards(velocity.z, 0, Time.deltaTime * 2f);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator ToggleBallModeRoutine()
    {

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && !stats.hasHyperRoll && !IsBallMode)
        {
            Debug.Log("Ještì nemáš HyperRoll!");
            yield break;
        }

        isTransforming = true;
        if (transformationEffect != null)
        {
            transformationEffect.transform.position = transform.position + Vector3.up * 0.5f;
            transformationEffect.Play();
        }

        IsBallMode = !IsBallMode;

        if (IsBallMode)
        {
            // AKTIVACE VFX
            if (ballTrailSystem != null) ballTrailSystem.Play();
            if (trailRendererObject != null)
            {
                trailRendererObject.SetActive(true);
                // Vymaže starou èáru, aby se netáhla z minula
                var tr = trailRendererObject.GetComponent<TrailRenderer>();
                if (tr != null) tr.Clear();
            }

            humanVisuals.SetActive(false);
            controller.enabled = false;
            ballObject.transform.position = transform.position + Vector3.up * 0.5f;
            ballObject.SetActive(true);
        }
        else
        {
            // DEAKTIVACE VFX
            if (ballTrailSystem != null) ballTrailSystem.Stop();
            if (trailRendererObject != null) trailRendererObject.SetActive(false);

            yield return new WaitForSeconds(visualDelay);
            transform.position = ballObject.transform.position;
            ballObject.SetActive(false);
            controller.enabled = true;
            humanVisuals.SetActive(true);
        }
        isTransforming = false;
    }

    void HandleDash()
    {
        if (IsBallMode && Input.GetMouseButtonDown(0) && Time.time > dashTimer)
        {
            Rigidbody rb = GetComponentInChildren<Rigidbody>(); // Najde RB koule
            if (rb != null)
            {
                // Odpálí kouli smìrem, kam se dívá kamera nebo kam se hýbeš
                Vector3 dashDir = playerCamera.forward;
                rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

                isDashing = true;
                dashTimer = Time.time + dashCooldown;

                // Po malé chvíli vypneme isDashing stav
                Invoke("ResetDash", 0.5f);
            }
        }
    }

    void ResetDash() { isDashing = false; }

    private void HandleBallMovement()
    {
        transform.position = ballObject.transform.position;
        Vector3 moveDirection = (playerCamera.forward * Input.GetAxisRaw("Vertical") + playerCamera.right * Input.GetAxisRaw("Horizontal"));
        moveDirection.y = 0;
        if (ballRb != null) ballRb.AddForce(moveDirection.normalized * ballMoveSpeed * Time.deltaTime, ForceMode.VelocityChange);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, mouseX, 0f);
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void UnlockDoubleJump()
    {
        maxJumps = 2; 
        Debug.Log("Dvojskok odemknut!");
    }

    public void UnlockClimbing()
    {
        canClimb = true;
        climbSpeed = 4f; 
        Debug.Log("Lezení odemknuto!");
    }
}