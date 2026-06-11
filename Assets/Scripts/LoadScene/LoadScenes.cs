using System.Collections;
using UnityEngine;

public class LoadScenes : MonoBehaviour
{
    public GameObject player;
    public int index;

    // 新增：拖拽挂载了SimpleFadeBlack脚本的黑屏Image
    public SimpleFadeBlack fadeBlack;

    private bool isTriggered = false;

    void Update()
    {
        // 防止重复触发
        if (isTriggered) return;

        if (player.transform.position.x < this.transform.position.x)
        {
            isTriggered = true;
            // 先播放渐变黑屏
            fadeBlack.StartFade();
            // 延迟和黑屏时长一致，再切换场景
            Invoke(nameof(LoadScene), fadeBlack.fadeTime);
        }
    }

    // 加载场景
    void LoadScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(index);
    }
}