using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class ObjectBehavior : ColorManager
{
	[Header("Player Object")]
	[SerializeField] public bool isPlayer = false;
    [Header("Green Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float deceleration = 100f;
    [SerializeField] private string moveActionName = "Move";

    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody2D rigidBody2D;
    private Vector2 moveInput;
	
	public int isGrounded = 0;
	public bool IsGrounded(){
		return isGrounded > 0;
	}

    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        rigidBody2D = GetComponent<Rigidbody2D>();
		playerInput.defaultActionMap = "Player";
        moveAction = playerInput.actions[moveActionName];

        if (moveAction == null)
        {
            Debug.LogError($"Move action not found: {moveActionName}", this);
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    protected override void OnRedChanged(ColorState previous)
    {
    }

    protected override void OnOrangeChanged(ColorState previous)
    {
    }

    protected override void OnYellowChanged(ColorState previous)
    {
    }

    protected override void OnGreenChanged(ColorState previous)
    {
    }

    protected override void OnBlueChanged(ColorState previous)
    {
    }

    protected override void OnPurpleChanged(ColorState previous)
    {
    }

    protected override void OnWhiteChanged(ColorState previous)
    {
    }

    protected override void OnColorChanged(ColorState previousColor, ColorState newColor)
    {
        if (previousColor == ColorState.Green && newColor != ColorState.Green)
        {
            moveInput = Vector2.zero;
        }
    }

    protected override void OnRedUpdate()
    {
    }

    protected override void OnOrangeUpdate()
    {
    }

    protected override void OnYellowUpdate()
    {
    }

    protected override void OnGreenUpdate()
    {
        if (moveAction == null)
        {
            return;
        }

        moveInput = moveAction.ReadValue<Vector2>().normalized;
    }

    protected override void OnBlueUpdate()
    {
    }

    protected override void OnPurpleUpdate()
    {
    }

    protected override void OnWhiteUpdate()
    {
    }

    private void FixedUpdate()
    {
        if (rigidBody2D == null || GetColor() != ColorState.Green)
        {
            return;
        }

        Vector2 currentVelocity = rigidBody2D.linearVelocity;
        Vector2 gravityVector = Physics2D.gravity * rigidBody2D.gravityScale;
        float stepAcceleration = acceleration * Time.fixedDeltaTime;
        float stepDeceleration = deceleration * Time.fixedDeltaTime;

        if (gravityVector.sqrMagnitude > 0.0001f)
        {
            Vector2 gravityDirection = gravityVector.normalized;
            float speedAlongGravity = Vector2.Dot(currentVelocity, gravityDirection);
            Vector2 gravityVelocity = gravityDirection * speedAlongGravity;

            Vector2 lateralCurrentVelocity = currentVelocity - gravityVelocity;
            Vector2 lateralInput = moveInput - gravityDirection * Vector2.Dot(moveInput, gravityDirection);
            Vector2 lateralTargetVelocity = lateralInput * moveSpeed;

            float rate = lateralInput.sqrMagnitude > 0.0001f ? stepAcceleration : stepDeceleration;
            Vector2 lateralNewVelocity = Vector2.MoveTowards(lateralCurrentVelocity, lateralTargetVelocity, rate);
            if (lateralNewVelocity.sqrMagnitude > moveSpeed * moveSpeed)
            {
                lateralNewVelocity = lateralNewVelocity.normalized * moveSpeed;
            }

            rigidBody2D.linearVelocity = gravityVelocity + lateralNewVelocity;
            return;
        }

        Vector2 targetVelocity = moveInput * moveSpeed;
        float groundRate = moveInput.sqrMagnitude > 0.0001f ? stepAcceleration : stepDeceleration;
        Vector2 newVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, groundRate);
        rigidBody2D.linearVelocity = Vector2.ClampMagnitude(newVelocity, moveSpeed);
    }

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 6) isGrounded += 1;
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 6) isGrounded -= 1;
	}
}
