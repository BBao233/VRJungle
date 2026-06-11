using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 角色对话组件
/// 挂载到每个角色身上，自己管理自己的音频、字幕和说话动画
/// 
/// 使用方式：
/// 1. 挂载到角色根物体上
/// 2. 在Inspector中配置对话列表（音频+字幕配对）
/// 3. 通过代码调用 PlayDialogue(index) 播放指定对话
/// </summary>
public class CharacterDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [Tooltip("对话字幕内容")]
        [TextArea] public string subtitleText = "";

        [Tooltip("对应的音频片段（可选，没有则只显示字幕）")]
        public AudioClip audioClip;

        [Tooltip("音频音量")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("音频淡入时间")]
        public float fadeIn = 0.3f;

        [Tooltip("音频淡出时间")]
        public float fadeOut = 0.3f;

        [Tooltip("字幕显示时间（秒），0=根据音频长度自动计算，音频为空时默认3秒")]
        public float displayDuration = 0f;

        [Tooltip("说话动画是否循环（短音频设为false，长音频设为true）")]
        public bool loopTalkAnim = true;

        [Tooltip("是否触发说话动画（走路时播放音频但不需要说话动画，设为false）")]
        public bool triggerTalkAnimation = true;
    }

    [Header("=== 对话列表 ===")]
    [Tooltip("该角色的所有对话（按顺序添加）")]
    public DialogueLine[] dialogueLines;

    [Header("=== 字幕UI ===")]
    [Tooltip("字幕Text组件（场景中的UI元素）")]
    public Text subtitleText;

    [Tooltip("说话者名称")]
    public string speakerName = "";

    [Header("=== 动画控制 ===")]
    [Tooltip("关联的CharacterAnimatorController（不填则自动查找）")]
    public CharacterAnimatorController animatorController;

    [Header("=== 事件 ===")]
    [Tooltip("对话开始时触发")]
    public UnityEngine.Events.UnityEvent onDialogueStart;

    [Tooltip("对话结束时触发")]
    public UnityEngine.Events.UnityEvent onDialogueEnd;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private AudioSource _audioSource;
    private bool _isPlaying = false;
    private Coroutine _dialogueCoroutine;
    private int _lastPlayedIndex = -1;

    /// <summary>
    /// 是否正在播放对话
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// 最后播放的对话索引
    /// </summary>
    public int LastPlayedIndex => _lastPlayedIndex;

    void Awake()
    {
        // 自动查找AnimatorController
        if (animatorController == null)
        {
            animatorController = GetComponent<CharacterAnimatorController>();
        }

        // 创建AudioSource
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    /// <summary>
    /// 播放指定索引的对话
    /// </summary>
    /// <param name="index">对话索引（从0开始）</param>
    public void PlayDialogue(int index)
    {
        if (index < 0 || index >= dialogueLines.Length)
        {
            Debug.LogWarning($"[角色对话] {speakerName} 对话索引 {index} 超出范围！");
            return;
        }

        if (_dialogueCoroutine != null)
            StopCoroutine(_dialogueCoroutine);

        _lastPlayedIndex = index;
        _dialogueCoroutine = StartCoroutine(PlayDialogueCoroutine(dialogueLines[index]));
    }

    /// <summary>
    /// 播放下一条对话（按索引递增）
    /// </summary>
    public void PlayNextDialogue()
    {
        int nextIndex = _lastPlayedIndex + 1;
        if (nextIndex < dialogueLines.Length)
        {
            PlayDialogue(nextIndex);
        }
        else
        {
            Debug.LogWarning($"[角色对话] {speakerName} 没有更多对话了（当前: {_lastPlayedIndex}，总数: {dialogueLines.Length}）");
        }
    }

    /// <summary>
    /// 播放第一条对话
    /// </summary>
    public void PlayFirstDialogue()
    {
        PlayDialogue(0);
    }

    /// <summary>
    /// 停止当前对话
    /// </summary>
    public void StopDialogue()
    {
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        StopTalking();
        HideSubtitle();
    }

    /// <summary>
    /// 获取指定对话的时长（用于外部等待）
    /// </summary>
    public float GetDialogueDuration(int index)
    {
        if (index < 0 || index >= dialogueLines.Length) return 0f;

        DialogueLine line = dialogueLines[index];
        if (line.displayDuration > 0f) return line.displayDuration;
        if (line.audioClip != null) return line.audioClip.length + line.fadeIn + line.fadeOut;
        return 3f;
    }

    private IEnumerator PlayDialogueCoroutine(DialogueLine line)
    {
        _isPlaying = true;

        // 只在需要时触发说话动画
        if (line.triggerTalkAnimation && animatorController != null)
        {
            animatorController.SetTalking(true);
        }

        // 显示字幕
        ShowSubtitle(line.subtitleText);

        // 播放音频
        if (line.audioClip != null)
        {
            _audioSource.clip = line.audioClip;
            _audioSource.volume = 0f;
            _audioSource.Play();

            // 淡入
            if (line.fadeIn > 0f)
            {
                float t = 0f;
                while (t < line.fadeIn)
                {
                    t += Time.deltaTime;
                    _audioSource.volume = Mathf.Lerp(0f, line.volume, t / line.fadeIn);
                    yield return null;
                }
            }
            _audioSource.volume = line.volume;

            if (debugMode)
                Debug.Log($"[角色对话] 💬 {speakerName}: {line.subtitleText}（音频: {line.audioClip.name}）");
        }
        else
        {
            if (debugMode)
                Debug.Log($"[角色对话] 💬 {speakerName}: {line.subtitleText}（无音频）");
        }

        onDialogueStart?.Invoke();

        // 计算等待时间
        float waitTime;
        if (line.displayDuration > 0f)
        {
            waitTime = line.displayDuration;
        }
        else if (line.audioClip != null)
        {
            waitTime = line.audioClip.length;
        }
        else
        {
            waitTime = 3f;
        }

        // 等待对话播放
        yield return new WaitForSeconds(waitTime);

        // 淡出音频
        if (line.audioClip != null && _audioSource.isPlaying)
        {
            if (line.fadeOut > 0f)
            {
                float startVol = _audioSource.volume;
                float t = 0f;
                while (t < line.fadeOut)
                {
                    t += Time.deltaTime;
                    _audioSource.volume = Mathf.Lerp(startVol, 0f, t / line.fadeOut);
                    yield return null;
                }
            }
            _audioSource.Stop();
            _audioSource.volume = 0f;
        }

        // 停止说话动画（只在触发了说话动画时才停止）
        if (line.triggerTalkAnimation)
        {
            StopTalking();
        }

        // 隐藏字幕
        HideSubtitle();

        _isPlaying = false;

        onDialogueEnd?.Invoke();

        if (debugMode)
            Debug.Log($"[角色对话] ✅ {speakerName} 对话结束");
    }

    private void ShowSubtitle(string text)
    {
        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(speakerName))
            {
                subtitleText.text = $"<b>{speakerName}：</b>{text}";
            }
            else
            {
                subtitleText.text = text;
            }
        }
    }

    private void HideSubtitle()
    {
        if (subtitleText != null)
        {
            subtitleText.text = "";
            subtitleText.gameObject.SetActive(false);
        }
    }

    private void StopTalking()
    {
        if (animatorController != null)
        {
            animatorController.SetTalking(false);
        }
    }

    /// <summary>
    /// 只播放音频不显示字幕（用于背景音效）
    /// </summary>
    public void PlayAudioOnly(AudioClip clip, float vol = 1f, float fadeIn = 0.5f, float fadeOut = 0.5f)
    {
        if (clip == null) return;
        StartCoroutine(PlayAudioOnlyCoroutine(clip, vol, fadeIn, fadeOut));
    }

    private IEnumerator PlayAudioOnlyCoroutine(AudioClip clip, float vol, float fadeIn, float fadeOut)
    {
        _audioSource.clip = clip;
        _audioSource.volume = 0f;
        _audioSource.Play();

        if (fadeIn > 0f)
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(0f, vol, t / fadeIn);
                yield return null;
            }
        }
        _audioSource.volume = vol;

        yield return new WaitForSeconds(clip.length);

        if (fadeOut > 0f)
        {
            float startVol = _audioSource.volume;
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeOut);
                yield return null;
            }
        }
        _audioSource.Stop();
        _audioSource.volume = 0f;
    }

    /// <summary>
    /// 停止音频
    /// </summary>
    public void StopAudio()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
            _audioSource.volume = 0f;
        }
    }
}
