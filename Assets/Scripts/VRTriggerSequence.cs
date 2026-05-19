using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class VRTriggerSequence : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Trigger Settings")]
    public float triggerDistance = 2.0f;

    [Header("NPC Animators")]
    public Animator characterAnimator1;
    public Animator characterAnimator2;
    public string stopTriggerName = "StopWave";

    [Header("Effect")]
    public ParticleSystem effectToStop;

    [Header("Audio A")]
    public AudioSource audioSourceA;

    [Header("Run Audio")]
    public AudioSource runAudioSource;

    [Header("Boss Animator")]
    public Animator objectAnimator;

    [Header("Boss Move")]
    public float bossMoveSpeed = 3f;

    [Header("Animation Names")]
    public string runAnimation = "RunForward";
    public string attackAnimation = "Attack1";

    [Header("Run Duration")]
    public float runDuration = 2f;

    [Header("Attack Hit Time")]
    public float attackHitTime = 0.6f;

    [Header("Audio B（攻击音效）")]
    public AudioSource audioSourceB;

    [Header("Audio C（击飞音效）")]
    public AudioSource audioSourceC;

    [Header("Knockback")]
    public float knockbackDistance = 5f;
    public float knockbackHeight = 2f;
    public float knockbackDuration = 1f;

    [Header("Fade To Black")]
    public Image fadeImage;
    public float fadeDuration = 2f;

    [Header("Scene Load")]
    public string nextSceneName;

    private bool triggered = false;

    void Update()
    {
        if (triggered || player == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        if (distance <= triggerDistance)
        {
            triggered = true;
            StartCoroutine(Sequence());
        }
    }

    IEnumerator Sequence()
    {
        Debug.Log("剧情开始");

        // 1️⃣ NPC停止挥手
        if (characterAnimator1 != null)
            characterAnimator1.SetTrigger(stopTriggerName);

        if (characterAnimator2 != null)
            characterAnimator2.SetTrigger(stopTriggerName);

        // 2️⃣ 停止粒子
        if (effectToStop != null)
            effectToStop.Stop();

        // 3️⃣ 播放音频A
        if (audioSourceA != null)
        {
            audioSourceA.Play();

            if (audioSourceA.clip != null)
            {
                yield return new WaitForSeconds(
                    audioSourceA.clip.length
                );
            }
        }

        // 4️⃣ Boss转向玩家
        if (objectAnimator != null && player != null)
        {
            Vector3 lookPos = player.position;

            lookPos.y =
                objectAnimator.transform.position.y;

            objectAnimator.transform.LookAt(lookPos);
        }

        // 5️⃣ RunForward + 前冲 + 跑步音效
        if (objectAnimator != null)
        {
            objectAnimator.Play(runAnimation);

            // 播放跑步音效
            if (runAudioSource != null)
            {
                runAudioSource.loop = true;
                runAudioSource.Play();
            }

            float elapsed = 0f;

            while (elapsed < runDuration)
            {
                elapsed += Time.deltaTime;

                objectAnimator.transform.position +=
                    objectAnimator.transform.forward *
                    bossMoveSpeed *
                    Time.deltaTime;

                yield return null;
            }

            // 停止跑步音效
            if (runAudioSource != null)
            {
                runAudioSource.Stop();
            }
        }

        // 6️⃣ Attack动画 + 音频B 同时开始
        if (objectAnimator != null)
        {
            objectAnimator.Play(attackAnimation);
        }

        if (audioSourceB != null)
        {
            audioSourceB.Play();
        }

        // 等待攻击真正命中时间
        yield return new WaitForSeconds(
            attackHitTime
        );

        // 7️⃣ 音频C + 玩家击飞同步
        if (audioSourceC != null)
        {
            audioSourceC.Play();
        }

        yield return StartCoroutine(
            KnockbackPlayer()
        );

        // 8️⃣ 黑屏渐变
        yield return StartCoroutine(
            FadeToBlack()
        );

        // 9️⃣ 切换场景
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(
                nextSceneName
            );
        }

        Debug.Log("剧情结束");
    }

    IEnumerator KnockbackPlayer()
    {
        Vector3 startPos = player.position;

        // 从Boss指向玩家
        Vector3 knockDirection =
            (
                player.position -
                objectAnimator.transform.position
            ).normalized;

        // 去除上下方向
        knockDirection.y = 0f;

        knockDirection.Normalize();

        Vector3 targetPos =
            startPos +
            knockDirection *
            knockbackDistance;

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                elapsed / knockbackDuration;

            // 水平位移
            Vector3 horizontalPos =
                Vector3.Lerp(
                    startPos,
                    targetPos,
                    t
                );

            // 抛物线高度
            float parabola =
                4f *
                knockbackHeight *
                t *
                (1f - t);

            player.position =
                horizontalPos +
                Vector3.up * parabola;

            yield return null;
        }

        player.position = targetPos;
    }

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            Debug.LogError(
                "Fade Image没有指定"
            );

            yield break;
        }

        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    elapsed / fadeDuration
                );

            fadeImage.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    alpha
                );

            yield return null;
        }

        fadeImage.color =
            new Color(
                0f,
                0f,
                0f,
                1f
            );
    }
}