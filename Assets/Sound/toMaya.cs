using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class toMaya : MonoBehaviour
{
    [Header("淡入淡出时长")]
    public float fadeDuration = 2f;

    [Header("目标场景名")]
    public string nextSceneName;

    [Header("音频片段（必须拖拽）")]
    public AudioClip audioClip;

    [Header("手动拖入全屏黑Image")]
    public Image fadeImage;

    private AudioSource audioSource;

    void Start()
    {
        Debug.Log("📌 toMaya脚本Start()执行");

        // 修复射线阻挡问题
        if (fadeImage != null)
            fadeImage.raycastTarget = false;
        else
            Debug.LogError("❌ fadeImage未赋值！");

        // 修复音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.Log("📌 自动添加AudioSource组件");
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 强制2D音频（VR必开）
        audioSource.spatialBlend = 0;
        audioSource.volume = 1;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.mute = false;

        // 初始纯黑
        if (fadeImage != null)
            fadeImage.color = Color.black;

        // 等待一帧，解决VR加载延迟
        StartCoroutine(StartFlow());
    }

    IEnumerator StartFlow()
    {
        Debug.Log("📌 StartFlow协程启动");
        yield return null;

        // 1. 黑屏 → 完全透明
        if (fadeImage != null)
            yield return StartCoroutine(Fade(1, 0));
        else
            Debug.LogError("❌ 无法执行淡入：fadeImage为空");

        // 2. 播放音频
        if (audioClip != null && audioSource != null)
        {
            Debug.Log("✅ 开始播放音频");
            audioSource.clip = audioClip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
            Debug.Log("✅ 音频播放完成");
        }
        else
        {
            Debug.LogError("⚠️ 无法播放音频：audioClip或audioSource为空");
            // 即使无音频，也继续流程（可选：注释下面一行则无音频时停止）
            yield return new WaitForSeconds(2f); // 无音频时等待2秒再继续
        }

        // 3. 透明 → 黑屏
        if (fadeImage != null)
            yield return StartCoroutine(Fade(0, 1));
        else
            Debug.LogError("❌ 无法执行淡出：fadeImage为空");

        // 4. 切换场景
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"📌 开始加载场景：{nextSceneName}");
            StartCoroutine(LoadSceneAsync(nextSceneName));
        }
        else
        {
            Debug.LogError("❌ nextSceneName为空！未设置目标场景");
        }
    }

    // 平滑渐变（带日志调试）
    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeImage == null)
        {
            Debug.LogError("❌ Fade失败：fadeImage为空");
            yield break;
        }

        // 兜底：如果时长为0，强制设为2秒
        if (fadeDuration <= 0)
        {
            Debug.LogWarning("⚠️ fadeDuration为0，自动修正为2秒");
            fadeDuration = 2f;
        }

        Debug.Log($"📌 开始Fade：{startAlpha}→{endAlpha}，时长{fadeDuration}s");
        float time = 0;
        while (time < fadeDuration)
        {
            // 兜底：防止游戏暂停(Time.timeScale=0)导致渐变卡死
            time += Time.unscaledDeltaTime;
            float progress = time / fadeDuration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            fadeImage.color = new Color(0, 0, 0, alpha);

            // 打印关键数据（定位问题）
            Debug.Log($"🔍 time={time:F2}, 进度={progress:F2}, 透明度={alpha:F2}");
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, endAlpha);
        Debug.Log($"✅ Fade完成，最终透明度：{endAlpha}");
    }

    // 异步加载场景（避免卡顿）
    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            Debug.Log($"🔍 场景加载进度：{asyncLoad.progress * 100:F1}%");
            yield return null;
        }

        Debug.Log("✅ 场景加载完成，激活场景");
        asyncLoad.allowSceneActivation = true;
    }
}