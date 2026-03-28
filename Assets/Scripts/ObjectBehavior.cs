using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class ObjectBehavior : ColorManager
{
    [Header("Green Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private string moveActionName = "Move";

    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody2D rigidBody2D;
    private Vector2 moveInput;

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

        rigidBody2D.linearVelocity += moveInput * moveSpeed * Time.fixedDeltaTime;
    }
}
