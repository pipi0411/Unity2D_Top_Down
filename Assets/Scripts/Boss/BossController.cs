using UnityEngine;

public class BossController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Transform player;
    public Transform attackArea;
    private Animator animator;
    private bool facingRight = true;
    private bool isAttacking = false;
    private float attackCooldown = 2f;
    private float lastAttackTime;
    private BossHealthManager healthManager;

    void Start()
    {
        animator = GetComponent<Animator>();
        healthManager = GetComponent<BossHealthManager>();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isAttacking && distanceToPlayer > 2f)
        {
            MoveTowardsPlayer();
        }
        else if (!isAttacking && distanceToPlayer <= 2f && Time.time - lastAttackTime >= attackCooldown)
        {
            StartAttack();
        }

        if (isAttacking && distanceToPlayer > 3f)
        {
            InterruptAttack();
        }

        // Thêm logic dịch chuyển ở Phase 2
        if (healthManager.isPhase2 && healthManager.CanTeleport() && Random.value < 0.1f) // 10% cơ hội dịch chuyển mỗi frame
        {
            healthManager.TeleportToPlayer(player);
        }
    }

    void MoveTowardsPlayer()
    {
        float moveDirection = player.position.x - transform.position.x;
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        if (moveDirection > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveDirection < 0 && facingRight)
        {
            Flip();
        }

        animator.SetBool("isMoving", true);
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void StartAttack()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);
        animator.SetBool("isMoving", false);
        animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
    }

    void InterruptAttack()
    {
        isAttacking = false;
        animator.SetBool("isAttacking", false);
        animator.SetTrigger("interruptAttack");
    }

    public void DealDamageToBoss(int damage)
    {
        if (healthManager != null)
        {
            healthManager.TakeDamage(damage);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && attackArea.gameObject.activeSelf)
        {
            Debug.Log("Player hit by attack!");
        }
    }

    public void ActivateAttackArea()
    {
        attackArea.gameObject.SetActive(true);
    }

    public void DeactivateAttackArea()
    {
        attackArea.gameObject.SetActive(false);
        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }
}