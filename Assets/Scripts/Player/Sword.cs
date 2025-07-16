using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class Sword : MonoBehaviour
{
    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponCollider;
    [SerializeField] private float attackCooldown = 0.5f;
    private PlayerControls playerControls;
    private Animator animator;
    private ActiveWeapon activeWeapon;
    private PlayerController playerController;
    private GameObject slashAnim;
    private bool attackButtonDown, isAttacking = false;
    private void Awake()
    {
        playerControls = new PlayerControls();
        animator = GetComponent<Animator>();
        activeWeapon = GetComponentInParent<ActiveWeapon>();
        playerController = GetComponentInParent<PlayerController>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }
    void Start()
    {
        playerControls.Combat.Attack.started += _ => StartAttacking();
        playerControls.Combat.Attack.canceled += _ => StopAttacking();
    }
    private void Update()
    {
        MouseFollowWithOffSet();
        Attack();
    }
    private void StartAttacking()
    {
        attackButtonDown = true;
    }
    private void StopAttacking()
    {
        attackButtonDown = false;
    }
    private void Attack()
    {
        if (attackButtonDown && !isAttacking)
        {
            isAttacking = true;
            animator.SetTrigger("Attack");
            weaponCollider.gameObject.SetActive(true);
            slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
            slashAnim.transform.parent = this.transform.parent;
            StartCoroutine(ResetAttack());
        }
    }

    private IEnumerator ResetAttack()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }
    public void DoneAttackAnim()
    {
        weaponCollider.gameObject.SetActive(false);
        if (slashAnim != null)
        {
            Destroy(slashAnim, 0.5f);
        }
    }
    public void SwingUpFlipAnim()
    {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(-180, 0, 0);
        if (playerController.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }
    public void SwingDownFlipAnim()
    {
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (playerController.FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }
    private void MouseFollowWithOffSet()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(playerController.transform.position);

        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        if (mousePos.x < playerScreenPos.x)
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, -180, angle);
            weaponCollider.transform.rotation = Quaternion.Euler(0, -180, 0);
        }
        else
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, 0, angle);
            weaponCollider.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}
