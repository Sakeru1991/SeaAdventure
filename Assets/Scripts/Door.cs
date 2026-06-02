using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Door : MonoBehaviour
{
    private bool isPlayerInRange = false;
    private bool isPlayerHidden = false;
    private GameObject playerObject;
    private Transform originalParent;

    private StageMover stageMover;

    [Header("隠れる時間の設定")]
    public float hideTime = 5f;
    private const float maxHideTime = 5f;

    private Slider hideTimeSlider;
    private GameObject sliderObject;

    private void Start()
    {
        // --- 描画と表示の補正（敵と同じ仕組みで表示されない問題を回避） ---
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 10; // 強制的に手前に表示
        }

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, 0f);

        if (transform.parent != null)
        {
            // ステージなど親のスケールの影響を打ち消して、元の大きさを維持する
            Vector3 parentScale = transform.parent.lossyScale;
            if (parentScale.x != 0 && parentScale.y != 0)
            {
                float signX = Mathf.Sign(transform.localScale.x);
                float signY = Mathf.Sign(transform.localScale.y);
                // 仮の基準サイズ（必要に応じて1fなどに変更してください）
                Vector3 baseScale = new Vector3(1f, 1f, 1f); 
                
                transform.localScale = new Vector3(
                    (baseScale.x / Mathf.Abs(parentScale.x)) * signX, 
                    (baseScale.y / Mathf.Abs(parentScale.y)) * signY, 
                    1f
                );
            }
        }
        // -------------------------------------------------------------

        // シーン内のStageMoverを探す
        stageMover = FindFirstObjectByType<StageMover>();

        // スライダーを探す
        GameObject sliderObj = GameObject.Find("HideTimeSlider");
        if (sliderObj != null)
        {
            hideTimeSlider = sliderObj.GetComponent<Slider>();
            if (hideTimeSlider != null)
            {
                sliderObject = sliderObj;
                // 初期状態は非表示にするなどの調整が必要であればここで行う
                // 今回はUpdateで制御するため、ここでの非表示は必須ではないが、初期値設定をしておく
                hideTimeSlider.maxValue = 1f; // 正規化して使う
                hideTimeSlider.value = hideTime / maxHideTime;
                sliderObject.SetActive(false); // 最初は隠しておく
            }
        }
    }

    void Update()
    {
        // 隠れている時間の管理
        if (isPlayerHidden)
        {
            hideTime -= Time.deltaTime;
            if (hideTime <= 0f)
            {
                UnhidePlayer();
            }
        }
        else
        {
            hideTime += Time.deltaTime;
            if (hideTime > maxHideTime)
            {
                hideTime = maxHideTime;
            }
        }

        // スライダーの更新
        if (hideTimeSlider != null && sliderObject != null)
        {
            if (isPlayerHidden || (isPlayerInRange && hideTime < maxHideTime))
            {
                if (!sliderObject.activeSelf) sliderObject.SetActive(true);
                hideTimeSlider.value = hideTime / maxHideTime;
            }
            else
            {
                if (sliderObject.activeSelf) sliderObject.SetActive(false);
            }
        }

        if (Keyboard.current == null) return;

        // 隠れていない状態で、範囲内にいて、スペースキーが「押された瞬間」なら隠す
        // かつ、隠れる時間が残っている場合のみ
        if (!isPlayerHidden && isPlayerInRange && Keyboard.current.spaceKey.wasPressedThisFrame && hideTime > 0f)
        {
            HidePlayer();
        }
        // 隠れている状態で、スペースキーが離されたら元に戻す
        else if (isPlayerHidden && !Keyboard.current.spaceKey.isPressed)
        {
            UnhidePlayer();
        }
    }

    private void HidePlayer()
    {
        if (playerObject == null) return;

        isPlayerHidden = true;

        // 表示を無効化
        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // ステージの移動を停止（プレイヤーの移動不可）
        if (stageMover != null)
        {
            stageMover.canMove = false;
        }
    }

    private void UnhidePlayer()
    {
        if (playerObject == null) return;

        isPlayerHidden = false;

        // 表示を有効化
        SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // ステージの移動を再開
        if (stageMover != null)
        {
            stageMover.canMove = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log($"OnTriggerEnter2D: {other.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerObject = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            
            // ドアがプレイヤーから離れたら、隠れていたとしても強制的に表示する（隠れる場所がなくなったため）
            if (isPlayerHidden)
            {
                UnhidePlayer();
            }
            
            playerObject = null;
        }
    }
}
