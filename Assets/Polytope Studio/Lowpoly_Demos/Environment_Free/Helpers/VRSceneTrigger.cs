using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// VR场景事件触发器
/// 当VR玩家进入指定区域时触发事件（支持一次性和重复触发）
/// </summary>
public class VRSceneTrigger : MonoBehaviour
{
    [Header("=== 触发器设置 ===")]
    [Tooltip("触发区域中心（世界坐标），不设置则使用当前物体位置")]
    public Transform triggerCenter;

    [Tooltip("触发半径")]
    public float triggerRadius = 1f;

    [Tooltip("VR玩家/相机的Transform（通常挂Head或Camera）")]
    public Transform vrPlayerTransform;

    [Header("=== 触发模式 ===")]
    [Tooltip("一次性触发：触发后自动禁用")]
    public bool triggerOnce = true;

    [Tooltip("是否在场景开始时就检测（否则等待启用）")]
    public bool activeOnStart = true;

    [Header("=== 事件 ===")]
    public UnityEvent onTriggerEnter;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private bool _hasTriggered = false;
    private bool _isActive = false;

    void Start()
    {
        _isActive = activeOnStart;

        if (triggerCenter == null)
            triggerCenter = transform;

        if (vrPlayerTransform == null)
        {
            // 尝试自动查找VR相机
            Camera cam = Camera.main;
            if (cam != null)
                vrPlayerTransform = cam.transform;
            else
                Debug.LogError("[VR触发器] 未设置VR玩家Transform，也未找到Main Camera！");
        }
    }

    void Update()
    {
        if (!_isActive || _hasTriggered || vrPlayerTransform == null) return;

        float distance = Vector3.Distance(
            new Vector3(vrPlayerTransform.position.x, 0, vrPlayerTransform.position.z),
            new Vector3(triggerCenter.position.x, 0, triggerCenter.position.z)
        );

        if (distance <= triggerRadius)
        {
            Trigger();
        }
    }

    /// <summary>
    /// 手动触发
    /// </summary>
    public void Trigger()
    {
        if (_hasTriggered && triggerOnce) return;

        _hasTriggered = true;

        if (debugMode)
            Debug.Log($"[VR触发器] ✅ 触发器 \"{gameObject.name}\" 已触发！");

        onTriggerEnter?.Invoke();

        if (triggerOnce)
        {
            _isActive = false;
        }
    }

    /// <summary>
    /// 重置触发器（允许再次触发）
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
        _isActive = true;
    }

    /// <summary>
    /// 启用/禁用触发器
    /// </summary>
    public void SetActive(bool active)
    {
        _isActive = active;
        if (active && triggerOnce)
            _hasTriggered = false;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = triggerCenter != null ? triggerCenter.position : transform.position;
        Gizmos.color = _hasTriggered ? Color.gray : Color.green;
        Gizmos.DrawWireSphere(center, triggerRadius);

        // 画一个向上的标记
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center + Vector3.up * 2f);
    }
}
