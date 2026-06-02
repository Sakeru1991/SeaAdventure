using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI;

public class StageMover : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("ステージが移動する速度")]
    public float moveSpeed = 5f;

    [Tooltip("ダッシュ時の速度倍率")]
    public float dashMultiplier = 2.0f;

    [Header("ダッシュ時間の設定")]
    public float dashTime = 5f;
    private const float maxDashTime = 5f;

    [HideInInspector]
    public bool canMove = true; // 追加：移動可能かどうかのフラグ

    private Slider dashTimeSlider;
    private GameObject sliderObject;

    private void Start()
    {
        // スライダーを探す
        GameObject sliderObj = GameObject.Find("DashTimeSlider");
        if (sliderObj != null)
        {
            dashTimeSlider = sliderObj.GetComponent<Slider>();
            if (dashTimeSlider != null)
            {
                sliderObject = sliderObj;
                dashTimeSlider.maxValue = 1f; // 正規化して使う
                dashTimeSlider.value = dashTime / maxDashTime;
                sliderObject.SetActive(false); // 最初は隠しておく
            }
        }
    }

    void Update()
    {
        float horizontalInput = 0f;

        // キーボード入力の取得（移動可能な時のみ）
        if (canMove && Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            {
                horizontalInput = -1f;
            }
            else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                horizontalInput = 1f;
            }
        }

        // ゲームパッド入力の取得（オプション）
        if (canMove && Gamepad.current != null && horizontalInput == 0f)
        {
            horizontalInput = Gamepad.current.leftStick.x.ReadValue();
        }

        bool isTryingToDash = canMove && Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        bool isDashing = false;

        // 現在の速度を計算、かつダッシュ可能か判定
        float currentSpeed = moveSpeed;
        if (isTryingToDash && dashTime > 0f)
        {
            currentSpeed *= dashMultiplier;
            isDashing = true;
        }

        // ダッシュ時間の管理とスライダーの更新処理
        // （enabled が true である限り、canMoveに関わらず毎フレーム実行される）
        ManageDashTime(isDashing);

        // 移動可能な時だけステージを動かす
        if (canMove)
        {
            // 移動ベクトルの計算
            // プレイヤーの移動をシミュレートするため、ステージを入力とは【逆】方向に移動させます
            Vector3 movement = new Vector3(-horizontalInput, 0f, 0f) * currentSpeed * Time.deltaTime;

            // ステージに移動を適用
            transform.Translate(movement);
        }
    }

    private void ManageDashTime(bool isDashing)
    {
        // ダッシュ時間の管理
        if (isDashing)
        {
            dashTime -= Time.deltaTime;
            if (dashTime < 0f)
            {
                dashTime = 0f;
            }
        }
        else
        {
            // ダッシュしていない間は常に回復させる
            dashTime += Time.deltaTime;
            if (dashTime > maxDashTime)
            {
                dashTime = maxDashTime;
            }
        }

        // スライダーの更新
        if (dashTimeSlider != null && sliderObject != null)
        {
            // ダッシュ中である、またはダッシュしていないが時間が完全回復していない場合は表示
            if (isDashing || dashTime < maxDashTime)
            {
                if (!sliderObject.activeSelf) sliderObject.SetActive(true);
                dashTimeSlider.value = dashTime / maxDashTime;
            }
            else
            {
                // 最大値まで回復したら非表示にする
                if (sliderObject.activeSelf) sliderObject.SetActive(false);
            }
        }
    }
}
