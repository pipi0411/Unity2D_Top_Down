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
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Rigidbody2D rb;
    private GameObject cachedWeaponGO;

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
            if (ActiveWeapon.Instance != null)
            {
                cachedWeaponGO = ActiveWeapon.Instance.gameObject;
                cachedWeaponGO.SetActive(false); // ẩn vũ khí khi chết
            }
            else if (cachedWeaponGO != null)
            {
                cachedWeaponGO.SetActive(false); // ẩn vũ khí khi chết
            }

            currentHealth = 0;
            if (animator != null)
            {
                animator.SetTrigger(DEATH_HASH);
            }
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // Ẩn player
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (bodyCollider != null) bodyCollider.enabled = false;

        // Dừng mọi hiệu ứng/coroutine còn lại
        if (flash != null)
        {
            flash.ResetFlash();
        }
        if (knockBack != null)
        {
            knockBack.StopKnockBack();
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(2f); // thời gian "chết"

        // Đưa player về vị trí spawn ban đầu
        transform.position = spawnPosition;

        // Reset máu
        currentHealth = maxHealth;
        UpdateHealthSlider();

        // Reset trạng thái
        isDead = false;
        canTakeDamage = true; // 🔑 FIX: reset luôn để không bị delay vũ khí

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
