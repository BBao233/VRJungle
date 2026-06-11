using UnityEngine;

public class WarriorRandomMove : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 1.5f;
    public Vector3 moveAreaCenter = new Vector3(0, 0, 5);
    public Vector3 moveAreaSize = new Vector3(4, 0, 4);
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("移动优化")]
    public float arrivalDistance = 0.3f;
    public float stopThreshold = 0.05f;

    [Header("地面设置")]
    public float groundY = 0f;

    [Header("模型朝向")]
    public Vector3 modelRotationOffset = new Vector3(0, 90f, 0);

    [Header("转向玩家（流畅设置）")]
    public float turnSpeed = 8f;
    public float turnCompleteAngle = 15f;
    public float maxTurnTime = 1.5f;

    [Header("动画设置")]
    public Animator warriorAnimator;
    public float baseballAnimDuration = 4.0f;  // 投球动画时长，根据实际动画调整

    // 动画状态常量
    private const int ANIM_IDLE = 0;
    private const int ANIM_RUNNING = 1;
    private const int ANIM_BASEBALL = 2;

    private Rigidbody rb;
    private Vector3 targetPos;
    private bool isWaiting = false;
    private bool isMoving = false;
    private float currentWaitTime = 0f;

    // 转向专用
    private bool isTurningToPlayer = false;
    private Transform targetPlayer;
    private float turnStartTime;

    // 动画专用
    private bool isPlayingBaseballAnim = false;
    private float baseballAnimEndTime = 0f;
    private bool pendingThrowAction = false;  // 待执行的扔球动作

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.drag = 2f;
        rb.angularDrag = 5f;
        rb.useGravity = false;

        if (warriorAnimator == null)
            warriorAnimator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        groundY = moveAreaCenter.y;

        Vector3 startPos = transform.position;
        startPos.y = groundY;
        transform.position = startPos;

        SetAnimationState(ANIM_IDLE);
        PickNewRandomPoint();
    }

    void FixedUpdate()
    {
        // 🔴 优先级最高：正在播放投球动画时，绝对不能移动
        if (isPlayingBaseballAnim)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 检查动画是否播放完成
            if (Time.time >= baseballAnimEndTime)
            {
                OnBaseballAnimFinished();
            }
            return;
        }

        if (isTurningToPlayer)
        {
            TurnToPlayerSmoothly();
            return;
        }

        if (isWaiting)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            SetAnimationState(ANIM_IDLE);
            return;
        }

        if (!isMoving)
        {
            SetAnimationState(ANIM_IDLE);
            return;
        }

        Vector3 currentFlat = new Vector3(rb.position.x, 0, rb.position.z);
        Vector3 targetFlat = new Vector3(targetPos.x, 0, targetPos.z);
        float distance = Vector3.Distance(currentFlat, targetFlat);

        if (distance <= arrivalDistance)
        {
            OnArrivedAtTarget();
            return;
        }

        Vector3 moveDirection = (targetFlat - currentFlat).normalized;
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = 0;

        rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, 0.5f);

        Vector3 pos = rb.position;
        pos.y = groundY;
        rb.position = pos;

        if (moveDirection.magnitude > 0.1f && rb.velocity.magnitude > 0.1f)
        {
            SetAnimationState(ANIM_RUNNING);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            targetRotation *= Quaternion.Euler(modelRotationOffset);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.3f));
        }
        else
        {
            SetAnimationState(ANIM_IDLE);
        }
    }

    void TurnToPlayerSmoothly()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (targetPlayer == null) return;

        Vector3 dir = targetPlayer.position - transform.position;
        dir.y = 0;

        if (dir.magnitude < 0.1f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        targetRot *= Quaternion.Euler(modelRotationOffset);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
    }

    public void StartTurnToPlayer(Transform player)
    {
        isTurningToPlayer = true;
        targetPlayer = player;
        turnStartTime = Time.time;
        SetAnimationState(ANIM_IDLE);
    }

    public bool IsTurnCompleted()
    {
        if (targetPlayer == null) return true;

        if (Time.time - turnStartTime > maxTurnTime)
        {
            Debug.LogWarning("⚠️ 转向超时，强制完成");
            return true;
        }

        Vector3 dir = targetPlayer.position - transform.position;
        dir.y = 0;

        float angle = Vector3.Angle(transform.forward, dir);
        return angle < turnCompleteAngle;
    }

    /// <summary>
    /// 转向完成后调用此方法（由 BallThrower 调用）
    /// 开始播放投球动画
    /// </summary>
    public void StartBaseballAnimation()
    {
        isTurningToPlayer = false;  // 结束转向状态
        isPlayingBaseballAnim = true;
        isMoving = false;

        // 彻底停止刚体
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 播放投球动画
        SetAnimationState(ANIM_BASEBALL);

        // 记录动画结束时间
        baseballAnimEndTime = Time.time + baseballAnimDuration;

        Debug.Log($"🎬 开始播放投球动画，将在 {baseballAnimDuration} 秒后结束");
    }

    /// <summary>
    /// 设置一个待执行的扔球动作（在动画播放中途执行）
    /// </summary>
    public void ScheduleThrowAction(float delay, System.Action action)
    {
        pendingThrowAction = true;
        Invoke(nameof(ExecuteThrowAction), delay);

        // 保存动作到临时变量（简化处理，用协程更合适）
        StartCoroutine(ExecuteDelayedAction(delay, action));
    }

    private System.Collections.IEnumerator ExecuteDelayedAction(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        if (action != null)
        {
            action();
        }
    }

    private void ExecuteThrowAction()
    {
        pendingThrowAction = false;
    }

    /// <summary>
    /// 投球动画完成后的回调
    /// </summary>
    private void OnBaseballAnimFinished()
    {
        isPlayingBaseballAnim = false;
        isTurningToPlayer = false;

        Debug.Log("✅ 投球动画播放完毕，恢复移动");

        // 取消任何可能还在等待的 Invoke
        CancelInvoke(nameof(PickNewRandomPoint));

        // 重置等待状态
        isWaiting = false;
        isMoving = false;

        // 立即恢复移动
        PickNewRandomPoint();
    }

    private void SetAnimationState(int state)
    {
        if (warriorAnimator != null && warriorAnimator.isActiveAndEnabled)
        {
            warriorAnimator.SetInteger("State", state);
        }
    }

    void OnArrivedAtTarget()
    {
        isMoving = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isWaiting = true;
        currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
        Invoke(nameof(PickNewRandomPoint), currentWaitTime);

        SetAnimationState(ANIM_IDLE);
    }

    void PickNewRandomPoint()
    {
        // 只有在动画播放中才阻止（防御性代码）
        if (isPlayingBaseballAnim)
        {
            Debug.LogWarning("PickNewRandomPoint 被调用时动画还在播放，延迟0.1秒重试");
            Invoke(nameof(PickNewRandomPoint), 0.1f);
            return;
        }

        float halfX = moveAreaSize.x / 2f;
        float halfZ = moveAreaSize.z / 2f;

        Vector3 newTarget;
        int attempts = 0;

        do
        {
            newTarget = new Vector3(
                moveAreaCenter.x + Random.Range(-halfX, halfX),
                groundY,
                moveAreaCenter.z + Random.Range(-halfZ, halfZ)
            );
            attempts++;
            if (attempts > 10) break;
        }
        while (Vector3.Distance(newTarget, transform.position) < 0.5f && attempts < 10);

        targetPos = newTarget;
        isWaiting = false;
        isMoving = true;

        Debug.Log($"🎯 恢复移动，目标点: {targetPos}，当前动画状态: {(isPlayingBaseballAnim ? "播放中" : "空闲")}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(moveAreaCenter, moveAreaSize);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPos, 0.3f);

            if (isMoving && !isWaiting)
            {
                Gizmos.color = Color.green;
                Vector3 dir = (targetPos - transform.position).normalized;
                Gizmos.DrawRay(transform.position, dir * 1f);
            }
        }
    }

    public void ResetMovement()
    {
        CancelInvoke(nameof(PickNewRandomPoint));
        isWaiting = false;
        isMoving = false;
        isPlayingBaseballAnim = false;
        isTurningToPlayer = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        groundY = moveAreaCenter.y;
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;

        PickNewRandomPoint();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isMoving && !isWaiting && collision.relativeVelocity.magnitude > 0.5f)
        {
            Vector3 avoidDir = (transform.position - collision.transform.position).normalized;
            avoidDir.y = 0;
            rb.position += avoidDir * 0.2f;
        }
    }
}