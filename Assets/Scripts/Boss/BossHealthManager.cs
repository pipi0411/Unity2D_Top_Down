using UnityEngine;
using UnityEngine.UI;
using System;

public class BossHealthManager : MonoBehaviour
{
    [Header("HP")]
    public int maxHealth = 10;
    public int currentHealth;
    public Slider healthSlider;

    [Header("Phase / Teleport")]
    public bool isPhase2 = false;
    private float teleportCooldown = 5f;
    private float lastTeleportTime;

    // ✅ Boss state / events
    public bool IsDead { get; private set; } = false;
    public event Action<BossHealthManager> OnBossDied;

    private Animator animator;

    [Header("Drops")]
    // Prefab chứa GemPickup
    public GameObject gemPrefab;
    // Số viên gem spawn (mỗi viên sẽ có amount = 1). Nếu muốn 1 object chứa nhiều gem, thay logic tương ứng.
    public int gemDropAmount = 3;

    private void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        animator = GetComponent<Animator>();

        // Bảo đảm có collider để nhận đòn (nếu prefab thiếu)
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead) return;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        if (healthSlider != null) healthSlider.value = currentHealth;

        if (currentHealth > 0)
        {
            animator?.SetTrigger("Hurt");
        }

        if (!isPhase2 && currentHealth <= maxHealth / 2)
        {
            isPhase2 = true;
            animator?.SetBool("isPhase2", true);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool CanTeleport() => isPhase2 && Time.time - lastTeleportTime >= teleportCooldown;

    public void TeleportToPlayer(Transform player)
    {
        if (player != null && CanTeleport())
        {
            transform.position = (Vector2)player.position;
            lastTeleportTime = Time.time;
            animator?.SetTrigger("Teleport");
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (animator != null)
        {
            animator.SetBool("isDead", true);
            animator.SetTrigger("Die");
            animator.ResetTrigger("Hurt");
        }

        // ✅ Báo cho Spawner biết Boss đã chết
        OnBossDied?.Invoke(this);

        // Ngừng AI/điều khiển
        var controller = GetComponent<BossController>();
        if (controller != null) controller.enabled = false;

        // Spawn gem khi chết (spawn nhiều viên rải quanh vị trí boss)
        if (gemPrefab != null && GemManager.Instance != null)
        {
            for (int i = 0; i < Mathf.Max(1, gemDropAmount); i++)
            {
                Vector2 spawnPos = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 0.5f;
                GemManager.Instance.SpawnGemPrefab(gemPrefab, spawnPos, 1);
            }

            // phát âm thanh rơi item qua AudioManager (nếu có)
            AudioManager.Instance?.PlayItemDrop();
        }

        // Cho anim Die chạy rồi hủy object
        Destroy(gameObject, 1.5f);
    }
}
