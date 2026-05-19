using UnityEngine;

public class VisionGameDirector : MonoBehaviour
{
    [Header("场景光源")]
    public Light sceneLight;

    [Header("亮度设置")]
    public float normalIntensity = 0.2f; // 原环境亮度
    public float visionIntensity = 1.5f; // 夜视亮度

    [Header("激光控制")]
    public FingerBeamCaster beamCaster;

    [Header("游戏参数")]
    public TargetGenerator targetFactory;
    public float gestureCooldown = 0.4f;

    private bool isSessionLive = false;
    private float lastGestureTime = 0f;

    // 默认保持原环境亮度
    private void Awake()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = normalIntensity;
        }
    }

    // 开始游戏
    public void ActivateVisionMode()
    {
        if (isSessionLive) return;

        isSessionLive = true;

        targetFactory?.StartSequence();

        Debug.Log("目标游戏开始");
    }

    // ?? 开启夜视
    public void EnableNightVision()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = visionIntensity;
        }

        Debug.Log("夜视仪开启");
    }

    // ?? 关闭夜视
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
        if (!isSessionLive) return;
        if (Time.time - lastGestureTime < gestureCooldown) return;

        GameObject target = GameObject.FindGameObjectWithTag("TargetObject");

        if (target != null)
        {
            target.SetActive(false);

            lastGestureTime = Time.time;

            Debug.Log("手势触发消除成功");

            bool hasMore = targetFactory?.SpawnNextTarget() ?? false;

            if (!hasMore)
            {
                Debug.Log("达到目标总数，游戏结束");

                EndGame();
            }
        }
    }

    // 游戏结束
    public void EndGame()
    {
        if (!isSessionLive) return;

        isSessionLive = false;

        if (beamCaster != null)
        {
            beamCaster.ExtinguishBeam();
        }

        targetFactory?.ClearAll();

        Debug.Log("游戏结束");
    }
}