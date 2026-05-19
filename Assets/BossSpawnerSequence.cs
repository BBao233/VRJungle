using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BossSpawnerSequence : MonoBehaviour
{
    [Header("Boss Settings")]
    public GameObject bossPrefab;

    // 玩家（建议XR Origin）
    public Transform player;

    [Header("Random Spawn Points")]
    public List<Transform> spawnPoints =
        new List<Transform>();

    [Header("Spawn Settings")]
    public int totalBossCount = 5;

    // 每只Boss生成间隔
    public float spawnInterval = 1f;

    [Header("Boss Move")]
    public float moveSpeed = 2f;

    // Boss移动多久后死亡
    public float moveDuration = 3f;

    [Header("Animation Names")]
    public string runAnimation = "RunForward";
    public string deathAnimation = "Death";

    [Header("Death Animation Duration")]
    public float deathDuration = 2f;

    [Header("Run Audio")]
    public AudioClip runClip;

    [Header("Death Audio")]
    public AudioClip deathClip;

    [Header("Boss Colors")]
    public List<Color> randomColors =
        new List<Color>()
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.magenta,
            Color.cyan
        };

    [Header("Final Voice")]
    public AudioSource finalVoiceAudio;

    [Header("Fade To Black")]
    public Image fadeImage;

    public float fadeDuration = 2f;

    [Header("Scene Load")]
    public string nextSceneName;

    private int finishedBossCount = 0;

    void Start()
    {
        StartCoroutine(SpawnSequence());
    }

    IEnumerator SpawnSequence()
    {
        // 生成Boss
        for (int i = 0; i < totalBossCount; i++)
        {
            SpawnBoss();

            yield return new WaitForSeconds(
                spawnInterval
            );
        }

        // 等待全部Boss结束
        while (finishedBossCount < totalBossCount)
        {
            yield return null;
        }

        // 播放最终语音
        if (finalVoiceAudio != null)
        {
            finalVoiceAudio.Play();

            if (finalVoiceAudio.clip != null)
            {
                yield return new WaitForSeconds(
                    finalVoiceAudio.clip.length
                );
            }
        }

        // 黑屏渐变
        yield return StartCoroutine(
            FadeToBlack()
        );

        // 切换场景
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(
                nextSceneName
            );
        }

        Debug.Log("流程结束");
    }

    void SpawnBoss()
    {
        if (bossPrefab == null ||
            player == null ||
            spawnPoints.Count == 0)
        {
            Debug.LogError(
                "Spawner缺少引用"
            );

            return;
        }

        // 随机出生点
        Transform randomSpawn =
            spawnPoints[
                Random.Range(
                    0,
                    spawnPoints.Count
                )
            ];

        GameObject boss =
            Instantiate(
                bossPrefab,
                randomSpawn.position,
                randomSpawn.rotation
            );

        // 随机颜色
        ApplyRandomColor(boss);

        // 启动Boss行为
        StartCoroutine(
            BossBehavior(boss)
        );
    }

    void ApplyRandomColor(GameObject boss)
    {
        Renderer[] renderers =
            boss.GetComponentsInChildren<Renderer>();

        Color randomColor =
            randomColors[
                Random.Range(
                    0,
                    randomColors.Count
                )
            ];

        foreach (Renderer renderer in renderers)
        {
            // 创建材质实例
            renderer.material =
                new Material(renderer.material);

            // 兼容URP/HDRP
            if (
                renderer.material.HasProperty(
                    "_BaseColor"
                )
            )
            {
                renderer.material.SetColor(
                    "_BaseColor",
                    randomColor
                );
            }
            else
            {
                renderer.material.color =
                    randomColor;
            }
        }
    }

    IEnumerator BossBehavior(GameObject boss)
    {
        Animator animator =
            boss.GetComponent<Animator>();

        AudioSource audioSource =
            boss.GetComponent<AudioSource>();

        // 朝向玩家
        Vector3 lookPos = player.position;

        lookPos.y = boss.transform.position.y;

        boss.transform.LookAt(lookPos);

        // 播放RunForward
        if (animator != null)
        {
            animator.Play(runAnimation);
        }

        // 播放跑步音效（循环）
        if (audioSource != null &&
            runClip != null)
        {
            audioSource.clip = runClip;

            audioSource.loop = true;

            audioSource.Play();
        }

        float elapsed = 0f;

        // 持续移动
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            boss.transform.position +=
                boss.transform.forward *
                moveSpeed *
                Time.deltaTime;

            yield return null;
        }

        // 停止跑步音效
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 播放死亡动画
        if (animator != null)
        {
            animator.Play(deathAnimation);
        }

        // 播放死亡音效
        if (audioSource != null &&
            deathClip != null)
        {
            audioSource.PlayOneShot(
                deathClip
            );
        }

        // 等待死亡动画
        yield return new WaitForSeconds(
            deathDuration
        );

        Destroy(boss);

        finishedBossCount++;

        Debug.Log(
            "Boss结束：" +
            finishedBossCount +
            "/" +
            totalBossCount
        );
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