using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private WeaponInfor weaponInfo;
    public WeaponInfor GetWeaponInfor() => weaponInfo;
}
