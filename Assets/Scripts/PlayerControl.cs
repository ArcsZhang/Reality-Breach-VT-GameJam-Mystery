using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private LayerMask groundLayer = 1 << 6;
    [SerializeField] private Collider2D groundCheckTrigger;

    [Header("Input")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Rigidbody2D rigidBody2D;
    private float moveInputX;
    private float verticalVelocity;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isGrounded;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        rigidBody2D.gravityScale = 0f;

        if (groundCheckTrigger == null)
        {
            Transform groundCheckTransform = transform.Find("GroundCheck");
            if (groundCheckTransform != null)
            {
                groundCheckTrigger = groundCheckTransform.GetComponent<Collider2D>();
            }
        }

        if (groundCheckTrigger == null)
        {
            Debug.LogError("GroundCheck trigger collider is missing. Assign child GroundCheck BoxCollider2D.", this);
        }
        else if (!groundCheckTrigger.isTrigger)
        {
            Debug.LogWarning("GroundCheck collider should be set as Trigger.", groundCheckTrigger);
        }

        moveAction = playerInput.actions[moveActionName];
        jumpAction = playerInput.actions[jumpActionName];

        if (moveAction == null)
        {
            Debug.LogError($"Move action not found: {moveActionName}", this);
        }

        if (jumpAction == null)
        {
            Debug.LogError($"Jump action not found: {jumpActionName}", this);
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }

    private void Update()
    {
        if (moveAction == null || jumpAction == null || groundCheckTrigger == null)
        {
            return;
        }

        moveInputX = moveAction.ReadValue<Vector2>().x;
        isGrounded = CheckGrounded();

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rigidBody2D.linearVelocity;
        velocity.x = moveInputX * moveSpeed;
        velocity.y = verticalVelocity;
        rigidBody2D.linearVelocity = velocity;
    }

    private bool CheckGrounded()
    {
        Bounds bounds = groundCheckTrigger.bounds;
        Vector2 center = bounds.center;
        Vector2 size = bounds.size;
        return Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckTrigger == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Bounds bounds = groundCheckTrigger.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

	
}
