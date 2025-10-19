using System.Collections;
using UnityEngine;

public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon { get; private set; }

    private PlayerControls playerControls;
    private float timeBetweenAttacks;
    private bool attackButtonDown, isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        playerControls = new PlayerControls();
    }

    private void OnEnable() => playerControls.Enable();

    private void Start()
    {
        playerControls.Combat.Attack.started += _ => StartAttacking();
        playerControls.Combat.Attack.canceled += _ => StopAttacking();
        AttackCooldown();
    }

    private void Update() => Attack();

    public void NewWeapon(MonoBehaviour newWeapon)
    {
        if (CurrentActiveWeapon != null && CurrentActiveWeapon != newWeapon)
        {
            Destroy(CurrentActiveWeapon.gameObject);
        }

        CurrentActiveWeapon = newWeapon;
        var weapon = CurrentActiveWeapon as IWeapon;
        if (weapon == null)
        {
            Debug.LogWarning("[ActiveWeapon] Vũ khí mới không implement IWeapon!");
            return;
        }

        timeBetweenAttacks = weapon.GetWeaponInfo().weaponCooldown;

        string weaponName = weapon.GetWeaponInfo().weaponName;
        if (!string.IsNullOrEmpty(weaponName))
            AudioManager.Instance?.SetCurrentWeapon(weaponName);

        AttackCooldown();
    }

    private void AttackCooldown()
    {
        isAttacking = true;
        StopAllCoroutines();
        StartCoroutine(TimeBetweenAttacksRoutine());
    }

    private IEnumerator TimeBetweenAttacksRoutine()
    {
        yield return new WaitForSeconds(timeBetweenAttacks);
        isAttacking = false;
    }

    private void StartAttacking() => attackButtonDown = true;
    private void StopAttacking() => attackButtonDown = false;

    private void Attack()
    {
        if (attackButtonDown && !isAttacking && CurrentActiveWeapon)
        {
            AttackCooldown();
            var weapon = CurrentActiveWeapon as IWeapon;
            if (weapon == null) return;

            AudioManager.Instance?.PlayWeaponAttack();
            weapon.Attack();
        }
    }

    // ============================================================
    // 🔹 BỔ SUNG: HỖ TRỢ SAVE/LOAD VŨ KHÍ
    // ============================================================
    public string CurrentWeaponName
    {
        get
        {
            if (CurrentActiveWeapon == null) return null;
            var weapon = CurrentActiveWeapon as IWeapon;
            return weapon?.GetWeaponInfo()?.weaponName;
        }
    }

    public void EquipWeaponByName(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return;

        WeaponInfor weaponInfo = Resources.Load<WeaponInfor>($"Weapons/{weaponName}");
        if (weaponInfo == null)
        {
            Debug.LogWarning($"[ActiveWeapon] Không tìm thấy WeaponInfor cho {weaponName}");
            return;
        }

        GameObject weaponObj = Instantiate(weaponInfo.weaponPrefab);
        IWeapon newWeapon = weaponObj.GetComponent<IWeapon>();
        if (newWeapon == null)
        {
            Debug.LogError($"[ActiveWeapon] Prefab của {weaponName} không có script IWeapon!");
            Destroy(weaponObj);
            return;
        }

        NewWeapon(newWeapon as MonoBehaviour);
    }
}
