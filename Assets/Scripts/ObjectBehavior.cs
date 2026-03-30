using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ObjectBehavior : ColorManager
{
	[Header("Player Object")]
	[SerializeField] public bool isPlayer = false;
    [Header("Yellow Attack")]
    [SerializeField] private GameObject projectileObject;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float projectileSpawnDistance = 1f;

    [Header("Green Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float deceleration = 100f;

    private Rigidbody2D rigidBody2D;
	private Renderer objectRenderer;
	private Collider2D objectCollider;
	private AudioSource audioSource;
    private float fireTimer;
    private float defaultGravityScale;
    private RigidbodyConstraints2D defaultConstraints;
    private Vector2 moveInput;
	private bool isInPortalTransition;
	public bool hasDied;
	
	public int isGrounded = 0;
	public bool IsGrounded(){
		return isGrounded > 0;
	}

    public void SetPortalTransitionActive(bool active)
    {
        isInPortalTransition = active;
    }

    protected override void Awake()
    {
        base.Awake();

        rigidBody2D = GetComponent<Rigidbody2D>();
		audioSource = GetComponent<AudioSource>();
		objectRenderer = GetComponent<Renderer>();
		objectCollider = GetComponent<Collider2D>();
        defaultGravityScale = rigidBody2D != null ? rigidBody2D.gravityScale : 1f;
        defaultConstraints = rigidBody2D != null ? rigidBody2D.constraints : RigidbodyConstraints2D.None;
    }

    protected override void OnRedChanged(ColorState previous)
    {
    }

    protected override void OnOrangeChanged(ColorState previous)
    {
    }

    protected override void OnYellowChanged(ColorState previous)
    {
        if (rigidBody2D != null)
        {
            rigidBody2D.gravityScale = 0f;
            rigidBody2D.constraints = defaultConstraints | RigidbodyConstraints2D.FreezeRotation;
        }

        moveInput = Vector2.zero;
        fireTimer = 0f;
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

		if (previousColor == ColorState.Yellow && newColor != ColorState.Yellow && rigidBody2D != null)
		{
			rigidBody2D.gravityScale = defaultGravityScale;
			rigidBody2D.constraints = defaultConstraints;
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
        if (projectileObject == null)
        {
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
        {
            return;
        }

        FireProjectile();
        fireTimer = fireRate;
    }

    protected override void OnGreenUpdate()
    {
        LevelManager manager = LevelManager.Instance;
        if (manager == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = manager.ReadMoveInput().normalized;
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

		if (isPlayer)
		{
			if (audioSource != null && !audioSource.isPlaying)
			{
				audioSource.Play();
			}
			// make child camera not child of player so it doesn't get disabled immediately
			Transform cameraTransform = transform.Find("Main Camera");
			if (cameraTransform != null)
			{
				cameraTransform.SetParent(null);
			}
			objectRenderer.enabled = false;
			objectCollider.enabled = false;
			this.enabled = false;
		}
		else 
		{
			gameObject.SetActive(false);
		}
	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        ColorCollisionResolver.ResolveObjectTouch(this, collision.gameObject);
    }

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 6) isGrounded += 1;
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 6) isGrounded -= 1;
        if (!isInPortalTransition && !FoldAbility.IsAnyFoldActive && !IsGrounded() && Physics2D.gravity == Vector2.zero) Die();
	}

    private void FireProjectile()
    {
        Vector2 direction = transform.right.normalized;
        Vector2 spawnPoint = (Vector2)transform.position + direction * projectileSpawnDistance;
        Instantiate(projectileObject, spawnPoint, transform.rotation);
    }
}
