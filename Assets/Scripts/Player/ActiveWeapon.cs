using System.Collections;
using UnityEngine;

public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon { get; private set; }

    private PlayerControls playerControls;
    private float timeBetweenAttacks;
    private bool attackButtonDown, isAttacking = false;

    [Header("Audio Settings")]
    [SerializeField] private string defaultSwitchSound = "WeaponSwitch";

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

        AudioManager.Instance?.PlayPlayerSfx(defaultSwitchSound);
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
}
