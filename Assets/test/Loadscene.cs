using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loadscene : MonoBehaviour
{
    [Header("监听目标画布")]
    public Canvas targetCanvas;       // 绑定你那个进入区域后出现的 canvas_Target
    [Header("跳转设置")]
    public int targetSceneIndex = 1;  // 场景序号
    public float waitSecond = 35f;    // 画布出现后等待35秒跳转
    public float fadeBlackTime = 2f;  // 渐黑过渡2秒

    private bool isStartCountDown = false;
    private Image fadeImage;

    void Start()
    {
        // 创建全屏黑屏遮罩
        CreateFadeBlackPanel();
    }

    void Update()
    {
        // 画布激活 且 还没开始倒计时
        if (targetCanvas != null && targetCanvas.gameObject.activeSelf && !isStartCountDown)
        {
            isStartCountDown = true;
            StartCoroutine(DelayLoadScene());
        }
    }

    IEnumerator DelayLoadScene()
    {
        // 先等35秒
        yield return new WaitForSeconds(waitSecond);

        // 再2秒渐黑
        float t = 0;
        while (t < fadeBlackTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, t / fadeBlackTime);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 变黑后跳转场景
        SceneManager.LoadScene(targetSceneIndex);
    }

    // 可选：离开区域重置倒计时
    public void ResetCountDown()
    {
        isStartCountDown = false;
        // 重置黑屏透明度
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);
    }

    // 创建全屏黑色遮罩UI
    void CreateFadeBlackPanel()
    {
        GameObject fadeObj = new GameObject("FadeBlackPanel");
        fadeObj.transform.SetParent(transform);

        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
}