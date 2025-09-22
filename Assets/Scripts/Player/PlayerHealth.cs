using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : Singleton<PlayerHealth>
{
    public bool isDead { get; private set; }
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockBackThrustAmount = 10f;
    [SerializeField] private float damageRecoveryTime = 1f;

    private Slider healthSlider;
    private int currentHealth;
    private bool canTakeDamage = true;
    private KnockBack knockBack;
    private Flash flash;
    const string HEALTH_SLIDER_TEXT = "Health Slider";
    readonly int DEATH_HASH = Animator.StringToHash("Death");
    private Vector3 spawnPosition;
    private Animator animator;
    protected override void Awake()
    {
        base.Awake();
        knockBack = GetComponent<KnockBack>();
        flash = GetComponent<Flash>();
        animator = GetComponent<Animator>();

    }
    private void Start()
    {
        isDead = false;
        currentHealth = maxHealth;
        spawnPosition = transform.position;

        UpdateHealthSlider();
    }
    private void OnCollisionStay2D(Collision2D other)
    {
        EnemyAI enemy = other.gameObject.GetComponent<EnemyAI>();
        if (enemy)
        {
            TakeDamage(1, other.transform);
        }
    }
    public void HealPlayer()
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += 1;
            UpdateHealthSlider();
        }
    }
    public void TakeDamage(int damageAmount, Transform hitTransform)
    {
        if (!canTakeDamage) return;

        ScreenShakeManager.Instance.ShakeScreen();
        knockBack.GetKnockedBack(hitTransform, knockBackThrustAmount);
        StartCoroutine(flash.FlashRoutine());
        canTakeDamage = false;
        currentHealth -= damageAmount;
        StartCoroutine(DamageRecoveryRoutine());
        UpdateHealthSlider();
        CheckIfPlayerDeath();
    }
    private void CheckIfPlayerDeath()
    {
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            if (ActiveWeapon.Instance != null) ActiveWeapon.Instance.gameObject.SetActive(false);
            currentHealth = 0;
            GetComponent<Animator>().SetTrigger(DEATH_HASH);
            StartCoroutine(RespawnRoutine());
        }
    }
    private IEnumerator RespawnRoutine()
    {
        // Ẩn player
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        // Dừng mọi hiệu ứng/coroutine còn lại
        if (flash != null)
        {
            flash.ResetFlash(); // Thêm hàm này trong Flash.cs để reset alpha về bình thường
        }
        if (knockBack != null)
        {
            knockBack.StopKnockBack(); // Nếu có trạng thái knockback, hãy reset ở đây
        }

        yield return new WaitForSeconds(2f); // thời gian "chết"

        // Đưa player về vị trí spawn ban đầu
        transform.position = spawnPosition;

        // Reset máu
        currentHealth = maxHealth;
        UpdateHealthSlider();

        // Reset trạng thái
        isDead = false;

        // Hiện lại player
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;
        // 🔑 Reset Animator về Idle
        if (animator != null)
        {
            animator.ResetTrigger(DEATH_HASH);
            animator.Play("Idle"); // đổi "Idle" thành đúng tên state idle trong Animator
        }
        // Bật lại vũ khí
        if (ActiveWeapon.Instance != null)
            ActiveWeapon.Instance.gameObject.SetActive(true);

        // 🔑 Gọi WaveUI để hiện wave hiện tại
        WaveUI waveUI = FindFirstObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.RefreshUI();
        }
    }
    private IEnumerator DamageRecoveryRoutine()
    {
        yield return new WaitForSeconds(damageRecoveryTime);
        canTakeDamage = true;
    }
    private void UpdateHealthSlider()
    {
        if (healthSlider == null)
        {
            healthSlider = GameObject.Find(HEALTH_SLIDER_TEXT).GetComponent<Slider>();
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

}
