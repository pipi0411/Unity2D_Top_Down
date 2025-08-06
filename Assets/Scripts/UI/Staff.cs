using UnityEngine;
using System.Collections;

public class Staff : MonoBehaviour, IWeapon
{
    public void Attack()
    {
        Debug.Log("Staff attack executed.");
        ActiveWeapon.Instance.ToggleIsAttacking(false);
    }
}