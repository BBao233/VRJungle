using UnityEngine;

public class VisionGameDirector : MonoBehaviour
{
    [Header("场景光源")]
    public Light sceneLight;

    [Header("亮度设置")]
    public float normalIntensity = 0.2f;

    public float visionIntensity = 1.5f;

    [Header("激光控制")]
    public FingerBeamCaster beamCaster;

    [Header("Boss管理")]
    public BossSpawnerSequence bossSpawner;

    [Header("手势冷却")]
    public float gestureCooldown = 0.4f;

    private float lastGestureTime = 0f;

    // 默认环境亮度
    private void Awake()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = normalIntensity;
        }
    }

    // 开启夜视
    public void EnableNightVision()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = visionIntensity;
        }

        Debug.Log("夜视仪开启");
    }

    // 关闭夜视
    public void DisableNightVision()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = normalIntensity;
        }

        Debug.Log("夜视仪关闭");
    }

    // 射击手势
    public void OnShootGesture()
    {
        Debug.Log("检测到射击手势");

        // 冷却
        if (Time.time - lastGestureTime < gestureCooldown)
        {
            return;
        }

        lastGestureTime = Time.time;

        // 查找Boss
        GameObject boss =
            GameObject.FindGameObjectWithTag("Boss");

        if (boss != null)
        {
            Debug.Log("找到Boss：" + boss.name);

            // 调用Boss死亡
            if (bossSpawner != null)
            {
                bossSpawner.KillBoss(boss);
            }
        }
        else
        {
            Debug.Log("没有找到Boss");
        }
    }
}