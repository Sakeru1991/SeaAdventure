using UnityEngine;
using System.Collections.Generic;

public class DoorGenerator : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("生成するドアのプレハブ")]
    public GameObject doorPrefab;

    [Tooltip("生成するドアの数")]
    public int numberOfDoors = 3;

    [Tooltip("生成位置のX座標の範囲（このオブジェクトからの相対距離）")]
    public float spawnRangeX = 20f;

    [Tooltip("ドア同士の最低間隔（重なり防止）")]
    public float minDistanceBetweenDoors = 4f;

    [Tooltip("配置試行の最大回数（無限ループ防止）")]
    public int maxSpawnAttempts = 100;

    private void Start()
    {
        GenerateDoors();
    }

    private void GenerateDoors()
    {
        if (doorPrefab == null)
        {
            Debug.LogWarning("DoorGenerator: ドアのプレハブが設定されていません！Inspectorで設定してください。");
            return;
        }

        List<float> spawnedXPositions = new List<float>();

        for (int i = 0; i < numberOfDoors; i++)
        {
            float spawnX = 0f;
            bool positionFound = false;

            // 指定した回数まで、重ならない位置を探す
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                spawnX = Random.Range(-spawnRangeX, spawnRangeX);
                bool isOverlapping = false;

                // 既に生成されたドアとの距離をチェック
                foreach (float pos in spawnedXPositions)
                {
                    if (Mathf.Abs(pos - spawnX) < minDistanceBetweenDoors)
                    {
                        isOverlapping = true; // 他のドアと重なっている
                        break;
                    }
                }

                // 重なっていなければ位置を確定
                if (!isOverlapping)
                {
                    positionFound = true;
                    spawnedXPositions.Add(spawnX);
                    break;
                }
            }

            if (positionFound)
            {
                // ドアの生成座標を計算
                Vector3 spawnPosition = new Vector3(transform.position.x + spawnX, transform.position.y, 0f);
                
                // ドアを生成
                GameObject door = Instantiate(doorPrefab, spawnPosition, Quaternion.identity);

                // 生成されたドアを、このGeneratorの親（通常はStage）の子オブジェクトにする
                // これにより、ステージの移動に追従し、自立移動は行いません
                if (transform.parent != null)
                {
                    door.transform.SetParent(transform.parent);
                }
            }
            else
            {
                Debug.LogWarning("DoorGenerator: ドアを配置できるスペースが見つかりませんでした。配置数や範囲を見直してください。");
            }
        }
    }
}
