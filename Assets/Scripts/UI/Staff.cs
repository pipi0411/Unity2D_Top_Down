using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Thêm dòng này

public class Staff : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfor weaponInfo;
    [SerializeField] private GameObject magicLaser;
    [SerializeField] private Transform magicLaserSpawnPoint;
    private Animator animator;
    readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        MouseFollowWithOffSet();
    }
    public void Attack()
    {
        animator.SetTrigger(ATTACK_HASH);
    }
    public void SpawnStaffProjectileAnimEvent()
    {
        GameObject newLaser = Instantiate(magicLaser, magicLaserSpawnPoint.position, Quaternion.identity);
        newLaser.GetComponent<MagicLaser>().UpdateLaserRange(weaponInfo.weaponRange);
    }
    public WeaponInfor GetWeaponInfo()
    {
        return weaponInfo;
    }
    private void MouseFollowWithOffSet()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue(); // Sửa dòng này
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(PlayerController.Instance.transform.position);

        float angle = Mathf.Atan2(mousePosition.y, mousePosition.x) * Mathf.Rad2Deg;
        if (mousePosition.x < playerScreenPos.x)
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, -180, angle);
        }
        else
        {
            ActiveWeapon.Instance.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}