using UnityEngine;

/// <summary>
/// Quản lý việc tạo ra các vật phẩm khi nhặt.
/// </summary>
public class PickUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goldCoin, healthGlobe, staminaGlobe;
    [Header("Số lượng tối đa mỗi loại vật phẩm khi spawn")]
    [SerializeField] private int maxGoldCoins = 5;
    [SerializeField] private int maxHealthGlobes = 2;
    [SerializeField] private int maxStaminaGlobes = 2;

    /// <summary>
    /// Tạo vật phẩm ngẫu nhiên tại vị trí hiện tại.
    /// </summary>
    public void DropItems()
    {
        int randomNum = Random.Range(1, 4);

        if (randomNum == 1)
        {
            int healthAmount = Random.Range(1, maxHealthGlobes + 1);
            for (int i = 0; i < healthAmount; i++)
            {
                Instantiate(healthGlobe, transform.position, Quaternion.identity);
            }
        }
        if (randomNum == 2)
        {
            int staminaAmount = Random.Range(1, maxStaminaGlobes + 1);
            for (int i = 0; i < staminaAmount; i++)
            {
                Instantiate(staminaGlobe, transform.position, Quaternion.identity);
            }
        }
        if (randomNum == 3)
        {
            int goldAmount = Random.Range(1, maxGoldCoins + 1);
            for (int i = 0; i < goldAmount; i++)
            {
                Instantiate(goldCoin, transform.position, Quaternion.identity);
            }
        }
    }
}