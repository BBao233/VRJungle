using UnityEngine;
using System.Collections;

/// <summary>
/// 玩家引导控制器
/// 在VR中引导玩家移动到指定位置（显示引导标记、光柱等）
/// 
/// 使用方式：
/// 1. 创建引导标记物体（如箭头、光柱、粒子效果等）
/// 2. 挂载此脚本
/// 3. 调用 ShowGuide() 显示引导，HideGuide() 隐藏引导
/// </summary>
public class PlayerGuideController : MonoBehaviour
{
    [Header("=== 引导标记设置 ===")]
    [Tooltip("默认引导标记预制体（如箭头、光柱等）")]
    public GameObject defaultGuideMarker;

    [Tooltip("VR玩家Transform（用于检测距离）")]
    public Transform vrPlayerTransform;

    [Header("=== 引导行为 ===")]
    [Tooltip("到达目标后是否自动隐藏引导")]
    public bool hideOnArrival = true;

    [Tooltip("到达目标的距离阈值")]
    public float arrivalDistance = 1f;

    [Tooltip("引导标记高度偏移")]
    public float markerHeightOffset = 0f;

    [Tooltip("引导标记是否朝向玩家")]
    public bool facePlayer = true;

    [Header("=== 脉冲动画 ===")]
    [Tooltip("是否启用脉冲动画（引导标记上下浮动）")]
    public bool enablePulseAnimation = true;

    [Tooltip("脉冲速度")]
    public float pulseSpeed = 2f;

    [Tooltip("脉冲幅度")]
    public float pulseAmplitude = 0.3f;

    [Header("=== 事件 ===")]
    [Tooltip("玩家到达引导目标时触发")]
    public UnityEngine.Events.UnityEvent onPlayerArrived;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private GameObject _currentMarker;
    private Vector3 _targetPosition;
    private bool _isGuiding = false;
    private Vector3 _baseMarkerPosition;

    /// <summary>
    /// 是否正在引导
    /// </summary>
    public bool IsGuiding => _isGuiding;

    void Start()
    {
        if (vrPlayerTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                vrPlayerTransform = cam.transform;
        }
    }

    void Update()
    {
        if (!_isGuiding || _currentMarker == null || vrPlayerTransform == null) return;

        // 检测玩家是否到达
        float distance = Vector3.Distance(
            new Vector3(vrPlayerTransform.position.x, 0, vrPlayerTransform.position.z),
            new Vector3(_targetPosition.x, 0, _targetPosition.z)
        );

        if (distance <= arrivalDistance)
        {
            if (debugMode)
                Debug.Log("[玩家引导] ✅ 玩家已到达引导目标");

            onPlayerArrived?.Invoke();

            if (hideOnArrival)
                HideGuide();
        }

        // 脉冲动画
        if (enablePulseAnimation)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            _currentMarker.transform.position = _baseMarkerPosition + Vector3.up * pulse;
        }

        // 朝向玩家
        if (facePlayer)
        {
            Vector3 lookDir = vrPlayerTransform.position - _currentMarker.transform.position;
            lookDir.y = 0;
            if (lookDir.magnitude > 0.01f)
            {
                _currentMarker.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    /// <summary>
    /// 显示引导
    /// </summary>
    /// <param name="target">引导目标位置</param>
    /// <param name="customMarker">自定义引导标记（null=使用默认）</param>
    public void ShowGuide(Vector3 target, GameObject customMarker = null)
    {
        _targetPosition = target;

        // 清除旧标记
        if (_currentMarker != null)
        {
            Destroy(_currentMarker);
        }

        // 创建新标记
        GameObject markerPrefab = customMarker != null ? customMarker : defaultGuideMarker;
        if (markerPrefab == null)
        {
            Debug.LogError("[玩家引导] 未设置引导标记预制体！");
            return;
        }

        _currentMarker = Instantiate(markerPrefab, target + Vector3.up * markerHeightOffset, Quaternion.identity);
        _baseMarkerPosition = _currentMarker.transform.position;
        _isGuiding = true;

        if (debugMode)
            Debug.Log($"[玩家引导] 📍 显示引导 → 目标: {target}");
    }

    /// <summary>
    /// 隐藏引导
    /// </summary>
    public void HideGuide()
    {
        _isGuiding = false;

        if (_currentMarker != null)
        {
            Destroy(_currentMarker);
            _currentMarker = null;
        }

        if (debugMode)
            Debug.Log("[玩家引导] 隐藏引导");
    }

    /// <summary>
    /// 更新引导目标位置
    /// </summary>
    public void UpdateGuideTarget(Vector3 newTarget)
    {
        _targetPosition = newTarget;

        if (_currentMarker != null)
        {
            _baseMarkerPosition = newTarget + Vector3.up * markerHeightOffset;
            _currentMarker.transform.position = _baseMarkerPosition;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_isGuiding)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_targetPosition, arrivalDistance);
            Gizmos.DrawLine(_targetPosition, _targetPosition + Vector3.up * 3f);
        }
    }
}
