using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 纯渐变黑屏脚本，无任何多余功能
public class SimpleFadeBlack : MonoBehaviour
{
    [Header("渐变时长（秒）")]
    public float fadeTime = 1.5f;

    private Image fadeImage;
    private bool isFading = false;

    void Awake()
    {
        // 获取自身的Image组件（直接挂在黑屏Image上）
        fadeImage = GetComponent<Image>();
        // 初始完全透明
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;
    }

    // 公开方法：外部调用开始渐变黑屏
    public void StartFade()
    {
        if (!isFading)
            StartCoroutine(FadeToBlack());
    }

    // 核心：纯渐变变黑
    IEnumerator FadeToBlack()
    {
        isFading = true;
        float timer = 0;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / fadeTime);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 最终锁定纯黑
        fadeImage.color = Color.black;
        isFading = false;
    }
}