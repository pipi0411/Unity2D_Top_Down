using UnityEngine;
using System.Collections;

public class Bow : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfor weaponInfo;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;
    readonly int FIRE_HASH = Animator.StringToHash("Fire");
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Attack()
    {
        animator.SetTrigger(FIRE_HASH);
        GameObject newArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, ActiveWeapon.Instance.transform.rotation);
        newArrow.GetComponent<Projectile>().UpdateProjectileRange(weaponInfo.weaponRange);
    }

    public WeaponInfor GetWeaponInfo()
    {
        return weaponInfo;
    }
}