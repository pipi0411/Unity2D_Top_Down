using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    /// Các GameObject đại diện cho các vật phẩm có thể được tạo ra khi nhặt, bao gồm đồng vàng, quả cầu máu và quả cầu stamina.
    /// Sử dụng SerializeField để cho phép gán các GameObject từ Unity Inspector.
    [SerializeField] private GameObject goldCoin, healthGlobe, staminaGlobe;
    /// Hàm xử lý việc tạo (spawn) các vật phẩm ngẫu nhiên tại vị trí của đối tượng.
    /// Tùy thuộc vào một số ngẫu nhiên, hàm sẽ tạo ra một quả cầu máu, quả cầu stamina hoặc một số lượng ngẫu nhiên đồng vàng.
    public void DropItems()
    {
        /// Tạo một số ngẫu nhiên từ 1 đến 4 để quyết định loại vật phẩm sẽ được tạo.
        int randomNum = Random.Range(1, 5);
        /// Nếu số ngẫu nhiên là 1, tạo một quả cầu máu (healthGlobe) tại vị trí của đối tượng
        /// với hướng mặc định (Quaternion.identity).
        if (randomNum == 1)
        {
            Instantiate(healthGlobe, transform.position, Quaternion.identity);
        }
        /// Nếu số ngẫu nhiên là 2, tạo một quả cầu stamina (staminaGlobe) tại vị trí của đối tượng
        /// với hướng mặc định (Quaternion.identity).
        if (randomNum == 2)
        {
            Instantiate(staminaGlobe, transform.position, Quaternion.identity);
        }
        /// Nếu số ngẫu nhiên là 3, tạo một số lượng ngẫu nhiên (từ 1 đến 3) đồng vàng (goldCoin)
        /// tại vị trí của đối tượng với hướng mặc định (Quaternion.identity).
        if (randomNum == 3)
        {
            /// Tạo số lượng ngẫu nhiên từ 1 đến 3 để quyết định số đồng vàng sẽ được tạo.
            int randomAmountOfGold = Random.Range(1, 4);
            /// Lặp lại để tạo từng đồng vàng theo số lượng ngẫu nhiên đã xác định.
            for (int i = 0; i < randomAmountOfGold; i++)
            {
                Instantiate(goldCoin, transform.position, Quaternion.identity);
            }
        }
    }
}
