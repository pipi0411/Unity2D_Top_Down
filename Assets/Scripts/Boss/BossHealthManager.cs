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

        // Ensure the Boss has a collider to detect damage from weapon
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        // Do NOT add EnemyHealth here — boss health handled by this component
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Weapon"))
        {
            int damage = 10; // replace with actual weapon damage access if available
            TakeDamage(damage);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (healthSlider != null) healthSlider.value = currentHealth;

        if (currentHealth > 0)
        {
            var animator = GetComponent<Animator>();
            if (animator != null) animator.SetTrigger("Hurt");
        }

        if (!isPhase2 && currentHealth <= maxHealth / 2)
        {
            isPhase2 = true;
            var animator = GetComponent<Animator>();
            if (animator != null) animator.SetBool("isPhase2", true);
        }

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
            GetComponent<Animator>()?.SetTrigger("Teleport");
        }
    }

    void Die()
    {
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isDead", true);
            animator.SetTrigger("Die");
            animator.ResetTrigger("Hurt");
        }

        var controller = GetComponent<BossController>();
        if (controller != null) controller.enabled = false;

        // destroy after short delay (allow animation)
        Destroy(gameObject, 1.5f);
    }
}