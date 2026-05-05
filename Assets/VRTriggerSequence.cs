using UnityEngine;
using System.Collections;

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

    [Header("Audio A（先播放）")]
    public AudioSource audioSourceA; // 第一段音频

    [Header("New Object")]
    public GameObject objectToEnable;
    public Animator objectAnimator;

    [Header("Audio B（后播放）")]
    public AudioSource audioSourceB; // 第二段音频

    private bool triggered = false;

    void Update()
    {
        if (triggered || player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= triggerDistance)
        {
            triggered = true;
            StartCoroutine(Sequence());
        }
    }

    IEnumerator Sequence()
    {
        Debug.Log("触发剧情开始");

        // 1️⃣ 两个角色停止挥手
        if (characterAnimator1 != null)
            characterAnimator1.SetTrigger(stopTriggerName);

        if (characterAnimator2 != null)
            characterAnimator2.SetTrigger(stopTriggerName);

        // 2️⃣ 关闭粒子
        if (effectToStop != null)
            effectToStop.Stop();

        // 3️⃣ 播放音频A
        if (audioSourceA != null)
        {
            audioSourceA.Play();
            yield return new WaitForSeconds(audioSourceA.clip.length);
        }

        // 4️⃣ 激活新物体
        if (objectToEnable != null)
            objectToEnable.SetActive(true);

        // 5️⃣ 播放新物体动画
        if (objectAnimator != null)
            objectAnimator.Play("Attack1");

        // 6️⃣ 播放音频B
        if (audioSourceB != null)
            audioSourceB.Play();

        Debug.Log("剧情执行完毕");
    }
}