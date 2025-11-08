using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public float moveSpeed = 2f;
    public AttackArea attackArea; // changed from Transform to AttackArea
    
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

        // safety: if designer assigned Transform previously, try to find AttackArea component on it
        if (attackArea == null)
        {
            var child = transform.Find("AttackArea");
            if (child != null)
                attackArea = child.GetComponent<AttackArea>();
        }
    }

    void FixedUpdate()
    {
        if (playerObj == null)
        {
            FindPlayer();
            if (playerObj == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isAttacking && !attackLocked && distanceToPlayer <= attackRange)
        {
            StartAttack();
            StartCoroutine(ActivateAttackAreaAfterDelay());
        }
        else if (!isAttacking && !attackLocked && distanceToPlayer > attackRange)
        {
            MoveTowardsPlayer();
        }

        if (isAttacking && distanceToPlayer > interruptRange)
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

        if (moveDirection > 0 && !facingRight) Flip();
        else if (moveDirection < 0 && facingRight) Flip();

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
        attackLocked = true;
        animator.SetBool("isAttacking", true);
        animator.SetBool("isMoving", false);
        lastAttackTime = Time.time;
        
        StartCoroutine(UnlockAttackAfterDuration());
    }

    private IEnumerator ActivateAttackAreaAfterDelay()
    {
        yield return new WaitForSeconds(attackActivationDelay);
        if (isAttacking && attackArea != null)
        {
            attackArea.Activate(); // AttackArea handles OnTriggerEnter2D -> damage player
        }
    }

    private IEnumerator UnlockAttackAfterDuration()
    {
        yield return new WaitForSeconds(attackLockDuration);
        attackLocked = false;
        isAttacking = false;
        animator.SetBool("isAttacking", false);
        // ensure hitbox off
        if (attackArea != null)
            attackArea.StopAllCoroutines(); // stop activate coroutine if running
    }

    void InterruptAttack()
    {
        if (attackLocked) return;
        isAttacking = false;
        attackLocked = false;
        animator.SetBool("isAttacking", false);
        animator.SetTrigger("interruptAttack");
        if (attackArea != null)
        {
            attackArea.StopAllCoroutines();
        }
    }

    public void DealDamageToBoss(int damage)
    {
        if (healthManager != null)
        {
            healthManager.TakeDamage(damage);
        }
    }

    // expose utility methods if other systems want to trigger area manually
    public void ActivateAttackArea()
    {
        attackArea?.Activate();
    }

    public void DeactivateAttackArea()
    {
        // stop any active coroutine and disable collider
        if (attackArea != null)
        {
            attackArea.StopAllCoroutines();
            // ensure collider off
            var col = attackArea.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    public void OnBossHitByWeapon(int damage)
    {
        DealDamageToBoss(damage);
    }
}