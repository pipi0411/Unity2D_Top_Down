// Scripts/EnemyPathfinding.cs
using UnityEngine;

public class EnemyPathfinding : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    private KnockBack knockBack;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockBack = GetComponent<KnockBack>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (knockBack.GettingKnockedBack) return;

        rb.MovePosition(rb.position + moveDir * (enemyData.moveSpeed * Time.fixedDeltaTime));

        if (moveDir.x < 0) spriteRenderer.flipX = true;
        else if (moveDir.x > 0) spriteRenderer.flipX = false;
    }

    public void MoveTo(Vector2 targetPosition) => moveDir = targetPosition;
    public void StopMoving() => moveDir = Vector3.zero;
}
