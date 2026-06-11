using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class loadscenebymusic : MonoBehaviour
{
    [Header("赋值：熊身上的AudioSource")]
    public AudioSource bearAudio;

    [Header("全屏黑色遮罩UI")]
    public Image blackFadePanel;

    [Header("渐显的TMP文字")]
    public TextMeshProUGUI tipTMP;

    private bool alreadyStartFlow = false;

    void Start()
    {
        // 初始化：面板透明、文字透明
        blackFadePanel.color = new Color(0, 0, 0, 0);
        tipTMP.alpha = 0;
    }

    void Update()
    {
        // 还没开始流程 并且 熊刚好开始播放声音了
        if (!alreadyStartFlow && bearAudio != null && bearAudio.isPlaying)
        {
            alreadyStartFlow = true;
            StartCoroutine(TransitionFlow());
        }
    }

    IEnumerator TransitionFlow()
    {
        // 1. 等待熊叫声完全播放完毕
        while (bearAudio.isPlaying)
        {
            yield return null;
        }

        // 2. 叫声结束 间隔3秒
        yield return new WaitForSeconds(3f);

        // 3. 屏幕渐渐变黑
        yield return FadePanelAlpha(0f, 1f, 1.5f);

        // 4. TMP文字渐显
        yield return FadeTMPAlpha(0f, 1f, 1.5f);

        // 5. 再等3秒
        yield return new WaitForSeconds(3f);

        // 6. 跳转到场景序号2
        SceneManager.LoadScene(2);
    }

    // 面板渐变透明度
    IEnumerator FadePanelAlpha(float from, float to, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, time / duration);
            blackFadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }

    // TMP文字渐变透明度
    IEnumerator FadeTMPAlpha(float from, float to, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, time / duration);
            tipTMP.alpha = alpha;
            yield return null;
        }
    }
}