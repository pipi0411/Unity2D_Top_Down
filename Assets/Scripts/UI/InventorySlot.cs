using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private WeaponInfor weaponInfor;
    public WeaponInfor GetWeaponInfor() => weaponInfor;
}
