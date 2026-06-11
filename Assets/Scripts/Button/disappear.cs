using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 脚本名必须和文件名一致（disappear）
public class disappear : MonoBehaviour
{
    // 游戏启动时执行一次
    void Start()
    {
        // 启动延迟消失的协程
        StartCoroutine(DisappearAfterDelay());
    }

    // Update 无需使用，留空即可
    void Update()
    {

    }

    /// <summary>
    /// 协程：延迟3.5秒后隐藏画布
    /// </summary>
    IEnumerator DisappearAfterDelay()
    {
        // 等待 3.5 秒
        yield return new WaitForSeconds(3.5f);

        // 核心：禁用当前画布对象（完全消失）
        gameObject.SetActive(false);
    }
}