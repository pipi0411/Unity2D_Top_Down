using UnityEngine;
using System.Collections;

public class Bow : MonoBehaviour, IWeapon
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponInfor weaponInfo;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    readonly int FIRE_HASH = Animator.StringToHash("Fire");
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public WeaponInfor GetWeaponInfo() => weaponInfo;
    public void Attack()
    {
        animator.SetTrigger(FIRE_HASH);

        GameObject newArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, ActiveWeapon.Instance.transform.rotation);
        newArrow.GetComponent<Projectile>().UpdateProjectileRange(weaponInfo.weaponRange);
    }
}
