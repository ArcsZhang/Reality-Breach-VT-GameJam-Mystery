using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class EnemyBehavior : ColorManager
{
	[Tooltip("Any layer that should obstruct line of site.")]
	public LayerMask obstacleLayer;
	
	[Header("Red Movement")]
	[SerializeField] private float redAcceleration = 15f;
	
	[Header("Green Movement")]
	[SerializeField] private float moveSpeed = 6f;
	[SerializeField] private float acceleration = 80f;
	[SerializeField] private float deceleration = 100f;
	[SerializeField] private string moveActionName = "Move";

	private PlayerInput playerInput;
	private InputAction moveAction;
	private Rigidbody2D rigidBody2D;
	private Transform player;
	private Portal portal;
	private Vector2 moveInput;
	public bool hasDied;

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

	private void Start()
	{
		LevelManager manager = LevelManager.Instance;
		if (manager != null)
		{
			if (manager.TryGetPortal(out Portal resolvedPortal))
			{
				portal = resolvedPortal;
			}

			if (manager.TryGetPlayer(out ObjectBehavior playerBehavior))
			{
				player = playerBehavior.transform;
			}
		}

		if (portal == null)
		{
			portal = FindAnyObjectByType<Portal>();
		}

		if (player == null)
		{
			ObjectBehavior[] allObjects = FindObjectsByType<ObjectBehavior>();
			for (int i = 0; i < allObjects.Length; i++)
			{
				if (allObjects[i].isPlayer)
				{
					player = allObjects[i].transform;
					break;
				}
			}
		}

		if (player == null)
		{
			Debug.LogWarning("[EnemyBehavior] No player with ObjectBehavior.isPlayer was found.", this);
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
        if (player == null || player.GetComponent<ObjectBehavior>() == null || player.GetComponent<ObjectBehavior>().hasDied)
            return;
        if (!IsVisible())
            return;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

		MoveTowardPlayer();

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

	public void Die()
	{
		if (hasDied)
		{
			return;
		}

		hasDied = true;

		if (portal == null)
		{
			LevelManager manager = LevelManager.Instance;
			if (manager != null && manager.TryGetPortal(out Portal resolvedPortal))
			{
				portal = resolvedPortal;
			}
			else
			{
				portal = FindAnyObjectByType<Portal>();
			}
		}

		if (portal != null)
		{
			portal.DecrementEnemiesLeft();
		}

		gameObject.SetActive(false);
	}

    private void OnCollisionEnter2D(Collision2D col)
    {
		if (col == null)
		{
			return;
		}

		ColorCollisionResolver.ResolveEnemyTouch(this, col.gameObject);
    }

    private void MoveTowardPlayer()
    {
		if (player == null)
		{
			return;
		}
		ObjectBehavior playerBehavior = player.GetComponent<ObjectBehavior>();
		if (playerBehavior == null || playerBehavior.hasDied)
		{
			return;
		}
		Renderer playerRenderer = player.GetComponent<Renderer>();
		if (playerRenderer == null || playerRenderer.enabled == false)
		{
			return;
		}
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
		rigidBody2D.AddForce(direction * redAcceleration);
    }
    private bool IsVisible()
    {
        Vector2 origin = transform.position;
        Vector2 target = player.position;

        RaycastHit2D hit = Physics2D.Linecast(origin, target, obstacleLayer);
        return hit.collider == null;
    }

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 6) isGrounded += 1;
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 6) isGrounded -= 1;
		if (!IsGrounded() && Physics2D.gravity == Vector2.zero) Die();
	}
}
