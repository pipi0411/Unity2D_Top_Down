using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : Singleton<PlayerHealth>
{
    public bool isDead { get; private set; }

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockBackThrustAmount = 10f;
    [SerializeField] private float damageRecoveryTime = 1f;

    private Slider healthSlider;
    private int currentHealth;
    private bool canTakeDamage = true;

    private KnockBack knockBack;
    private Flash flash;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Rigidbody2D rb;
    private GameObject cachedWeaponGO;
    private Vector3 spawnPosition;

    const string HEALTH_SLIDER_TEXT = "Health Slider";
    readonly int DEATH_HASH = Animator.StringToHash("Death");

    protected override void Awake()
    {
        base.Awake();
        knockBack = GetComponent<KnockBack>();
        flash = GetComponent<Flash>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
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
        if (!canTakeDamage || isDead) return;

        ScreenShakeManager.Instance.ShakeScreen();
        knockBack.GetKnockedBack(hitTransform, knockBackThrustAmount);
        StartCoroutine(flash.FlashRoutine());

        AudioManager.Instance.PlayPlayerHurt();

        canTakeDamage = false;
        currentHealth -= damageAmount;
        UpdateHealthSlider();

        StartCoroutine(DamageRecoveryRoutine());
        CheckIfPlayerDeath();
    }

    private void CheckIfPlayerDeath()
    {
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            currentHealth = 0;

            // 🔊 Dừng tiếng chạy (nếu đang chạy)
            AudioManager.Instance?.StopPlayerRun();

            // 🔊 Phát âm thanh chết
            AudioManager.Instance?.PlayPlayerDeath();

            // Ẩn vũ khí khi chết
            if (ActiveWeapon.Instance != null)
            {
                cachedWeaponGO = ActiveWeapon.Instance.gameObject;
                cachedWeaponGO.SetActive(false);
            }
            else if (cachedWeaponGO != null)
            {
                cachedWeaponGO.SetActive(false);
            }

            // Kích hoạt animation chết
            if (animator != null)
            {
                animator.SetTrigger(DEATH_HASH);
            }

            // Dừng di chuyển hoàn toàn
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // Ẩn player
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (bodyCollider != null) bodyCollider.enabled = false;

        // Dừng flash, knockback
        if (flash != null) flash.ResetFlash();
        if (knockBack != null) knockBack.StopKnockBack();

        yield return new WaitForSeconds(2f); // Thời gian chờ "chết"

        // Đưa player về vị trí spawn ban đầu
        transform.position = spawnPosition;

        // Reset máu
        currentHealth = maxHealth;
        UpdateHealthSlider();

        // Reset trạng thái
        isDead = false;
        canTakeDamage = true;

        // Hiện lại player
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bodyCollider != null) bodyCollider.enabled = true;

        // Reset Animator sạch trạng thái
        if (animator != null)
        {
            animator.ResetTrigger(DEATH_HASH);
            animator.Rebind();
            animator.Update(0f);
        }

        // Bật lại vũ khí
        if (cachedWeaponGO != null)
        {
            cachedWeaponGO.SetActive(true);
            var weaponAnim = cachedWeaponGO.GetComponent<Animator>();
            if (weaponAnim != null)
            {
                weaponAnim.Rebind();
                weaponAnim.Update(0f);
            }
        }

        // Cập nhật lại UI wave
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
