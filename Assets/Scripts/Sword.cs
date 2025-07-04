using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class Sword : MonoBehaviour
{
    private PlayerControls playerControls;
    private Animator animator;
    private ActiveWeapon activeWeapon;
    private PlayerController playerController;

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
        playerControls.Combat.Attack.started += _ => Attack();
    }
    private void Update()
    {
        MouseFollowWithOffSet();
    }
    private void Attack()
    {
        animator.SetTrigger("Attack");
    }
    private void MouseFollowWithOffSet()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(playerController.transform.position);

        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        if (mousePos.x < playerScreenPos.x)
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, -180, angle);
        }
        else
        {
            activeWeapon.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
