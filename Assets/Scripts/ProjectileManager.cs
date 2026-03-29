using UnityEngine;

public class ProjectileManager : MonoBehaviour
{    
    [SerializeField] private float projectileSpeed = 200.0f;
    [SerializeField] private float lifetime = 100f;

    private Rigidbody2D rb;
	private GameObject player;
    
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

        rb.linearVelocity = transform.right * projectileSpeed;
        transform.rotation *= Quaternion.Euler(0, 0, -90);

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
