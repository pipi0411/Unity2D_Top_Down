using UnityEngine;
using UnityEngine.UI;

public class BossHealthManager : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;
    public bool isPhase2 = false; // Theo dõi Phase 2

    private float teleportCooldown = 5f; // Hồi chiêu dịch chuyển
    private float lastTeleportTime; // Thời gian lần dịch chuyển cuối

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
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

        // Kích hoạt Phase 2 khi HP dưới 50%
        if (!isPhase2 && currentHealth <= maxHealth / 2)
        {
            isPhase2 = true;
            GetComponent<Animator>().SetBool("isPhase2", true); // Thêm Parameter isPhase2 trong Animator
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            GetComponent<Animator>().SetTrigger("Hurt");
        }
    }

    public bool CanTeleport()
    {
        return isPhase2 && Time.time - lastTeleportTime >= teleportCooldown;
    }

    public void TeleportToPlayer(Transform player)
    {
        if (CanTeleport())
        {
            Vector2 playerPos = player.position;
            transform.position = playerPos; // Dịch chuyển đến vị trí người chơi
            lastTeleportTime = Time.time; // Cập nhật thời gian dịch chuyển
            GetComponent<Animator>().SetTrigger("Teleport"); // Thêm animation Teleport nếu có
        }
    }

    void Die()
    {
        GetComponent<Animator>().SetBool("isDead", true);
        GetComponent<Animator>().SetTrigger("Die");
        Destroy(gameObject, 2f);
    }
}