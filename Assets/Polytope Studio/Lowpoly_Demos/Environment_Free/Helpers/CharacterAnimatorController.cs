using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 角色动画控制器（基于Animator系统，兼容Pico等VR平台）
/// 
/// 替代原来的VRCharacterWalkController（Legacy Animation），改用Animator状态机
/// 解决Pico平台动画丢失的问题
/// 
/// 使用前提：
/// 1. 角色必须挂载Animator组件
/// 2. 需要创建Animator Controller，包含以下状态和参数：
///    - bool "IsWalking"  → 控制走路/停止切换
///    - bool "IsTalking"  → 控制说话动画
///    - bool "IsIdle"     → 控制待机动画
///    - Trigger "SitDown" → 触发坐下/特殊动作
///    - float "Speed"     → 移动速度（可选，用于动画混合）
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterAnimatorController : MonoBehaviour
{
    [Header("=== 移动设置 ===")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 2f;

    [Tooltip("到达目标的停止距离阈值")]
    public float stopDistance = 0.15f;

    [Header("=== 旋转设置 ===")]
    [Tooltip("是否自动朝向目标方向")]
    public bool faceTarget = true;

    [Tooltip("转身速度（度/秒）")]
    public float rotationSpeed = 360f;

    [Header("=== 动画参数名（需与Animator Controller匹配） ===")]
    [Tooltip("走路参数名")]
    public string walkParamName = "IsWalking";
    [Tooltip("说话参数名")]
    public string talkParamName = "IsTalking";
    [Tooltip("待机参数名")]
    public string idleParamName = "IsIdle";
    [Tooltip("坐下触发器名")]
    public string sitTriggerName = "SitDown";
    [Tooltip("速度参数名（可选）")]
    public string speedParamName = "Speed";

    [Header("=== 到达目标事件 ===")]
    public UnityEvent onReachedTarget;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private Animator _animator;
    private Rigidbody _rb;
    private Vector3 _targetPosition;
    private bool _isMoving = false;
    private bool _hasReachedTarget = false;
    private bool _isInitialized = false;

    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving => _isMoving;

    /// <summary>
    /// 是否已到达目标
    /// </summary>
    public bool HasReachedTarget => _hasReachedTarget;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (_isInitialized) return;

        _animator = GetComponentInChildren<Animator>();
        _rb = GetComponent<Rigidbody>();

        if (_animator == null)
        {
            Debug.LogError($"[角色控制器] {gameObject.name} 未找到Animator组件！（请检查Animator是否在子物体上）");
            return;
        }

        // 确保Animator已启用
        _animator.enabled = true;

        // 确保不使用Root Motion（走路动画没有位移，通过脚本控制）
        _animator.applyRootMotion = false;

        // 处理Rigidbody
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log($"[角色控制器] {gameObject.name} 自动添加Rigidbody");
        }

        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 确保位置轴未冻结
        _rb.constraints &= ~RigidbodyConstraints.FreezePositionX;
        _rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        // 保持Y轴旋转自由
        _rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;

        _isInitialized = true;

        if (debugMode)
            Debug.Log($"[角色控制器] ✅ {gameObject.name} 初始化完成");
    }

    void FixedUpdate()
    {
        if (!_isMoving || _hasReachedTarget || _rb == null) return;

        Vector3 currentPosition = _rb.position;
        Vector3 direction = _targetPosition - currentPosition;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            OnReachedTarget();
            return;
        }

        // 旋转朝向目标
        if (faceTarget && distance > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _rb.MoveRotation(
                Quaternion.RotateTowards(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime)
            );
        }

        // 移动
        Vector3 moveDirection = direction / distance;
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = _rb.velocity.y;
        _rb.velocity = velocity;

        // 更新Animator速度参数
        if (_animator != null && !string.IsNullOrEmpty(speedParamName))
        {
            _animator.SetFloat(speedParamName, moveSpeed);
        }
    }

    /// <summary>
    /// 移动到指定位置
    /// </summary>
    public void MoveTo(Vector3 target, float speed = -1f)
    {
        Initialize();

        // 确保Rigidbody可移动（可能被外部设为kinematic）
        if (_rb != null)
            _rb.isKinematic = false;

        if (speed > 0f) moveSpeed = speed;

        _targetPosition = target;
        _hasReachedTarget = false;
        _isMoving = true;

        // 设置Animator走路状态
        SetWalking(true);

        if (debugMode)
        {
            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(target.x, 0, target.z)
            );
            Debug.Log($"[角色控制器] {gameObject.name} ▶ 开始移动 → 目标: {target}，距离: {distance:F2}");
        }
    }

    /// <summary>
    /// 停止移动
    /// </summary>
    public void StopMoving()
    {
        _isMoving = false;

        if (_rb != null)
        {
            _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
        }

        SetWalking(false);

        if (debugMode)
            Debug.Log($"[角色控制器] {gameObject.name} ⏹ 停止移动");
    }

    /// <summary>
    /// 设置走路状态
    /// </summary>
    public void SetWalking(bool isWalking)
    {
        if (_animator == null) return;

        _animator.SetBool(walkParamName, isWalking);

        if (isWalking)
        {
            // 走路时关闭待机和说话
            if (!string.IsNullOrEmpty(idleParamName))
                _animator.SetBool(idleParamName, false);
            if (!string.IsNullOrEmpty(talkParamName))
                _animator.SetBool(talkParamName, false);
        }
    }

    /// <summary>
    /// 设置说话状态
    /// </summary>
    public void SetTalking(bool isTalking)
    {
        if (_animator == null) return;

        _animator.SetBool(talkParamName, isTalking);

        if (isTalking)
        {
            // 开始说话时，关闭走路和待机（避免 Talk→Idle 弹回）
            if (!string.IsNullOrEmpty(walkParamName))
                _animator.SetBool(walkParamName, false);
            if (!string.IsNullOrEmpty(idleParamName))
                _animator.SetBool(idleParamName, false);
        }
        else
        {
            // 停止说话时，回到待机
            if (!string.IsNullOrEmpty(idleParamName))
                _animator.SetBool(idleParamName, true);
        }

        if (debugMode)
            Debug.Log($"[角色控制器] {gameObject.name} 说话状态: {(isTalking ? "开始" : "停止")}");
    }

    /// <summary>
    /// 设置待机状态
    /// </summary>
    public void SetIdle(bool isIdle)
    {
        if (_animator == null) return;

        _animator.SetBool(idleParamName, isIdle);

        if (isIdle)
        {
            // 待机时关闭走路和说话
            if (!string.IsNullOrEmpty(walkParamName))
                _animator.SetBool(walkParamName, false);
            if (!string.IsNullOrEmpty(talkParamName))
                _animator.SetBool(talkParamName, false);
        }
    }

    /// <summary>
    /// 触发坐下/特殊动作
    /// </summary>
    public void TriggerSitDown()
    {
        if (_animator == null) return;

        _animator.SetTrigger(sitTriggerName);

        if (debugMode)
            Debug.Log($"[角色控制器] {gameObject.name} ▶ 触发坐下动作");
    }

    /// <summary>
    /// 触发自定义Animator Trigger
    /// </summary>
    public void SetAnimatorTrigger(string triggerName)
    {
        if (_animator == null || string.IsNullOrEmpty(triggerName)) return;

        _animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// 设置自定义Animator Bool
    /// </summary>
    public void SetAnimatorBool(string paramName, bool value)
    {
        if (_animator == null || string.IsNullOrEmpty(paramName)) return;

        _animator.SetBool(paramName, value);
    }

    private void OnReachedTarget()
    {
        _isMoving = false;
        _hasReachedTarget = true;

        if (_rb != null)
        {
            _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            _rb.angularVelocity = Vector3.zero;
        }

        // 注意：不在这里关闭 IsWalking，让外部通过 SetTalking/SetIdle 来切换
        // 这样可以实现 Walk → Talk 的平滑过渡，避免中间闪一下 Idle

        if (debugMode)
            Debug.Log($"[角色控制器] {gameObject.name} ✅ 已到达目标位置");

        onReachedTarget?.Invoke();
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void ResetState()
    {
        _isMoving = false;
        _hasReachedTarget = false;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
        }

        SetWalking(false);
        SetTalking(false);
        SetIdle(false);
    }

    void OnDrawGizmosSelected()
    {
        if (_isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _targetPosition);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_targetPosition, stopDistance);
        }
    }
}
