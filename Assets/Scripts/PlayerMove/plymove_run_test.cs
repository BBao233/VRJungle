using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class plymove_run_test : MonoBehaviour
{
    private static Dictionary<Animator, float> noJudgeUntilTimes = new Dictionary<Animator, float>();
    bool is_rolePlay;
    bool is_tumblePlay;
    public float LowSpeed = 1.5f;
    public float UpSpeed = 2f;
    Transform playerTransform;
    Vector3 playerPosition;
    Animator playerAnimator;
    public GameObject groundcheck;
    public float jumpForce = 4f;
    Rigidbody player_rb;
    bool isLand;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool wasGrounded;
    public float maxJumpForceMultiplier = 2f;
    public float maxChargeTime = 0.5f;
    private bool isChargingJump = false;
    private float chargeStartTime = 0f;
    private bool canDoubleJump = true;

    // 引用换色脚本，用于键盘 J 键测试
    CaptureMaterial captureMaterialScript;

    private void Awake()
    {
        playerTransform = this.transform;
        playerAnimator = GetComponentInChildren<Animator>();
        player_rb = GetComponent<Rigidbody>();
        groundLayer = LayerMask.GetMask("Ground");

        // 获取同物体上的换色脚本引用
        captureMaterialScript = GetComponent<CaptureMaterial>();
    }

    void Start()
    {
        playerPosition = transform.position;
        wasGrounded = true;
    }

    void Update()
    {
        IsGroundedLand();
        Movement();

        // --- 键盘测试逻辑 ---
        HandleJumpInput(); // 空格跳跃
        HandleColorChangeInput(); // J键换色

        playerAnimator.SetBool("IsGrounded", isLand);
        if (isLand && !wasGrounded)
        {
            playerAnimator.SetTrigger("Land");
        }
        wasGrounded = isLand;
    }

    void Movement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? UpSpeed : LowSpeed;
        playerAnimator.SetBool("IsSpeedUp", Input.GetKey(KeyCode.LeftShift));

        Vector3 move = Vector3.left * speed;
        player_rb.velocity = new Vector3(move.x, player_rb.velocity.y, move.z);
        if (isLand)
        {
            playerAnimator.SetBool("IsSpeedUp", Input.GetKey(KeyCode.LeftShift));
        }
        else
        {
            playerAnimator.SetBool("IsSpeedUp", false);
        }
    }

    void IsGroundedLand()
    {
        bool wasLand = isLand;
        isLand = Physics.CheckSphere(groundcheck.transform.position, groundCheckRadius, groundLayer);
        if (isLand && !wasLand)
        {
            canDoubleJump = true;
        }
    }

    // -------------------- 键盘测试：J键切换颜色 --------------------
    void HandleColorChangeInput()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (captureMaterialScript != null)
            {
                // 调用你 CaptureMaterial 脚本里的手势触发函数，逻辑完全一致
                captureMaterialScript.OnLeftThumbsUp();
            }
        }
    }

    // -------------------- 键盘测试：空格跳跃逻辑 (与手势逻辑一致) --------------------
    void HandleJumpInput()
    {
        // 1. 空中二段跳判定
        if (!isLand && canDoubleJump && Input.GetKeyDown(KeyCode.Space))
        {
            if (isChargingJump) CancelCharge();
            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerAnimator.SetTrigger("Jump");
            canDoubleJump = false;
            return;
        }

        // 2. 地面开始蓄力
        if (isLand && !isChargingJump && Input.GetKeyDown(KeyCode.Space))
        {
            isChargingJump = true;
            chargeStartTime = Time.time;
        }

        // 3. 蓄力中途离地（例如滑落），取消蓄力
        if (isChargingJump && !isLand)
        {
            CancelCharge();
        }

        // 4. 松开空格释放跳跃
        if (isChargingJump && Input.GetKeyUp(KeyCode.Space))
        {
            float chargeDuration = Time.time - chargeStartTime;
            float t = Mathf.Clamp01(chargeDuration / maxChargeTime);
            float finalJumpForce = Mathf.Lerp(jumpForce, jumpForce * maxJumpForceMultiplier, t);

            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);

            playerAnimator.SetTrigger("Jump");
            CancelCharge();
        }
    }

    // -------------------- 手势事件：右手比耶开始 --------------------
    public void OnRightVSignStart()
    {
        if (isLand && !isChargingJump)
        {
            isChargingJump = true;
            chargeStartTime = Time.time;
        }
        else if (!isLand && canDoubleJump)
        {
            if (isChargingJump) CancelCharge();

            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerAnimator.SetTrigger("Jump");
            canDoubleJump = false;
        }
    }

    // -------------------- 手势事件：右手比耶结束 --------------------
    public void OnRightVSignEnd()
    {
        if (isChargingJump)
        {
            float chargeDuration = Time.time - chargeStartTime;
            float t = Mathf.Clamp01(chargeDuration / maxChargeTime);
            float finalJumpForce = Mathf.Lerp(jumpForce, jumpForce * maxJumpForceMultiplier, t);
            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);

            playerAnimator.SetTrigger("Jump");
            CancelCharge();
        }
    }

    void CancelCharge()
    {
        isChargingJump = false;
    }

    public static void JudgeCanrole(Animator playerAnimator, float cooldownAfterRoll)
    {
        if (playerAnimator == null) return;

        bool isTumblePlay = playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Jump_tumble");
        float currentTime = Time.time;

        if (!noJudgeUntilTimes.ContainsKey(playerAnimator))
            noJudgeUntilTimes[playerAnimator] = 0f;

        if (isTumblePlay)
        {
            noJudgeUntilTimes[playerAnimator] = currentTime + cooldownAfterRoll;
            playerAnimator.SetBool("Canrole", false);
        }
        else
        {
            if (currentTime < noJudgeUntilTimes[playerAnimator])
            {
                return;
            }
            else
            {
                playerAnimator.SetBool("Canrole", true);
            }
        }
    }
}