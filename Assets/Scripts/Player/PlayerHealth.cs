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
    public int CurrentHealth => currentHealth;

    private bool isReviving = false;

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

            AudioManager.Instance?.StopPlayerRun();
            AudioManager.Instance?.PlayPlayerDeath();

            if (ActiveWeapon.Instance != null)
            {
                cachedWeaponGO = ActiveWeapon.Instance.gameObject;
                cachedWeaponGO.SetActive(false);
            }
            else if (cachedWeaponGO != null)
            {
                cachedWeaponGO.SetActive(false);
            }

            if (animator != null)
                animator.SetTrigger(DEATH_HASH);

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            StartCoroutine(DeathRoutine());
        }
    }

    private IEnumerator DeathRoutine()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (bodyCollider != null) bodyCollider.enabled = false;

        if (flash != null) flash.ResetFlash();
        if (knockBack != null) knockBack.StopKnockBack();

        yield return new WaitForSeconds(0.5f);

        TryOfferRevive();
    }

    // ==========================
    // 💰 PHẦN HỒI SINH BẰNG COIN
    // ==========================
    private void TryOfferRevive()
    {
        int reviveCost = 10; // giá hồi sinh
        if (EconomyManager.Instance != null && EconomyManager.Instance.GetGold() >= reviveCost)
        {
            // Use the two-callback overload: onAccept and onDecline
            ReviveUI.Instance?.Show(
                () =>
                {
                    EconomyManager.Instance.SpendGold(reviveCost);
                    Revive();
                },
                () =>
                {
                    GameOverUI.Instance?.Show();
                }
            );
        }
        else
        {
            GameOverUI.Instance?.Show();
        }
    }

    private void Revive()
    {
        if (isReviving) return;
        isReviving = true;

        Debug.Log("[PlayerHealth] Reviving player with coins...");

        StartCoroutine(ReviveRoutine());
    }

    private IEnumerator ReviveRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        currentHealth = maxHealth;
        UpdateHealthSlider();

        transform.position = spawnPosition;
        isDead = false;
        canTakeDamage = true;

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (bodyCollider != null) bodyCollider.enabled = true;

        if (animator != null)
        {
            animator.ResetTrigger(DEATH_HASH);
            animator.Rebind();
            animator.Update(0f);
        }

        if (cachedWeaponGO != null)
            cachedWeaponGO.SetActive(true);

        UIFade.Instance?.FadeToClear();

        isReviving = false;

        // Làm mới lại WaveUI nếu có
        WaveUI waveUI = FindFirstObjectByType<WaveUI>();
        if (waveUI != null)
            waveUI.RefreshUI();
    }

    // ==========================
    // 🔁 HỒI SINH CŨ (nếu không có revive)
    // ==========================
    private IEnumerator RespawnRoutine()
    {
        yield return null;
        // Giờ không dùng nữa vì thay bằng revive
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
            healthSlider = GameObject.Find(HEALTH_SLIDER_TEXT)?.GetComponent<Slider>();
        }

        if (healthSlider == null) return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateHealthSlider();
    }
}
