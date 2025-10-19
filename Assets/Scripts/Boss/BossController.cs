using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public float moveSpeed = 2f;
    public Transform attackArea;
    
    [Header("⚔️ ATTACK SETTINGS")]
    public int bossDamage = 1;
    public float attackCooldown = 0.5f; 
    
    [Header("🔥 QUICK ATTACK FIX")]
    public float attackRange = 3f;  
    public float interruptRange = 4.5f;
    
    private Animator animator;
    private bool facingRight = true;
    private bool isAttacking = false;
    private float lastAttackTime;
    private BossHealthManager healthManager;
    private GameObject playerObj;
    private Transform player;
    private PlayerHealth playerHealth;
    
    // 🔑 LOCK ATTACK SYSTEM
    private bool attackLocked = false;
    private float attackLockDuration = 0.5f;
    private float attackActivationDelay = 0.2f; // Delay to simulate swing start

    void Start()
    {
        animator = GetComponent<Animator>();
        healthManager = GetComponent<BossHealthManager>();
        FindPlayer();
    }

    void FixedUpdate() // 🔑 CHUYỂN SANG FIXEDUPDATE ĐỂ TĂNG TẦN SUẤT KIỂM TRA
    {
        if (playerObj == null)
        {
            FindPlayer();
            if (playerObj == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // 🔑 TẤN CÔNG KHI VÀO TẦM 2.8F, KHÔNG BẬT ATTACKAREA NGAY
        if (!isAttacking && !attackLocked && distanceToPlayer <= attackRange)
        {
            StartAttack();
            // Start coroutine to activate attack area after delay
            StartCoroutine(ActivateAttackAreaAfterDelay());
        }
        // Chỉ di chuyển khi XA HƠN attackRange
        else if (!isAttacking && !attackLocked && distanceToPlayer > attackRange)
        {
            MoveTowardsPlayer();
        }

        // 🔑 NGẮT CHỈ KHI XA HƠN 4.5F
        if (isAttacking && distanceToPlayer > interruptRange)
        {
            InterruptAttack();
        }

        // Phase 2 teleport
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

    // 🔑 TẤN CÔNG CHỚP NHOÁNG, KHÔNG BẬT ATTACKAREA NGAY
    void StartAttack()
    {
        isAttacking = true;
        attackLocked = true; // 🔒 LOCK ATTACK
        animator.SetBool("isAttacking", true);
        animator.SetBool("isMoving", false);
        animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
        
        // 🔑 AUTO UNLOCK SAU 0.5s (animation duration)
        StartCoroutine(UnlockAttackAfterDuration());
        
        Debug.Log("⚔️ Boss QUICK ATTACK LOCKED!");
    }

    // 🔑 KÍCH HOẠT ATTACKAREA SAU KHOẢNG THỜI GIAN ĐỂ PHỎNG THEO ANIMATION
    private IEnumerator ActivateAttackAreaAfterDelay()
    {
        yield return new WaitForSeconds(attackActivationDelay); // Delay before activating
        if (isAttacking && !attackLocked) // Ensure still in attack state
        {
            ActivateAttackArea();
            Debug.Log("⚔️ Attack Area Activated after Delay!");
        }
    }

    // 🔑 AUTO UNLOCK SAU KHI HOÀN THÀNH ATTACK
    private IEnumerator UnlockAttackAfterDuration()
    {
        yield return new WaitForSeconds(attackLockDuration);
        attackLocked = false;
        isAttacking = false; // Reset attack state
        animator.SetBool("isAttacking", false);
        DeactivateAttackArea();
        Debug.Log("🔓 Boss ATTACK UNLOCKED - Ready for next hit!");
    }

    // 🔑 NGẮT CHỈ KHI THỰC SỰ XA
    void InterruptAttack()
    {
        if (attackLocked) return; // KHÔNG NGẮT KHI ĐANG LOCK
        
        isAttacking = false;
        attackLocked = false;
        animator.SetBool("isAttacking", false);
        animator.SetTrigger("interruptAttack");
        DeactivateAttackArea();
        Debug.Log(" Boss INTERRUPTED - Player too far!");
    }

    public void DealDamageToBoss(int damage)
    {
        if (healthManager != null)
        {
            healthManager.TakeDamage(damage);
            Debug.Log($"Boss took {damage} damage! HP: {healthManager.currentHealth}/{healthManager.maxHealth}");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && attackArea.gameObject.activeSelf && playerHealth != null)
        {
            playerHealth.TakeDamage(bossDamage, transform);
            Debug.Log($"Boss CHÉM Player! Damage: {bossDamage} - Player HP reduced!");
            
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                StartCoroutine(ReleasePlayerMovement(playerRb));
            }
        }
    }

    private IEnumerator ReleasePlayerMovement(Rigidbody2D playerRb)
    {
        yield return new WaitForSeconds(0.1f);
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    public void ActivateAttackArea()
    {
        attackArea.gameObject.SetActive(true);
    }

    public void DeactivateAttackArea()
    {
        attackArea.gameObject.SetActive(false);
    }

    public void OnBossHitByWeapon(int damage)
    {
        DealDamageToBoss(damage);
    }
}