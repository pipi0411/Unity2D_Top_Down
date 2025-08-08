using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Thêm dòng này

public class Staff : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfor weaponInfo;
    private void Update()
    {
        MouseFollowWithOffSet();
    }
    public void Attack()
    {
        Debug.Log("Staff attack executed.");
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