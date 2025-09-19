// Scripts/EnemyHealth.cs
using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

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
        currentHealth = enemyData.startingHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (knockBack != null && PlayerController.Instance != null)
        {
            knockBack.GetKnockedBack(PlayerController.Instance.transform, enemyData.knockBackThrust);
        }

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }

        StartCoroutine(CheckDetectDeathRoutine());
    }

    private IEnumerator CheckDetectDeathRoutine()
    {
        yield return new WaitForSeconds(enemyData.flashRestoreTime);
        DetectDeath();
    }

    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            if (enemyData.deathVFXPrefab != null)
                Instantiate(enemyData.deathVFXPrefab, transform.position, Quaternion.identity);

            GetComponent<PickUpSpawner>().DropItems(enemyData);

            OnEnemyDied?.Invoke(gameObject); // Báo về Spawner
            Destroy(gameObject);
        }
    }
}
