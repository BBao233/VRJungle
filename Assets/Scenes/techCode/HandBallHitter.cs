using UnityEngine;

public class HandBallHitter : MonoBehaviour
{
    [Header("击球设置")]
    public float hitForce = 8f;
    public float upwardForce = 0.18f;
    public float hitCooldown = 0.2f;
    public float randomAngleRange = 5f;

    private float lastHitTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (Time.time - lastHitTime < hitCooldown)
                return;

            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                // ✅ 正确：击球方向 = 从手 指向 球（真实碰撞方向）
                Vector3 hitDir = (other.transform.position - transform.position).normalized;

                // ✅ 保留水平方向，只微调Y（抛物线）
                hitDir.y = upwardForce;

                // ✅ 加一点随机
                hitDir = Quaternion.Euler(
                    0,
                    Random.Range(-randomAngleRange, randomAngleRange),
                    0
                ) * hitDir;

                hitDir.Normalize();

                rb.velocity = Vector3.zero;
                rb.AddForce(hitDir * hitForce, ForceMode.VelocityChange);

                lastHitTime = Time.time;
            }
        }
    }
}