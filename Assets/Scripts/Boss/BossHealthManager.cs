using UnityEngine;
using UnityEngine.UI;

public class BossHealthManager : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;
    public Slider healthSlider;
    public bool isPhase2 = false;
    private float teleportCooldown = 5f;
    private float lastTeleportTime;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        // Ensure the Boss has a collider to detect damage
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("Boss missing Collider2D! Adding one automatically.");
            gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is the weapon collider
        if (other.CompareTag("Weapon"))
        {
            // Assuming weaponInfo.damage is accessible or a fixed damage value
            int damage = 10; // Replace with actual damage value if available from weapon
            TakeDamage(damage);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // Trigger Hurt animation khi bị đánh
        if (currentHealth > 0)
        {
            GetComponent<Animator>().SetTrigger("Hurt"); // Đảm bảo trigger Hurt mỗi lần bị đánh
        }

        // Kích hoạt Phase 2 khi máu < 50%
        if (!isPhase2 && currentHealth <= maxHealth / 2)
        {
            isPhase2 = true;
            GetComponent<Animator>().SetBool("isPhase2", true);
            Debug.Log("Boss entered Phase 2! Teleport unlocked.");
        }

        // Xử lý chết: Chuyển từ Hurt cuối sang Die
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool CanTeleport()
    {
        return isPhase2 && Time.time - lastTeleportTime >= teleportCooldown;
    }

    public void TeleportToPlayer(Transform player)
    {
        if (player != null && CanTeleport())
        {
            Vector2 playerPos = player.position;
            transform.position = playerPos;
            lastTeleportTime = Time.time;
            GetComponent<Animator>().SetTrigger("Teleport");
        }
    }

    void Die()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isDead", true); // Đặt trạng thái chết
            animator.SetTrigger("Die"); // Trigger animation Die
            animator.ResetTrigger("Hurt"); // Đảm bảo tắt Hurt trước khi Die
        }
        GetComponent<BossController>().enabled = false;
        Destroy(gameObject, 2f);
    }
}