using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 屏幕渐黑/渐亮控制器
/// 用于场景切换、睡觉等过渡效果
/// 
/// 使用方式：
/// 1. 创建全屏Canvas（Render Mode: Screen Space - Overlay）
/// 2. 添加一个Image组件，颜色设为黑色，覆盖全屏
/// 3. 挂载此脚本
/// </summary>
public class ScreenFadeController : MonoBehaviour
{
    [Header("=== UI引用 ===")]
    [Tooltip("用于渐变的Image（全屏黑色Image）")]
    public Image fadeImage;

    [Header("=== 设置 ===")]
    [Tooltip("默认渐变时间（秒）")]
    public float defaultFadeDuration = 1.5f;

    [Tooltip("渐变曲线（可自定义缓入缓出）")]
    public AnimationCurve fadeCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 1f)
    );

    [Header("=== 事件 ===")]
    [Tooltip("渐黑完成时触发")]
    public UnityEngine.Events.UnityEvent onFadeToBlackComplete;

    [Tooltip("渐亮完成时触发")]
    public UnityEngine.Events.UnityEvent onFadeFromBlackComplete;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private bool _isFading = false;
    private Coroutine _fadeCoroutine;

    /// <summary>
    /// 是否正在渐变
    /// </summary>
    public bool IsFading => _isFading;

    /// <summary>
    /// 当前是否为黑色（完全渐黑）
    /// </summary>
    public bool IsBlack { get; private set; } = false;

    void Start()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        if (fadeImage != null)
        {
            // 初始为全透明
            SetAlpha(0f);
        }
    }

    /// <summary>
    /// 渐黑（屏幕变黑）
    /// </summary>
    /// <param name="duration">渐变时间（秒），0=使用默认值</param>
    public void FadeToBlack(float duration = 0f)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        float dur = duration > 0f ? duration : defaultFadeDuration;
        _fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, dur, true));
    }

    /// <summary>
    /// 渐亮（屏幕变亮）
    /// </summary>
    /// <param name="duration">渐变时间（秒），0=使用默认值</param>
    public void FadeFromBlack(float duration = 0f)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        float dur = duration > 0f ? duration : defaultFadeDuration;
        _fadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, dur, false));
    }

    /// <summary>
    /// 渐黑 → 等待 → 渐亮（用于睡觉等场景）
    /// </summary>
    /// <param name="blackDuration">保持黑色的时间（秒）</param>
    /// <param name="fadeDuration">渐变时间（秒）</param>
    public void FadeToBlackAndBack(float blackDuration, float fadeDuration = 0f)
    {
        StartCoroutine(FadeToBlackAndBackCoroutine(blackDuration, fadeDuration));
    }

    private IEnumerator FadeToBlackAndBackCoroutine(float blackDuration, float fadeDuration)
    {
        float dur = fadeDuration > 0f ? fadeDuration : defaultFadeDuration;

        // 渐黑
        FadeToBlack(dur);
        while (_isFading) yield return null;

        // 保持黑色
        yield return new WaitForSeconds(blackDuration);

        // 渐亮
        FadeFromBlack(dur);
        while (_isFading) yield return null;
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, bool toBlack)
    {
        _isFading = true;

        if (debugMode)
            Debug.Log($"[屏幕渐变] {(toBlack ? "渐黑" : "渐亮")} 开始，时长: {duration:F1}秒");

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = fadeCurve.Evaluate(t);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(endAlpha);
        _isFading = false;
        IsBlack = toBlack;

        if (debugMode)
            Debug.Log($"[屏幕渐变] {(toBlack ? "渐黑" : "渐亮")} 完成");

        if (toBlack)
            onFadeToBlackComplete?.Invoke();
        else
            onFadeFromBlackComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = Mathf.Clamp01(alpha);
            fadeImage.color = c;
        }
    }

    /// <summary>
    /// 立即设置为黑色
    /// </summary>
    public void SetBlackImmediate()
    {
        SetAlpha(1f);
        IsBlack = true;
    }

    /// <summary>
    /// 立即设置为透明
    /// </summary>
    public void SetClearImmediate()
    {
        SetAlpha(0f);
        IsBlack = false;
    }
}
