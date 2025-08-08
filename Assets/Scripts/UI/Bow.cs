using UnityEngine;
using System.Collections;

public class Bow : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfor weaponInfo;

    public void Attack()
    {
        Debug.Log("Bow attack executed.");
    }

    public WeaponInfor GetWeaponInfo()
    {
        return weaponInfo;
    }
}