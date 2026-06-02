using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [Header("追従設定")]
    [Tooltip("スクロール速度の倍率（1 = ステージと同じ速度。0.5 = 半分の速度で奥行きを表現）")]
    [Range(0f, 1f)]
    public float parallaxFactor = 1f;

    [Header("ループ設定")]
    [Tooltip("背景を無限ループさせる場合はチェック")]
    public bool isLooping = true;

    private Transform stageTransform;
    private Vector3 lastStagePosition;
    private float textureUnitSizeX;

    void Start()
    {
        // シーン内のStageMover（ステージの本体）を自動で探す
        StageMover mover = FindFirstObjectByType<StageMover>();
        if (mover != null)
        {
            stageTransform = mover.transform;
            lastStagePosition = stageTransform.position;
        }
        else
        {
            Debug.LogWarning("BackgroundScroller: StageMoverが見つかりません。");
        }

        // スプライトの横幅を取得（ループ用）
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            textureUnitSizeX = spriteRenderer.sprite.texture.width / spriteRenderer.sprite.pixelsPerUnit;
            // スケールを考慮
            textureUnitSizeX *= transform.lossyScale.x;
        }
    }

    void LateUpdate()
    {
        if (stageTransform == null) return;

        // ステージの移動量を計算
        Vector3 deltaMovement = stageTransform.position - lastStagePosition;
        
        // ステージの移動量に倍率をかけて背景を移動させる
        transform.position += new Vector3(deltaMovement.x * parallaxFactor, 0f, 0f);
        
        // 次のフレームの計算用に位置を保存
        lastStagePosition = stageTransform.position;

        // ループ処理
        if (isLooping && textureUnitSizeX > 0)
        {
            // カメラ（原点）と背景画像の距離
            // ※このゲームはカメラではなくステージが動くため、原点(カメラ位置と仮定)との相対距離で判定します
            float distanceFromOrigin = transform.position.x; // カメラ位置が(0,0,0)前提

            // 背景が画像の幅の半分以上ずれたら、位置をリセットする
            if (distanceFromOrigin < -textureUnitSizeX)
            {
                transform.position = new Vector3(transform.position.x + textureUnitSizeX * 2f, transform.position.y, transform.position.z);
            }
            else if (distanceFromOrigin > textureUnitSizeX)
            {
                transform.position = new Vector3(transform.position.x - textureUnitSizeX * 2f, transform.position.y, transform.position.z);
            }
        }
    }
}
