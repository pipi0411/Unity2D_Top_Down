using UnityEngine;

public class BossController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public int health = 100;
    public Transform player;
    public Transform attackArea;
    private Animator animator;
    private bool facingRight = true;
    private bool isAttacking = false; // Theo dõi trạng thái tấn công
    private float attackCooldown = 2f; // Thời gian chờ trước khi tấn công lại
    private float lastAttackTime;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Kiểm tra di chuyển
        if (!isAttacking && distanceToPlayer > 2f) // Chỉ di chuyển nếu không tấn công và cách xa
        {
            MoveTowardsPlayer();
        }
        else if (!isAttacking && distanceToPlayer <= 2f && Time.time - lastAttackTime >= attackCooldown)
        {
            StartAttack();
        }

        // Kiểm tra ngắt tấn công nếu người chơi chạy
        if (isAttacking && distanceToPlayer > 3f)
        {
            InterruptAttack();
        }

        // Kiểm tra HP
        if (health <= 0)
        {
            animator.SetTrigger("Die");
            Destroy(gameObject, 2f);
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
        animator.SetBool("isMoving", false); // Ngừng di chuyển khi tấn công
        animator.SetTrigger("Attack"); // Kích hoạt animation Attack
        lastAttackTime = Time.time;
    }

    void InterruptAttack()
    {
        isAttacking = false;
        animator.SetBool("isAttacking", false);
        animator.SetTrigger("interruptAttack"); // Ngắt và chuyển sang Run
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        animator.SetTrigger("Hurt");
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && attackArea.gameObject.activeSelf)
        {
            // Logic gây sát thương 
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
        isAttacking = false; // Kết thúc tấn công
        animator.SetBool("isAttacking", false);
    }
}