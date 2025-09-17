
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    public void DropItems(EnemyData enemyData)
    {
        int randomNum = Random.Range(1, 4);

        if (randomNum == 1)
        {
            int healthAmount = Random.Range(1, enemyData.maxHealthGlobes + 1);
            for (int i = 0; i < healthAmount; i++)
                Instantiate(enemyData.healthGlobe, transform.position, Quaternion.identity);
        }
        else if (randomNum == 2)
        {
            int staminaAmount = Random.Range(1, enemyData.maxStaminaGlobes + 1);
            for (int i = 0; i < staminaAmount; i++)
                Instantiate(enemyData.staminaGlobe, transform.position, Quaternion.identity);
        }
        else if (randomNum == 3)
        {
            int goldAmount = Random.Range(1, enemyData.maxGoldCoins + 1);
            for (int i = 0; i < goldAmount; i++)
                Instantiate(enemyData.goldCoin, transform.position, Quaternion.identity);
        }
    }
}
