using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    private Transform target;
    private Rigidbody rb;

    [Header("物理参数")]
    [Tooltip("重力倍数（越大下落越快，近距建议1.0，远距建议1.1~1.2）")]
    public float gravityMultiplier = 1.05f;
    [Tooltip("空气阻力（微小阻力提升真实感）")]
    public float airResistance = 0.08f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 手动控制重力，避免Unity内置重力的不稳定性
            rb.useGravity = false;
            rb.drag = airResistance;
            rb.angularDrag = 0.1f; // 增加旋转阻力，更真实
        }
    }

    void FixedUpdate()
    {
        // 精准应用重力（基于物理帧）
        if (rb != null && !rb.useGravity)
        {
            // 随速度衰减重力（近距更贴地，远距更自然）
            float dynamicGravity = gravityMultiplier * (rb.velocity.magnitude > 8f ? 1.1f : 1.0f);
            rb.AddForce(Physics.gravity * dynamicGravity, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// 设置投掷目标和初始速度
    /// </summary>
    /// <param name="targetTransform">目标位置</param>
    /// <param name="initialVelocity">初始速度</param>
    public void SetTargetAndVelocity(Transform targetTransform, Vector3 initialVelocity)
    {
        target = targetTransform;

        if (rb != null && target != null)
        {
            rb.velocity = initialVelocity;
            // 给球一个微小的旋转（视觉优化）
            rb.AddTorque(Random.onUnitSphere * 2f, ForceMode.Impulse);
        }

        // 延长销毁时间（适配远距离投掷）
        Destroy(gameObject, 7f);
    }
}