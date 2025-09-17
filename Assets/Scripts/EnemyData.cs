// Scripts/EnemyData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float roamChangeDirTime = 2f;

    [Header("Combat")]
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public bool stopMovingWhileAttacking = false;

    [Header("Health")]
    public int startingHealth = 3;
    public float knockBackThrust = 15f;
    public GameObject deathVFXPrefab;

    [Header("Flash")]
    public Material flashMaterial;
    public float flashRestoreTime = 0.2f;

    [Header("Drops")]
    public GameObject goldCoin;
    public GameObject healthGlobe;
    public GameObject staminaGlobe;
    public int maxGoldCoins = 5;
    public int maxHealthGlobes = 2;
    public int maxStaminaGlobes = 2;
}

