using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 踢球游戏管理器 - 挂在踢球场景里
/// </summary>
public class KickBallGameManager : MonoBehaviour
{
    [Header("游戏设置")]
    public int targetScore = 3;
    public float gameTime = 60f;

    [Header("超时设置")]
    public float timeout = 300f;
    public bool enableTimeout = true;

    [Header("场景设置")]
    public string returnSceneName = "mayan";  // 返回的剧情场景名
    public float fadeDuration = 0.5f;         // 淡入淡出时间

    [Header("提示设置")]
    [Tooltip("进入游戏时屏幕中央显示的提示文字")]
    public string hintText = "用手将球击入框中";
    [Tooltip("提示文字显示时长（秒），最后1秒淡出")]
    public float hintDuration = 3f;

    [Header("音效设置")]
    public AudioClip scoreSound;       // 进球音效
    public AudioClip gameStartSound;   // 游戏开始音效
    public AudioClip gameEndSound;     // 游戏结束音效
    public float volume = 1f;

    private int currentScore = 0;
    private float timer;
    private float timeoutTimer;
    private bool isGameActive = false;

    // 提示文字
    private float hintTimer = 0f;
    private bool showHint = false;

    // 淡入淡出
    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;

    // 音频
    private AudioSource audioSource;

    // 单例引用，方便其他脚本调用
    public static KickBallGameManager Instance { get; private set; }

    void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, Color.black);
        fadeTexture.Apply();

        // 创建音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D 音效
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        // 提示文字计时
        if (showHint)
        {
            hintTimer += Time.deltaTime;
            if (hintTimer >= hintDuration)
                showHint = false;
        }

        if (!isGameActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            EndGame();
            return;
        }

        if (currentScore >= targetScore)
        {
            EndGame();
            return;
        }

        if (enableTimeout)
        {
            timeoutTimer += Time.deltaTime;
            if (timeoutTimer >= timeout)
            {
                Debug.Log("【踢球游戏】超时，自动退出");
                AutoExit();
            }
        }
    }

    public void StartGame()
    {
        isGameActive = true;
        timer = gameTime;
        currentScore = 0;
        timeoutTimer = 0f;

        // 显示提示文字
        showHint = true;
        hintTimer = 0f;

        Debug.Log("【踢球游戏】开始！");

        if (gameStartSound != null)
        {
            audioSource.PlayOneShot(gameStartSound, volume);
        }
    }

    /// <summary>
    /// 增加分数 - 由 PermanentFloorFrame 进球时调用
    /// </summary>
    public void OnBallScored()
    {
        if (!isGameActive) return;
        currentScore++;
        Debug.Log($"【踢球游戏】进球！当前分数: {currentScore}/{targetScore}");

        if (scoreSound != null)
        {
            audioSource.PlayOneShot(scoreSound, volume);
        }
    }

    /// <summary>
    /// 旧接口，保留兼容
    /// </summary>
    public void AddScore()
    {
        OnBallScored();
    }

    /// <summary>
    /// 结束游戏 - 淡出 → 加载回原场景
    /// </summary>
    public void EndGame()
    {
        if (!isGameActive) return;
        isGameActive = false;

        Debug.Log($"【踢球游戏】结束！最终分数: {currentScore}");

        if (gameEndSound != null)
        {
            audioSource.PlayOneShot(gameEndSound, volume);
        }

        // 保存标记，让原场景知道是从踢球回来的
        PlayerPrefs.SetInt("ReturnFromKickBall", 1);
        PlayerPrefs.Save();

        StartCoroutine(FadeAndReturn());
    }

    System.Collections.IEnumerator FadeAndReturn()
    {
        if (isFading) yield break;
        isFading = true;

        // 淡出
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 加载回原场景
        SceneManager.LoadScene(returnSceneName);
        yield return null;

        // 淡入
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isFading = false;
    }

    System.Collections.IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            fadeAlpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fadeAlpha = to;
    }

    void OnGUI()
    {
        // 淡入淡出
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(1f, 1f, 1f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
            GUI.color = Color.white;
        }

        if (!isGameActive) return;

        DrawSciFiUI();
        DrawHintText();
    }

    /// <summary>
    /// 绘制科幻风格 UI
    /// </summary>
    void DrawSciFiUI()
    {
        float boxWidth = 220;
        float boxHeight = 100;
        float margin = 20;

        // 半透明背景框
        GUI.color = new Color(0, 0.8f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(margin, margin, boxWidth, boxHeight), Texture2D.whiteTexture);

        // 边框发光效果
        GUI.color = new Color(0, 0.8f, 1f, 0.6f);
        DrawBorder(new Rect(margin, margin, boxWidth, boxHeight), 2);

        GUI.color = Color.white;

        // 时间显示
        GUIStyle timeStyle = new GUIStyle();
        timeStyle.fontSize = 16;
        timeStyle.fontStyle = FontStyle.Bold;
        timeStyle.normal.textColor = new Color(0.5f, 0.8f, 1f, 0.8f);

        GUI.Label(new Rect(margin + 15, margin + 10, 100, 25), "TIME", timeStyle);

        GUIStyle timeValueStyle = new GUIStyle();
        timeValueStyle.fontSize = 36;
        timeValueStyle.fontStyle = FontStyle.Bold;
        timeValueStyle.normal.textColor = Color.cyan;

        string timeText = $"{Mathf.Max(0, timer):F1}";
        GUI.Label(new Rect(margin + 15, margin + 30, 150, 45), timeText, timeValueStyle);

        // 分数显示
        GUIStyle scoreStyle = new GUIStyle();
        scoreStyle.fontSize = 14;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = new Color(0.5f, 0.8f, 1f, 0.8f);

        GUI.Label(new Rect(margin + 15, margin + 70, 100, 20), "SCORE", scoreStyle);

        GUIStyle scoreValueStyle = new GUIStyle();
        scoreValueStyle.fontSize = 20;
        scoreValueStyle.fontStyle = FontStyle.Bold;
        scoreValueStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);

        string scoreText = $"{currentScore} / {targetScore}";
        GUI.Label(new Rect(margin + 70, margin + 68, 100, 25), scoreText, scoreValueStyle);

        // 进度条背景
        float barWidth = 180;
        float barHeight = 4;
        float barX = margin + 15;
        float barY = margin + 92;

        GUI.color = new Color(0.2f, 0.3f, 0.4f, 0.5f);
        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);

        // 进度条填充
        float progress = targetScore > 0 ? (float)currentScore / targetScore : 0;
        progress = Mathf.Clamp01(progress);

        GUI.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(barX, barY, barWidth * progress, barHeight), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制屏幕中央提示文字（带淡出效果）
    /// </summary>
    void DrawHintText()
    {
        if (!showHint) return;

        // 最后1秒淡出
        float alpha = 1f;
        float fadeOutTime = 1f;
        if (hintTimer > hintDuration - fadeOutTime)
        {
            alpha = Mathf.Clamp01((hintDuration - hintTimer) / fadeOutTime);
        }

        // 半透明背景条
        float barHeight = 80f;
        float barY = (Screen.height - barHeight) / 2f;
        GUI.color = new Color(0, 0, 0, 0.6f * alpha);
        GUI.DrawTexture(new Rect(0, barY, Screen.width, barHeight), Texture2D.whiteTexture);

        // 提示文字
        GUIStyle hintStyle = new GUIStyle();
        hintStyle.fontSize = 36;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.normal.textColor = new Color(1f, 0.9f, 0.3f, alpha);

        GUI.Label(new Rect(0, barY, Screen.width, barHeight), hintText, hintStyle);

        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制边框
    /// </summary>
    void DrawBorder(Rect rect, int thickness)
    {
        // 上边框
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        // 下边框
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        // 左边框
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        // 右边框
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }

    private void AutoExit()
    {
        isGameActive = false;
        Debug.Log("【踢球游戏】超时退出应用");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
