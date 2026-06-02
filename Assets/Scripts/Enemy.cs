using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("巡回時の速度")]
    public float patrolSpeed = 3f; // PL通常(5)より遅く
    [Tooltip("追跡時の速度")]
    public float chaseSpeed = 7.5f; // PL通常(5)より速く、PLダッシュ(10)より遅く

    [Header("検知設定")]
    [Tooltip("プレイヤーを検知する範囲")]
    public float detectionRange = 8f;
    [Tooltip("追跡を諦める範囲（画面外など）")]
    public float loseSightRange = 12f;

    [Header("巡回設定")]
    [Tooltip("振り返る間隔の最小値（秒）")]
    public float minTurnInterval = 5f;
    [Tooltip("振り返る間隔の最大値（秒）")]
    public float maxTurnInterval = 20f;

    [Header("消滅設定")]
    [Tooltip("プレイヤーからこの距離以上離れたら消滅する")]
    public float destroyRange = 50f;

    [Header("崖検知設定")]
    [Tooltip("足元確認用のレイの始点オフセット（X方向）")]
    public float groundCheckOffsetX = 0.5f;
    [Tooltip("足元確認用のレイの長さ")]
    public float groundCheckDistance = 2.0f; // 1.0 -> 2.0 に変更（ピボット位置による判定ミス防止）
    [Tooltip("地面とみなすレイヤー")]
    public LayerMask groundLayer;

    private Transform playerTransform;
    private SpriteRenderer playerRenderer;
    private float turnTimer;
    private bool isChasing = false;
    private int direction = 1; // 1: Right, -1: Left

    private void Start()
    {
        // Ground Layerの設定確認
        if (groundLayer.value == 0)
        {
            Debug.LogError("【Enemyエラー】Ground Layerが設定されていません！InspectorでEnemyの 'Ground Layer' を設定してください。");
        }

        // プレイヤーを探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRenderer = player.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogWarning("プレイヤーが見つかりません！プレイヤーに 'Player' タグがついているか確認してください。");
        }

        turnTimer = Random.Range(minTurnInterval, maxTurnInterval);

        // --- 画像表示の不具合修正 ---
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 強制的に手前に表示する（Order in Layer = 10）
            sr.sortingOrder = 10;
        }

        // Z座標を強制的に0（カメラに映る位置）にする
        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, 0f);

        // --- スケール（大きさ）の補正処理 ---
        // 親オブジェクトの影響で画像が歪む（細くなる・太くなる）のを防ぐため、
        // 「ワールドでの大きさが1x1」になるように自動計算します。
        
        // 元の向き（符号）を保持
        float signX = Mathf.Sign(transform.localScale.x);
        float signY = Mathf.Sign(transform.localScale.y);

        // 基準サイズを (0.5, 0.5, 1) と設定（大きすぎるため半分に）
        Vector3 baseScale = new Vector3(0.5f, 0.5f, 1f); 

        // 親がいる場合のみ補正
        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.lossyScale;
            // ゼロ除算防止
            if (parentScale.x != 0 && parentScale.y != 0)
            {
                // 親のスケールで割ることで、親の影響を打ち消す
                Vector3 newLocalScale = new Vector3(
                    (baseScale.x / Mathf.Abs(parentScale.x)) * signX, 
                    (baseScale.y / Mathf.Abs(parentScale.y)) * signY, 
                    1f
                );
                transform.localScale = newLocalScale;
            }
        }
        // -----------------------------
        // -----------------------------
    }

    // private void SnapToGround() ... （呼び出し元を消したのでメソッド定義は残しても良いが、混乱を避けるため放置または削除。今回はStartの呼び出しだけ消せばOK）
    private void SnapToGround()
    {
        // ... (省略、使用しない)
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // プレイヤーの状態を確認（隠れているか？）
        bool isPlayerHidden = !playerRenderer.enabled;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 状態遷移ロジック
        if (isChasing)
        {
            // 追跡中
            if (isPlayerHidden || distanceToPlayer > loseSightRange)
            {
                // 隠れたか、遠すぎる場合は追跡終了
                isChasing = false;
                // 巡回に戻る際、すぐに振り返らないようにタイマーをリセット
                turnTimer = Random.Range(minTurnInterval, maxTurnInterval);
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            // 巡回中
            // 隠れていない AND 範囲内 AND 前方にいる 場合のみ追跡
            float xDifference = playerTransform.position.x - transform.position.x;
            if (!isPlayerHidden && distanceToPlayer <= detectionRange && xDifference * direction > 0)
            {
                isChasing = true;
            }
            else
            {
                Patrol();
            }
        }

        // プレイヤーから離れすぎたら消滅（クローン対策）
        if (distanceToPlayer > destroyRange)
        {
            Destroy(gameObject);
        }
    }



    private void Patrol()
    {
        // 移動前に崖チェック
        if (!CheckGround())
        {
            TurnAround();
            return;
        }

        // 移動
        transform.Translate(Vector3.right * direction * patrolSpeed * Time.deltaTime);
        
        // 向きを更新
        UpdateFacing();

        // 定期的に振り返る
        turnTimer -= Time.deltaTime;
        if (turnTimer <= 0)
        {
            TurnAround();
            turnTimer = Random.Range(minTurnInterval, maxTurnInterval);
        }
    }

    private void ChasePlayer()
    {
        // プレイヤーの方向を向く
        if (playerTransform.position.x > transform.position.x)
        {
            direction = 1;
        }
        else
        {
            direction = -1;
        }

        // 移動前に崖チェック
        if (!CheckGround())
        {
            // Debug.Log("追跡中だが足元がないため停止"); 
            return; 
        }

        // 移動
        transform.Translate(Vector3.right * direction * chaseSpeed * Time.deltaTime);
        
        // 向きを更新
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        // 進行方向に向きを変える（画像の左右反転）
        // 元の画像が右向きであることを想定
        Vector3 localScale = transform.localScale;
        if ((direction > 0 && localScale.x < 0) || (direction < 0 && localScale.x > 0))
        {
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }

    private bool CheckGround()
    {
        // 進行方向の少し先、かつ少し上から下に向かってレイを飛ばす
        // 始点を少し高くする (0.5f -> 1.0f) ことで、多少の段差や埋まりに対応
        Vector3 rayOrigin = transform.position + new Vector3(groundCheckOffsetX * direction, 1.0f, 0);
        
        int mask = groundLayer.value != 0 ? groundLayer.value : Physics2D.AllLayers;

        // レイの距離も少し長くする (groundCheckDistance + 1.0f)
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, groundCheckDistance + 1.0f, mask);

        // --- デバッグ用ログ出力 (原因特定用) ---
        // Debug.Log($"[CheckGround] Mask: {mask}, Origin: {rayOrigin}, Hits: {hits.Length}");
        
        if (hits.Length == 0)
        {
             return false;
        }

        foreach (var hit in hits)
        {
            // 自分自身とTriggerは無視。Playerも地面ではないので無視。
            if (hit.collider.gameObject != gameObject && !hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        
        return false;
    }

    private void OnDrawGizmos()
    {
        // 検知範囲（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 視界の方向（黄色い線）
        Vector3 viewLine = Vector3.right * direction * detectionRange;
        Gizmos.DrawLine(transform.position, transform.position + viewLine);

        // 追跡解除範囲（赤色）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseSightRange);

        // 消滅範囲（グレー）
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, destroyRange);

        // 崖検知レイ（ヒットしたら緑、ヒットしなければ赤）
        Vector3 rayOrigin = transform.position + new Vector3(groundCheckOffsetX * direction, 0.5f, 0);
        bool isHit = false;
        if (Application.isPlaying) // 実行中のみ正確な判定
        {
             isHit = CheckGround();
        }
        
        Gizmos.color = isHit ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * (groundCheckDistance + 0.5f));
    }

    private void TurnAround()
    {
        direction *= -1;
        // Debug.Log("TurnAround: 崖または壁、あるいは時間経過で振り返りました");
    }

    // 画面端や壁に当たった場合の振り返り処理
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ステージの端や壁のタグを設定しておくと良いですが、
        // ここではとりあえず「プレイヤー以外」かつ「トリガーでないもの」などに当たったら振り返る簡易実装にします
        // 必要に応じてタグ判定を追加してください
        if (!other.CompareTag("Player") && !other.isTrigger)
        {
            TurnAround();
        }
        // プレイヤーに触れたら追跡開始
        else if (other.CompareTag("Player"))
        {
            isChasing = true;
        }
    }

    // 物理的な衝突（壁など）での振り返り処理
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // プレイヤーとの衝突は追跡開始
        if (collision.gameObject.CompareTag("Player"))
        {
            isChasing = true;
            return;
        }

        // 接地点の法線を確認して、壁（横方向の衝突）の場合のみ振り返る
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 法線のY成分が小さい（垂直に近い壁）場合
            if (Mathf.Abs(contact.normal.y) < 0.5f)
            {
                TurnAround();
                break;
            }
        }
    }
}
