using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement_run : MonoBehaviour
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

    private void Awake()
    {
        playerTransform = this.transform;
        playerAnimator = GetComponentInChildren<Animator>();
        player_rb = GetComponent<Rigidbody>();
        groundLayer = LayerMask.GetMask("Ground");
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
        // 【可选】保留键盘Space键，方便测试
        // HandleJumpInput();
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

    // -------------------- 手势事件：右手比耶开始（模拟Space按下） --------------------
    public void OnRightVSignStart()
    {
        if (isLand && !isChargingJump)
        {
            // 地面上：开始蓄力
            isChargingJump = true;
            chargeStartTime = Time.time;
        }
        else if (!isLand && canDoubleJump)
        {
            // 空中：触发二段跳
            if (isChargingJump) CancelCharge();

            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerAnimator.SetTrigger("Jump");
            canDoubleJump = false;
        }
    }

    // -------------------- 手势事件：右手比耶结束（模拟Space抬起，释放蓄力） --------------------
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

    // 【可选】保留原来的键盘跳跃逻辑，方便测试
    /*
    void HandleJumpInput()
    {
        if (!isLand && canDoubleJump && Input.GetKeyDown(KeyCode.Space))
        {
            if (isChargingJump) CancelCharge();
            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerAnimator.SetTrigger("Jump");
            canDoubleJump = false;
            return; 
        }

        if (isLand && !isChargingJump && Input.GetKeyDown(KeyCode.Space))
        {
            isChargingJump = true;
            chargeStartTime = Time.time;
        }

        if (isChargingJump && !isLand)
        {
            CancelCharge();
        }

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
    */
}