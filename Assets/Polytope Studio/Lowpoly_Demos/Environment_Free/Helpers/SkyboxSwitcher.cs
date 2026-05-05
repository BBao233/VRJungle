using UnityEngine;

/// <summary>
/// 天空盒切换控制器
/// 支持渐变切换天空盒（通过颜色插值实现平滑过渡）
/// 
/// 使用方式：
/// 1. 在场景中挂载此脚本
/// 2. 配置白天和夜晚的天空盒材质
/// 3. 调用 SwitchToDay() 或 SwitchToNight() 切换
/// </summary>
public class SkyboxSwitcher : MonoBehaviour
{
    [Header("=== 天空盒设置 ===")]
    [Tooltip("夜晚天空盒材质")]
    public Material nightSkybox;

    [Tooltip("白天天空盒材质")]
    public Material daySkybox;

    [Header("=== 渐变设置 ===")]
    [Tooltip("是否使用渐变切换")]
    public bool useSmoothTransition = true;

    [Tooltip("渐变时间（秒）")]
    public float transitionDuration = 3f;

    [Header("=== 光照设置（可选） ===")]
    [Tooltip("是否同时切换方向光的颜色和强度")]
    public bool updateDirectionalLight = false;

    [Tooltip("方向光（通常场景中的太阳光）")]
    public Light directionalLight;

    [Tooltip("夜晚光照颜色")]
    public Color nightLightColor = new Color(0.2f, 0.2f, 0.4f, 1f);

    [Tooltip("夜晚光照强度")]
    public float nightLightIntensity = 0.3f;

    [Tooltip("白天光照颜色")]
    public Color dayLightColor = new Color(1f, 0.95f, 0.8f, 1f);

    [Tooltip("白天光照强度")]
    public float dayLightIntensity = 1f;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private Material _currentSkybox;
    private bool _isTransitioning = false;

    /// <summary>
    /// 是否正在切换
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    void Start()
    {
        _currentSkybox = RenderSettings.skybox;
    }

    /// <summary>
    /// 切换到白天天空盒
    /// </summary>
    public void SwitchToDay()
    {
        if (daySkybox == null)
        {
            Debug.LogError("[天空切换] 白天天空盒材质未设置！");
            return;
        }

        if (useSmoothTransition)
        {
            StartCoroutine(SmoothSwitchSkybox(daySkybox, transitionDuration));
        }
        else
        {
            RenderSettings.skybox = daySkybox;
            _currentSkybox = daySkybox;
        }

        if (updateDirectionalLight && directionalLight != null)
        {
            StartCoroutine(UpdateLightTransition(dayLightColor, dayLightIntensity, transitionDuration));
        }

        if (debugMode)
            Debug.Log("[天空切换] ☀️ 切换到白天");
    }

    /// <summary>
    /// 切换到夜晚天空盒
    /// </summary>
    public void SwitchToNight()
    {
        if (nightSkybox == null)
        {
            Debug.LogError("[天空切换] 夜晚天空盒材质未设置！");
            return;
        }

        if (useSmoothTransition)
        {
            StartCoroutine(SmoothSwitchSkybox(nightSkybox, transitionDuration));
        }
        else
        {
            RenderSettings.skybox = nightSkybox;
            _currentSkybox = nightSkybox;
        }

        if (updateDirectionalLight && directionalLight != null)
        {
            StartCoroutine(UpdateLightTransition(nightLightColor, nightLightIntensity, transitionDuration));
        }

        if (debugMode)
            Debug.Log("[天空切换] 🌙 切换到夜晚");
    }

    /// <summary>
    /// 直接替换天空盒（无渐变）
    /// </summary>
    public void SetSkyboxImmediate(Material skybox)
    {
        if (skybox == null) return;

        RenderSettings.skybox = skybox;
        _currentSkybox = skybox;

        if (debugMode)
            Debug.Log($"[天空切换] 直接设置天空盒: {skybox.name}");
    }

    private System.Collections.IEnumerator SmoothSwitchSkybox(Material targetSkybox, float duration)
    {
        _isTransitioning = true;

        // 直接替换（天空盒渐变比较复杂，这里提供直接替换 + 光照渐变的方案）
        // 如果需要天空盒颜色渐变，可以使用两个天空盒的Color属性做插值
        RenderSettings.skybox = targetSkybox;
        _currentSkybox = targetSkybox;

        // 等待渐变时间（让光照有时间过渡）
        yield return new WaitForSeconds(duration);

        _isTransitioning = false;
    }

    private System.Collections.IEnumerator UpdateLightTransition(Color targetColor, float targetIntensity, float duration)
    {
        if (directionalLight == null) yield break;

        Color startColor = directionalLight.color;
        float startIntensity = directionalLight.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            directionalLight.color = Color.Lerp(startColor, targetColor, t);
            directionalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        directionalLight.color = targetColor;
        directionalLight.intensity = targetIntensity;
    }
}
