using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("生成する敵のプレハブ")]
    public GameObject enemyPrefab;

    [Tooltip("生成間隔（秒）")]
    public float spawnInterval = 5f;

    [Tooltip("生成位置のX座標の範囲（このオブジェクトからの相対距離）")]
    public float spawnRangeX = 10f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        // ランダムな位置を計算
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        Vector3 spawnPosition = new Vector3(transform.position.x + randomX, transform.position.y, 0f);

        // 敵を生成
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // 生成された敵を、このGeneratorの親（Stageなど）の子オブジェクトにする
        // これにより、ステージと一緒に移動するようになります
        if (transform.parent != null)
        {
            enemy.transform.SetParent(transform.parent);
        }
    }
}
