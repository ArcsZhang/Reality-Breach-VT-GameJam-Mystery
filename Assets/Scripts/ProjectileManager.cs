using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public EnemyBehavior source;
    
    [SerializeField] private float projectileSpeed = 200.0f;
    [SerializeField] private float lifetime = 100f;

    private Rigidbody2D rb;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = transform.right * projectileSpeed;
        transform.rotation *= Quaternion.Euler(0, 0, -90);

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            ColorCollisionResolver.ResolveEnemyTouch(source, collision.gameObject);
        Destroy(gameObject);
    }
}
