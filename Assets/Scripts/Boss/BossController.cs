using UnityEngine;

public class BossController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Transform attackArea;
    private Animator animator;
    private bool facingRight = true;
    private bool isAttacking = false;
    private float attackCooldown = 2f;
    private float lastAttackTime;
    private BossHealthManager healthManager;
    private GameObject playerObj;
    private Transform player;
    private PlayerHealth playerHealth;

    void Start()
    {
        animator = GetComponent<Animator>();
        healthManager = GetComponent<BossHealthManager>();
        FindPlayer();
    }

    void Update()
    {
        if (playerObj == null)
        {
            FindPlayer();
            if (playerObj == null) return;
        }

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

        if (healthManager.isPhase2 && healthManager.CanTeleport() && Random.value < 0.1f)
        {
            healthManager.TeleportToPlayer(player);
        }
    }

    void FindPlayer()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void MoveTowardsPlayer()
    {
        if (player == null) return;

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
        DeactivateAttackArea(); // Đảm bảo tắt AttackArea khi ngắt
    }

    public void DealDamageToBoss(int damage)
    {
        if (healthManager != null)
        {
            healthManager.TakeDamage(damage);
            Debug.Log($"Boss took {damage} damage! HP: {healthManager.currentHealth}/{healthManager.maxHealth}");
        }
    }

    // 🔑 FIX 1: Tùy chỉnh logic va chạm để tránh đẩy Player
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && attackArea.gameObject.activeSelf && playerHealth != null)
        {
            // Gây damage nhưng không đẩy Player
            playerHealth.TakeDamage(1, transform);
            Debug.Log("Boss HIT Player! Player HP reduced!");
            
            // Giữ Player trong tầm tấn công bằng cách tạm khóa chuyển động
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero; // Ngăn Player bị đẩy
                StartCoroutine(ReleasePlayerMovement(playerRb)); // Giải phóng sau 0.1s
            }
        }
    }

    // 🔑 FIX 2: Thêm Coroutine để giải phóng Player sau khi tấn công
    private System.Collections.IEnumerator ReleasePlayerMovement(Rigidbody2D playerRb)
    {
        yield return new WaitForSeconds(0.1f); // Giữ 0.1s để hoàn thành animation
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero; // Đảm bảo không bị đẩy thêm
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

    public void OnBossHitByWeapon(int damage)
    {
        DealDamageToBoss(damage);
    }
}