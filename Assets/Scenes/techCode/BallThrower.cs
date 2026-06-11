using UnityEngine;

public class BallThrower : MonoBehaviour
{
    [Header("扔球基础设置")]
    public GameObject ballPrefab;
    public Transform throwSpawnPoint;
    public float throwInterval = 8f;
    public Transform playerTarget;

    [Header("投球动画时机")]
    [Tooltip("动画播放后多少秒实际扔球")]
    public float throwDelayInAnim = 0.4f;

    [Header("动态投掷参数")]
    [Tooltip("最小投掷距离（过近不投）")]
    public float minThrowDistance = 2f;
    [Tooltip("最大投掷距离（过远不投）")]
    public float maxThrowDistance = 15f;
    [Tooltip("投掷初速度基数（核心参数，控制整体力度）")]
    public float throwSpeedBase = 10f;
    [Tooltip("角度随机偏移范围（±值，单位：度）")]
    public float angleRandomRange = 1.5f; // 缩小随机偏移，提升精准度
    [Tooltip("力度随机偏移范围（±百分比）")]
    [Range(0f, 0.1f)] public float powerRandomRange = 0.015f; // 降低随机偏移
    [Tooltip("基础抛物线高度系数（0.05~0.8，越大弧度越高）")]
    [Range(0.05f, 0.8f)] public float baseArcHeightFactor = 0.3f;

    [Header("距离分段校准（新增）")]
    [Tooltip("近距离阈值（小于此值为近距离投掷）")]
    public float closeDistanceThreshold = 4f;
    [Tooltip("远距离阈值（大于此值为远距离投掷）")]
    public float farDistanceThreshold = 10f;
    [Tooltip("近距离弧度衰减系数（越小越贴地）")]
    [Range(0.1f, 0.5f)] public float closeArcFactor = 0.2f;
    [Tooltip("远距离速度补偿系数（大于1提升速度）")]
    [Range(1.0f, 1.4f)] public float farSpeedFactor = 1.2f;

    private WarriorRandomMove warriorMove;
    private bool canThrow = false;
    private float timer;
    private PermanentFloorFrame frame;
    private bool hasFirstBallThrown = false;

    void Start()
    {
        // 自动查找玩家
        if (playerTarget == null)
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 默认生成点为自身位置
        if (throwSpawnPoint == null)
            throwSpawnPoint = transform;

        warriorMove = GetComponent<WarriorRandomMove>();

        // 查找地框（支持未激活物体）
        FindFrame();
    }

    void FindFrame()
    {
        // 方法1：查找所有包含未激活的PermanentFloorFrame
        PermanentFloorFrame[] frames = FindObjectsOfType<PermanentFloorFrame>(true);
        if (frames.Length > 0)
        {
            frame = frames[0];
            Debug.Log($"✅ 成功找到地框（未激活也能找到）: {frame.gameObject.name}");
            return;
        }

        // 方法2：通过物体名查找
        GameObject pointObj = GameObject.Find("point");
        if (pointObj != null)
        {
            frame = pointObj.GetComponent<PermanentFloorFrame>();
            if (frame != null)
            {
                Debug.Log($"✅ 通过名称找到地框: point");
                return;
            }
        }

        // 延迟重试
        Debug.LogWarning("⚠️ 查找地框失败，0.5秒后重试");
        Invoke(nameof(FindFrame), 0.5f);
    }

    void Update()
    {
        if (playerTarget == null) return;

        timer += Time.deltaTime;

        if (timer >= throwInterval)
        {
            timer = 0;
            PrepareToThrow();
        }

        // 转向完成后执行投掷
        if (canThrow && warriorMove != null && warriorMove.IsTurnCompleted())
        {
            canThrow = false;
            StartThrowSequence();
        }
    }

    void PrepareToThrow()
    {
        if (warriorMove == null || playerTarget == null) return;

        // 计算距离，判断是否在有效范围内（仅水平距离）
        Vector3 flatWarriorPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatPlayerPos = new Vector3(playerTarget.position.x, 0, playerTarget.position.z);
        float horizontalDistance = Vector3.Distance(flatWarriorPos, flatPlayerPos);

        if (horizontalDistance < minThrowDistance || horizontalDistance > maxThrowDistance)
        {
            Debug.Log($"📏 玩家水平距离{horizontalDistance:F1}米，超出有效投掷范围（{minThrowDistance}-{maxThrowDistance}米），跳过本次投掷");
            return;
        }

        // 转向玩家
        warriorMove.StartTurnToPlayer(playerTarget);
        canThrow = true;
    }

    void StartThrowSequence()
    {
        if (warriorMove == null) return;

        // 播放投球动画
        warriorMove.StartBaseballAnimation();
        // 延迟后执行实际投掷
        Invoke(nameof(ThrowBallAtPlayer), throwDelayInAnim);
    }

    void ThrowBallAtPlayer()
    {
        if (ballPrefab == null || playerTarget == null) return;

        // 计算水平距离（忽略Y轴，精准判断投掷范围）
        Vector3 flatSpawnPos = new Vector3(throwSpawnPoint.position.x, 0, throwSpawnPoint.position.z);
        Vector3 flatPlayerPos = new Vector3(playerTarget.position.x, 0, playerTarget.position.z);
        float horizontalDistance = Vector3.Distance(flatSpawnPos, flatPlayerPos);

        // 再次校验距离（双重保险）
        if (horizontalDistance < minThrowDistance || horizontalDistance > maxThrowDistance)
        {
            Debug.Log($"🚫 投掷时距离无效（水平距离{horizontalDistance:F1}米），跳过投球");
            return;
        }

        // 计算目标位置（缩小随机偏移，提升精准度）
        Vector3 targetPos = playerTarget.position;
        targetPos += new Vector3(
            Random.Range(-0.08f, 0.08f),
            0,
            Random.Range(-0.08f, 0.08f)
        );

        // 计算投掷初速度（优化后的精准算法）
        Vector3 throwVelocity = CalculateAccurateProjectileVelocity(throwSpawnPoint.position, targetPos, horizontalDistance);

        // 实例化球
        GameObject ball = Instantiate(ballPrefab, throwSpawnPoint.position, Quaternion.identity);
        BallProjectile projectile = ball.GetComponent<BallProjectile>();

        if (projectile != null)
        {
            projectile.SetTargetAndVelocity(playerTarget, throwVelocity);
        }

        Debug.Log($"⚾ 扔出球 | 水平距离:{horizontalDistance:F1}米 | 速度:{throwVelocity.magnitude:F1}m/s | 竖直速度:{throwVelocity.y:F1}m/s");

        // 第一次扔球激活地框
        if (!hasFirstBallThrown)
        {
            if (frame == null)
            {
                FindFrame();
            }

            if (frame != null)
            {
                frame.StartGameAndShowFrame();
                hasFirstBallThrown = true;
                Debug.Log("✅ 第一次扔球，地框已激活");
            }
            else
            {
                Debug.LogError("❌ 地框引用为空！请确保场景中有 point 物体并挂载 PermanentFloorFrame 脚本");
            }
        }
    }

    /// <summary>
    /// 优化版：精准计算抛物线投掷初速度（适配近/远距离校准）
    /// </summary>
    /// <param name="startPos">投掷起点</param>
    /// <param name="endPos">投掷终点</param>
    /// <param name="horizontalDistance">水平距离</param>
    /// <returns>精准的初速度向量</returns>
    private Vector3 CalculateAccurateProjectileVelocity(Vector3 startPos, Vector3 endPos, float horizontalDistance)
    {
        // 1. 基础配置
        float gravity = Physics.gravity.magnitude; // 重力加速度（默认9.81）
        Vector3 horizontalDir = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z).normalized;

        // 2. 距离分段校准（核心优化）
        float speedMultiplier = 1f;
        float arcFactor = baseArcHeightFactor;

        // 近距离校准：降低弧度 + 降低速度
        if (horizontalDistance <= closeDistanceThreshold)
        {
            arcFactor = closeArcFactor;
            speedMultiplier = Mathf.Lerp(0.7f, 0.95f, horizontalDistance / closeDistanceThreshold);
        }
        // 远距离校准：提升速度 + 适度提高弧度
        else if (horizontalDistance >= farDistanceThreshold)
        {
            arcFactor = Mathf.Lerp(baseArcHeightFactor, baseArcHeightFactor * 1.3f, (horizontalDistance - farDistanceThreshold) / (maxThrowDistance - farDistanceThreshold));
            speedMultiplier = farSpeedFactor;
        }
        // 中距离：线性过渡
        else
        {
            arcFactor = Mathf.Lerp(closeArcFactor, baseArcHeightFactor, (horizontalDistance - closeDistanceThreshold) / (farDistanceThreshold - closeDistanceThreshold));
            speedMultiplier = Mathf.Lerp(0.95f, 1f, (horizontalDistance - closeDistanceThreshold) / (farDistanceThreshold - closeDistanceThreshold));
        }

        // 3. 计算目标高度（适配距离分段的弧度）
        float heightDiff = endPos.y - startPos.y;
        float arcHeight = horizontalDistance * arcFactor; // 弧度高度与距离+分段系数关联
        float targetHeight = heightDiff + arcHeight;

        // 4. 精准计算水平速度（基于物理公式）
        float baseHorizontalSpeed = throwSpeedBase * speedMultiplier;
        // 动态适配距离的水平速度（确保飞行时间合理）
        float horizontalSpeed = baseHorizontalSpeed * Mathf.Sqrt(horizontalDistance / maxThrowDistance) * speedMultiplier;
        // 加入微小力度随机（降低偏移）
        float randomPower = Random.Range(1 - powerRandomRange, 1 + powerRandomRange);
        horizontalSpeed *= randomPower;

        // 5. 计算飞行时间（水平方向匀速）
        float flightTime = horizontalDistance / horizontalSpeed;
        // 安全校验：避免飞行时间过短/过长
        flightTime = Mathf.Clamp(flightTime, 0.2f, 2f);

        // 6. 精准计算竖直速度（物理公式反向推导）
        // 公式：h = v_y * t - 0.5 * g * t² → v_y = (h + 0.5*g*t²) / t
        float verticalSpeed = (targetHeight + 0.5f * gravity * flightTime * flightTime) / flightTime;
        // 角度随机偏移（缩小范围，提升精准度）
        float angleOffset = Random.Range(-angleRandomRange, angleRandomRange) * Mathf.Deg2Rad;
        verticalSpeed *= Mathf.Sin(Mathf.PI / 4 + angleOffset); // 45度基础角 ± 微小偏移
        horizontalSpeed *= Mathf.Cos(angleOffset);

        // 7. 组装最终速度向量
        Vector3 finalVelocity = horizontalDir * horizontalSpeed;
        finalVelocity.y = verticalSpeed;

        // 8. 最终安全校验（避免速度异常）
        if (finalVelocity.magnitude < 2f)
        {
            finalVelocity = horizontalDir * throwSpeedBase * 0.9f;
            finalVelocity.y = throwSpeedBase * 0.3f;
        }

        return finalVelocity;
    }
}