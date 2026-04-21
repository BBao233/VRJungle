using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    public CharacterController controller;
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;

    [Tooltip("加速度 — 越大起步越快")]
    public float acceleration = 8f;
    [Tooltip("地面阻力 — 越大刹车越快")]
    public float groundFriction = 10f;
    [Tooltip("空中控制力 — 建议比地面小")]
    public float airControl = 2f;
    [Tooltip("空中阻力 — 建议比地面小很多")]
    public float airFriction = 0.5f;

    [Header("重力与跳跃")]
    public float gravity = -18f;          // 比物理默认值大，跳跃感更好
    public float jumpHeight = 2.5f;
    public float maxFallSpeed = 25f;
    [Tooltip("跳跃后下落额外重力倍率 — 让跳跃弧线更自然")]
    public float fallGravityMultiplier = 1.8f;

    [Header("Coyote Time & 跳跃缓冲")]
    [Tooltip("离开平台后仍能跳跃的时间窗口")]
    public float coyoteTime = 0.12f;
    [Tooltip("提前按跳跃键的缓冲时间")]
    public float jumpBufferTime = 0.12f;

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundDistance = 0.35f;
    public LayerMask groundMask;

    [Header("摄像机")]
    public Transform cameraTransform;     // 用摄像机朝向决定移动方向

    // ── 内部状态 ──────────────────────────────────────────────
    private Vector3 horizontalVelocity;   // 单独管理水平速度（有惯性）
    private float verticalVelocity;     // 单独管理垂直速度
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;

    // 平滑输入用
    private Vector2 rawInput;
    private Vector2 smoothInput;
    private Vector2 smoothInputVel;
    [Tooltip("输入平滑时间 — 模拟人腿的启动/停步延迟")]
    public float inputSmoothTime = 0.12f;

    void Update()
    {
        // 输入读取放 Update，保证响应帧率
        rawInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 跳跃缓冲：提前按下 → 记录缓冲时间，落地时自动触发
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // ── 1. 地面检测 ──────────────────────────────────────
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
            coyoteCounter = coyoteTime;   // 落地 → 重置缓冲
        else
            coyoteCounter -= dt;

        // ── 2. 平滑输入（模拟人腿惯性）──────────────────────
        smoothInput = Vector2.SmoothDamp(
            smoothInput, rawInput, ref smoothInputVel, inputSmoothTime);

        // 用摄像机水平朝向计算世界空间移动方向
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 wishDir = (camForward * smoothInput.y + camRight * smoothInput.x);
        if (wishDir.magnitude > 1f) wishDir.Normalize();

        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // ── 3. 加速度 + 阻力（地面/空中分开）────────────────
        if (isGrounded)
        {
            // 目标速度向量
            Vector3 targetVelocity = wishDir * targetSpeed;

            // 用加速度逼近目标速度
            Vector3 velocityDiff = targetVelocity - horizontalVelocity;
            float accel = wishDir.magnitude > 0.01f ? acceleration : groundFriction;
            horizontalVelocity += velocityDiff * (accel * dt);

            // 静止时额外施加阻力，模拟刹车
            if (rawInput.magnitude < 0.01f)
            {
                float frictionForce = groundFriction * dt;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, frictionForce * horizontalVelocity.magnitude);
            }
        }
        else
        {
            // 空中：控制力弱，保留大部分惯性
            Vector3 airAccel = wishDir * (airControl * dt);
            horizontalVelocity += airAccel;

            // 空中阻力（很小，只轻微衰减）
            horizontalVelocity *= Mathf.Clamp01(1f - airFriction * dt);

            // 限制空中水平速度不能超过目标速度太多（防止借助跳跃加速）
            if (horizontalVelocity.magnitude > targetSpeed * 1.2f)
                horizontalVelocity = horizontalVelocity.normalized * targetSpeed * 1.2f;
        }

        // ── 4. 跳跃（Coyote Time + 跳跃缓冲）───────────────
        bool canJump = coyoteCounter > 0f;
        bool wantsJump = jumpBufferCounter > 0f;

        if (canJump && wantsJump)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteCounter = 0f;   // 消耗一次
            jumpBufferCounter = 0f;
        }

        // ── 5. 重力（下落时额外倍率，弧线更自然）────────────
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;  // 轻微贴地
        }
        else
        {
            float gravMult = verticalVelocity < 0f ? fallGravityMultiplier : 1f;
            verticalVelocity += gravity * gravMult * dt;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
        }

        // ── 6. 合并一次 Move ──────────────────────────────────
        Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(finalVelocity * dt);

        // ── 7. 角色朝向跟随移动方向（可选）──────────────────
        if (horizontalVelocity.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontalVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * dt);
        }
    }
}