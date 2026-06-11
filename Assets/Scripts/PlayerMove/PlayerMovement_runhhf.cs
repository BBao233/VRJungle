using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement_runhhf : MonoBehaviour
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
    public float jumpForce = 4f; // 固定跳跃力度
    Rigidbody player_rb;
    bool isLand;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool wasGrounded;
    private bool canDoubleJump = true; // 保留二段跳

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
        HandleJumpInput(); // 键盘测试跳跃
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
            canDoubleJump = true; // 落地重置二段跳
        }
    }

    // -------------------- 手势事件：右手比耶 = 普通跳跃（无蓄力） --------------------
    public void OnRightVSignStart()
    {
        // 地面：第一次跳跃
        if (isLand)
        {
            NormalJump();
        }
        // 空中：二段跳
        else if (!isLand && canDoubleJump)
        {
            NormalJump();
            canDoubleJump = false;
        }
    }

    // 【删除了蓄力结束方法 OnRightVSignEnd】

    /// <summary>
    /// 通用普通跳跃方法（地面/二段跳共用）
    /// </summary>
    void NormalJump()
    {
        // 重置垂直速度，防止叠加
        player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
        // 施加固定跳跃力
        player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        // 触发跳跃动画
        playerAnimator.SetTrigger("Jump");
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

    // -------------------- 键盘测试：普通跳跃 --------------------
    void HandleJumpInput()
    {
        // 按下空格触发跳跃
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 地面跳跃
            if (isLand)
            {
                NormalJump();
            }
            // 空中二段跳
            else if (!isLand && canDoubleJump)
            {
                NormalJump();
                canDoubleJump = false;
            }
        }
    }
}