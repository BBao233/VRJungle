using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 对话管理器
/// 管理对话的显示、播放和队列
/// 
/// 使用方式：
/// 1. 创建Canvas，添加对话UI（Text + 背景Image）
/// 2. 挂载此脚本到Canvas或空物体
/// 3. 拖拽UI引用
/// 4. 通过代码调用 ShowDialogue() 播放对话
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("=== UI引用 ===")]
    [Tooltip("对话面板（包含说话者名称和对话内容的父物体）")]
    public GameObject dialoguePanel;

    [Tooltip("说话者名称Text")]
    public Text speakerNameText;

    [Tooltip("对话内容Text")]
    public Text dialogueContentText;

    [Header("=== 设置 ===")]
    [Tooltip("每个字符的显示时间（秒），用于打字机效果")]
    public float typewriterSpeed = 0.05f;

    [Tooltip("是否启用打字机效果")]
    public bool useTypewriterEffect = true;

    [Tooltip("最小显示时间（秒），即使文字很短也至少显示这么久")]
    public float minDisplayTime = 2f;

    [Tooltip("每字符额外显示时间（秒），用于自动计算对话时长")]
    public float timePerCharacter = 0.1f;

    [Header("=== 事件 ===")]
    [Tooltip("对话开始时触发")]
    public UnityEngine.Events.UnityEvent onDialogueStart;

    [Tooltip("对话结束时触发")]
    public UnityEngine.Events.UnityEvent onDialogueEnd;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private bool _isPlaying = false;
    private Coroutine _dialogueCoroutine;

    /// <summary>
    /// 是否正在播放对话
    /// </summary>
    public bool IsPlaying => _isPlaying;

    void Start()
    {
        // 初始隐藏对话面板
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// 显示对话
    /// </summary>
    /// <param name="speaker">说话者名称</param>
    /// <param name="text">对话内容</param>
    /// <param name="duration">显示时间（秒），0=自动计算</param>
    public void ShowDialogue(string speaker, string text, float duration = 0f)
    {
        if (_dialogueCoroutine != null)
            StopCoroutine(_dialogueCoroutine);

        _dialogueCoroutine = StartCoroutine(PlayDialogue(speaker, text, duration));
    }

    /// <summary>
    /// 立即结束当前对话
    /// </summary>
    public void SkipDialogue()
    {
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        HideDialogue();
    }

    private IEnumerator PlayDialogue(string speaker, string text, float duration)
    {
        _isPlaying = true;

        // 显示面板
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // 设置说话者名称
        if (speakerNameText != null)
            speakerNameText.text = speaker;

        // 设置对话内容
        if (dialogueContentText != null)
        {
            if (useTypewriterEffect)
            {
                yield return StartCoroutine(TypewriterEffect(dialogueContentText, text));
            }
            else
            {
                dialogueContentText.text = text;
            }
        }

        onDialogueStart?.Invoke();

        if (debugMode)
            Debug.Log($"[对话] 💬 {speaker}: {text}");

        // 计算显示时间
        if (duration <= 0f)
        {
            duration = Mathf.Max(minDisplayTime, text.Length * timePerCharacter);
        }

        // 等待显示时间
        yield return new WaitForSeconds(duration);

        HideDialogue();
    }

    private IEnumerator TypewriterEffect(Text textComponent, string fullText)
    {
        textComponent.text = "";
        int currentChar = 0;

        while (currentChar < fullText.Length)
        {
            currentChar++;
            textComponent.text = fullText.Substring(0, currentChar);
            yield return new WaitForSeconds(typewriterSpeed);
        }

        textComponent.text = fullText;
    }

    private void HideDialogue()
    {
        _isPlaying = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        onDialogueEnd?.Invoke();

        if (debugMode)
            Debug.Log("[对话] 对话结束");
    }
}
