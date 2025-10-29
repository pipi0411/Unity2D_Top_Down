// Scripts/EnemyHealth.cs
using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    // Fallbacks khi enemyData == null (ví dụ boss thêm runtime)
    [SerializeField] private int fallbackStartingHealth = 1;
    [SerializeField] private float fallbackFlashRestoreTime = 0.1f;

    private int currentHealth;
    private KnockBack knockBack;
    private Flash flash;

    public event Action<GameObject> OnEnemyDied;

    private void Awake()
    {
        knockBack = GetComponent<KnockBack>();
        flash = GetComponent<Flash>();
    }

    private void Start()
    {
        if (enemyData != null)
            currentHealth = enemyData.startingHealth;
        else
        {
            // Nếu có BossHealthManager, lấy maxHealth làm start
            var boss = GetComponent<BossHealthManager>();
            if (boss != null)
                currentHealth = boss.currentHealth > 0 ? boss.currentHealth : Mathf.Max(1, boss.maxHealth);
            else
                currentHealth = fallbackStartingHealth;
        }
    }

    // Cho phép khởi tạo/ghi đè health từ bên ngoài (BossHealthManager khi AddComponent runtime)
    public void SetCurrentHealth(int hp)
    {
        currentHealth = hp;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (knockBack != null && PlayerController.Instance != null)
        {
            knockBack.GetKnockedBack(PlayerController.Instance.transform, enemyData != null ? enemyData.knockBackThrust : 0f);
        }

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        StartCoroutine(CheckDetectDeathRoutine());
    }

    private IEnumerator CheckDetectDeathRoutine()
    {
        float wait = enemyData != null ? enemyData.flashRestoreTime : fallbackFlashRestoreTime;
        yield return new WaitForSeconds(wait);
        DetectDeath();
    }

    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            if (enemyData != null && enemyData.deathVFXPrefab != null)
                Instantiate(enemyData.deathVFXPrefab, transform.position, Quaternion.identity);

            GetComponent<PickUpSpawner>()?.DropItems(enemyData);

            OnEnemyDied?.Invoke(gameObject); // Báo về Spawner
            Destroy(gameObject);
        }
    }

    // Gọi từ bên ngoài (ví dụ BossHealthManager) để báo chết và cho Spawner biết
    public void NotifyExternalDeath()
    {
        // optional VFX / drops nếu có enemyData
        if (enemyData != null && enemyData.deathVFXPrefab != null)
            Instantiate(enemyData.deathVFXPrefab, transform.position, Quaternion.identity);

        GetComponent<PickUpSpawner>()?.DropItems(enemyData);

        OnEnemyDied?.Invoke(gameObject);
        Destroy(gameObject);
    }
}
