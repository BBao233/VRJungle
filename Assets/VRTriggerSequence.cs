using UnityEngine;

public class VRTriggerSequence : MonoBehaviour
{
    [Header("Player")]
    public Transform player; // 建议拖 Main Camera

    [Header("Trigger Settings")]
    public float triggerDistance = 2.0f;

    [Header("Character Animators（两个模型）")]
    public Animator characterAnimator1;
    public Animator characterAnimator2;
    public string stopTriggerName = "StopWave";

    [Header("Effect")]
    public ParticleSystem effectToStop;

    [Header("New Object")]
    public GameObject objectToEnable;
    public Animator objectAnimator;

    private bool triggered = false;

    void Update()
    {
        if (triggered || player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= triggerDistance)
        {
            TriggerEvent();
        }
    }

    void TriggerEvent()
    {
        triggered = true;

        Debug.Log("触发剧情：停止挥手");

        // 1️⃣ 两个角色停止挥手 → 进入待机
        if (characterAnimator1 != null)
        {
            characterAnimator1.SetTrigger(stopTriggerName);
        }

        if (characterAnimator2 != null)
        {
            characterAnimator2.SetTrigger(stopTriggerName);
        }

        // 2️⃣ 关闭粒子效果
        if (effectToStop != null)
        {
            effectToStop.Stop();
        }

        // 3️⃣ 激活新物体
        if (objectToEnable != null)
        {
            objectToEnable.SetActive(true);
        }

        // 4️⃣ 播放新物体动画
        if (objectAnimator != null)
        {
            objectAnimator.Play(0);
        }
    }
}