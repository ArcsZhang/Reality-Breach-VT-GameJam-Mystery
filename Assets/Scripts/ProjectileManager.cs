using UnityEngine;

public class ProjectileManager : MonoBehaviour
{    
    [SerializeField] private float projectileSpeed = 200.0f;
    [SerializeField] private float lifetime = 100f;
    [SerializeField] private bool applyInitialVisualRotation = true;

    private Rigidbody2D rb;
	private GameObject player;
    private bool hasInitialVelocityOverride;
    private Vector2 initialVelocityOverride;

    public void SetSkipInitialVisualRotation(bool skip)
    {
        applyInitialVisualRotation = !skip;
    }

    public void SetInitialVelocityOverride(Vector2 velocity)
    {
        hasInitialVelocityOverride = true;
        initialVelocityOverride = velocity;
    }

    public void InitializeSnapshotClone(Vector2 velocity)
    {
        SetSkipInitialVisualRotation(true);
        SetInitialVelocityOverride(velocity);

        if (velocity.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angleDeg = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		LevelManager manager = LevelManager.Instance;
		if (manager != null)
		{
			if (manager.TryGetPlayer(out ObjectBehavior playerBehavior))
			{
				player = playerBehavior.gameObject;
			}
		}
        rb = GetComponent<Rigidbody2D>();

        if (hasInitialVelocityOverride && initialVelocityOverride.sqrMagnitude > 0.0001f)
        {
            rb.linearVelocity = initialVelocityOverride;
        }
        else
        {
            rb.linearVelocity = transform.right * projectileSpeed;
        }

        if (applyInitialVisualRotation)
        {
            transform.rotation *= Quaternion.Euler(0, 0, -90);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
        {
            Destroy(gameObject);
            return;
        }

        ObjectBehavior objectBehavior = collision.gameObject.GetComponent<ObjectBehavior>();
        if (objectBehavior != null)
        {
            objectBehavior.Die();
        }

        EnemyBehavior enemyBehavior = collision.gameObject.GetComponent<EnemyBehavior>();
        if (enemyBehavior != null)
        {
            enemyBehavior.Die();
        }

		TerrainBehavior terrainBehavior = collision.gameObject.GetComponent<TerrainBehavior>();
		if (terrainBehavior != null && terrainBehavior.GetColor() == ColorManager.ColorState.Green)
		{
			terrainBehavior.Destroy();
		}
		if (terrainBehavior != null && terrainBehavior.GetColor() == ColorManager.ColorState.Blue)
		{
			terrainBehavior.Destroy();
		}


        Destroy(gameObject);
    }
}
