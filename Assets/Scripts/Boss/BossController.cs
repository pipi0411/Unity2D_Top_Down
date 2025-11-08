using UnityEngine;
using System.Collections;

/// <summary>
/// Điều khiển Boss: tấn công theo phạm vi; kích hoạt AttackArea sau một delay.
/// Có đầy đủ guard để không gọi vào object đã unload/disable.
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public float moveSpeed = 2f;

    [Header("References")]
    public AttackArea attackArea; // kéo thả từ Inspector, hoặc sẽ tìm con tên "AttackArea"

    [Header("Attack Settings")]
    public int bossDamage = 1;
    [Tooltip("Thời gian giữa các lần tấn công (khóa điều khiển).")]
    public float attackCooldown = 0.5f;

    [Header("Ranges")]
    [Tooltip("Khoảng cách để bắt đầu tấn công.")]
    public float attackRange = 3f;
    [Tooltip("Khoảng cách để hủy tấn công đang diễn ra.")]
    public float interruptRange = 4.5f;

    [Header("Timing")]
    [Tooltip("Độ trễ (giây) trước khi bật hitbox sau khi bắt đầu animation tấn công.")]
    public float attackActivationDelay = 0.2f;
    [Tooltip("Thời gian khoá tấn công sau khi bắt đầu (giây).")]
    public float attackLockDuration = 0.5f;

    private Animator animator;
    private bool facingRight = true;
    private bool isAttacking = false;
    private bool attackLocked = false;
    private float lastAttackTime;

    // Tuỳ dự án của bạn – giả định có BossHealthManager để phase/teleport
    private BossHealthManager healthManager;

    private GameObject playerObj;
    private Transform player;
    private PlayerHealth playerHealth;

    private void Start()
    {
        animator = GetComponent<Animator>();
        healthManager = GetComponent<BossHealthManager>();
        FindPlayer();

        if (attackArea == null)
        {
            var child = transform.Find("AttackArea");
            if (child != null) attackArea = child.GetComponent<AttackArea>();
        }
    }

    private void FixedUpdate()
    {
        if (playerObj == null)
        {
            FindPlayer();
            if (playerObj == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Điều kiện bắt đầu tấn công
        if (!isAttacking && !attackLocked && distanceToPlayer <= attackRange)
        {
            StartAttack();
            StartCoroutine(ActivateAttackAreaAfterDelay());
        }
        else if (!isAttacking && !attackLocked && distanceToPlayer > attackRange)
        {
            MoveTowardsPlayer();
        }

        // Hủy tấn công nếu người chơi chạy ra xa
        if (isAttacking && distanceToPlayer > interruptRange)
        {
            InterruptAttack();
        }

        // Ví dụ phase 2 (tuỳ dự án của bạn)
        if (healthManager != null && healthManager.isPhase2 && healthManager.CanTeleport() && Random.value < 0.1f)
        {
            healthManager.TeleportToPlayer(player);
        }
    }

    private void FindPlayer()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    private void MoveTowardsPlayer()
    {
        if (player == null) return;

        float moveDirection = player.position.x - transform.position.x;
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        if (moveDirection > 0 && !facingRight) Flip();
        else if (moveDirection < 0 && facingRight) Flip();

        animator.SetBool("isMoving", true);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        var scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackLocked = true;
        animator.SetBool("isAttacking", true);
        animator.SetBool("isMoving", false);
        lastAttackTime = Time.time;

        StartCoroutine(UnlockAttackAfterDuration());
    }

    /// <summary>
    /// Bật hitbox sau 1 khoảng delay để khớp animation vung tay.
    /// Có đầy đủ guard để tránh gọi vào object đã bị unload/disable.
    /// </summary>
    private IEnumerator ActivateAttackAreaAfterDelay()
    {
        yield return new WaitForSeconds(attackActivationDelay);

        // Guard cực chặt
        if (this == null || !isActiveAndEnabled) yield break;
        if (!isAttacking) yield break;
        if (attackArea == null || !attackArea.isActiveAndEnabled || !attackArea.gameObject.activeInHierarchy) yield break;

        attackArea.Activate(); // AttackArea không còn coroutine → rất an toàn
    }

    /// <summary>
    /// Mở khoá tấn công sau một thời gian; đảm bảo tắt hitbox an toàn.
    /// </summary>
    private IEnumerator UnlockAttackAfterDuration()
    {
        yield return new WaitForSeconds(attackLockDuration);

        if (this == null) yield break;

        attackLocked = false;
        isAttacking = false;
        animator.SetBool("isAttacking", false);

        if (attackArea != null) attackArea.Deactivate();
    }

    private void InterruptAttack()
    {
        if (attackLocked) return; // đang khoá thì không interrupt

        isAttacking = false;
        attackLocked = false;
        animator.SetBool("isAttacking", false);
        animator.SetTrigger("interruptAttack");

        if (attackArea != null) attackArea.Deactivate();
    }

    // API phụ (nếu animator gọi event)
    public void ActivateAttackArea()  => attackArea?.Activate();
    public void DeactivateAttackArea() => attackArea?.Deactivate();

    // Nếu có vũ khí người chơi đánh vào boss
    public void OnBossHitByWeapon(int damage)
    {
        if (healthManager != null) healthManager.TakeDamage(damage);
    }
}
