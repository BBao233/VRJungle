using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeCounter : MonoBehaviour
{
    public GameObject player;
    public TextMeshProUGUI time_count;
    private static Death deathJudge;
    public int index;
    private static float totalTime = 0f;
    private static bool isTiming = false;
    private static bool isPaused = false;
    private static bool isDead = false;
    private static bool hasStarted = false;
    private static bool hasTimeOut = false;
    private const float TimeOutLimit = 150f;

    // 👇 所有原有函数100%保留，完全兼容其他脚本调用
    public static void PauseTiming()
    {
        if (!isDead && isTiming)
        {
            isPaused = true;
            isTiming = false;
        }
    }

    public static void ResumeTiming()
    {
        if (!isDead && !isTiming && isPaused)
        {
            isPaused = false;
            isTiming = true;
        }
    }

    public static void ResetTiming()
    {
        totalTime = 0f;
        isTiming = true;
        isPaused = false;
        isDead = false;
        hasStarted = true;
        hasTimeOut = false;
    }

    public static void StopTimingOnDeath()
    {
        if (!isDead)
        {
            isDead = true;
            isTiming = false;
            isPaused = false;
        }
    }

    private void HandleTimeOut()
    {
        hasTimeOut = true;
        isTiming = false;
        Debug.Log("超时！自动跳转到下一个场景");
        SceneManager.LoadScene(index);
    }

    private void Awake()
    {
        if (player != null && deathJudge == null)
            deathJudge = player.GetComponent<Death>();
    }

    // 🔥 核心修改：第一次加载场景自动启动计时，后续重载场景不重置
    private void Start()
    {
        // 全局仅第一次启动时初始化计时，场景重载不再执行
        if (!hasStarted)
        {
            ResetTiming();
        }
        UpdateDisplay();
    }

    // 🔥 核心修改：永久计时，不受死亡/暂停/时停影响
    private void Update()
    {
        // 无视所有状态，永久累加时间（满足你的核心需求）
        if (!hasTimeOut)
        {
            totalTime += Time.unscaledDeltaTime;
            UpdateDisplay();

            if (totalTime >= TimeOutLimit && !hasTimeOut)
            {
                HandleTimeOut();
            }
        }

        // 👇 原有状态判断保留，不影响计时，仅兼容旧逻辑
        if (isTiming && !isPaused && !isDead && !hasTimeOut)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (time_count == null) return;

        int minutes = Mathf.FloorToInt(totalTime / 60f);
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        time_count.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}