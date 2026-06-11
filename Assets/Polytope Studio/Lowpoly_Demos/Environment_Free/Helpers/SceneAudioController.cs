using UnityEngine;

/// <summary>
/// 场景音频控制器
/// 管理场景中不同阶段的音频播放和切换
/// 
/// 使用方式：
/// 1. 在场景中创建空物体，挂载此脚本
/// 2. 在Inspector中拖入音频片段（AudioClip）
/// 3. 通过代码调用 PlayAudio() / StopAudio() 控制播放
/// </summary>
public class SceneAudioController : MonoBehaviour
{
    [Header("=== 音频源设置 ===")]
    [Tooltip("音频播放器（自动创建，也可手动指定）")]
    public AudioSource audioSource;

    [Header("=== 音频片段 ===")]
    [Tooltip("音频1：角色走路时播放")]
    public AudioClip walkAudio;

    [Tooltip("音频2：角色停下说话时播放")]
    public AudioClip talkAudio;

    [Tooltip("音频3：角色B说话时播放（可选）")]
    public AudioClip characterBTalkAudio;

    [Tooltip("音频4：睡觉/渐黑时播放（可选）")]
    public AudioClip sleepAudio;

    [Tooltip("音频5：天亮/渐亮时播放（可选）")]
    public AudioClip wakeUpAudio;

    [Header("=== 播放设置 ===")]
    [Tooltip("是否循环播放")]
    public bool loop = false;

    [Tooltip("音量（0-1）")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("淡入时间（秒），0=立即播放")]
    public float fadeInDuration = 0.5f;

    [Tooltip("淡出时间（秒），0=立即停止")]
    public float fadeOutDuration = 0.5f;

    [Header("=== 事件 ===")]
    [Tooltip("音频播放完成时触发")]
    public UnityEngine.Events.UnityEvent onAudioFinished;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private bool _isFading = false;
    private Coroutine _fadeCoroutine;
    private Coroutine _playCoroutine;

    /// <summary>
    /// 当前是否正在播放
    /// </summary>
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;

    void Awake()
    {
        // 自动创建AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = loop;
        }
    }

    /// <summary>
    /// 播放走路音频（音频1）
    /// </summary>
    public void PlayWalkAudio()
    {
        PlayAudio(walkAudio);
    }

    /// <summary>
    /// 播放说话音频（音频2）
    /// </summary>
    public void PlayTalkAudio()
    {
        PlayAudio(talkAudio);
    }

    /// <summary>
    /// 播放角色B说话音频（音频3）
    /// </summary>
    public void PlayCharacterBTalkAudio()
    {
        PlayAudio(characterBTalkAudio);
    }

    /// <summary>
    /// 播放睡觉音频（音频4）
    /// </summary>
    public void PlaySleepAudio()
    {
        PlayAudio(sleepAudio);
    }

    /// <summary>
    /// 播放天亮音频（音频5）
    /// </summary>
    public void PlayWakeUpAudio()
    {
        PlayAudio(wakeUpAudio);
    }

    /// <summary>
    /// 播放指定音频片段
    /// </summary>
    public void PlayAudio(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[场景音频] ⚠️ 音频片段为空，无法播放！");
            return;
        }

        // 停止当前播放
        StopAudioImmediate();

        audioSource.clip = clip;
        audioSource.volume = 0f; // 从0开始淡入
        audioSource.loop = loop;
        audioSource.Play();

        if (debugMode)
            Debug.Log($"[场景音频] ▶ 播放音频: \"{clip.name}\"（时长: {clip.length:F1}秒）");

        // 淡入
        if (fadeInDuration > 0f)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeIn(fadeInDuration));
        }
        else
        {
            audioSource.volume = volume;
        }

        // 如果不循环，等待播放完成
        if (!loop)
        {
            if (_playCoroutine != null)
                StopCoroutine(_playCoroutine);
            _playCoroutine = StartCoroutine(WaitForAudioFinish(clip.length));
        }
    }

    /// <summary>
    /// 淡出停止当前音频
    /// </summary>
    public void StopAudio()
    {
        if (audioSource == null || !audioSource.isPlaying) return;

        if (debugMode)
            Debug.Log("[场景音频] ⏹ 淡出停止音频");

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        if (fadeOutDuration > 0f)
        {
            _fadeCoroutine = StartCoroutine(FadeOut(fadeOutDuration));
        }
        else
        {
            StopAudioImmediate();
        }
    }

    /// <summary>
    /// 立即停止（不淡出）
    /// </summary>
    public void StopAudioImmediate()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
            _playCoroutine = null;
        }
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        if (audioSource != null && !_isFading)
        {
            audioSource.volume = volume;
        }
    }

    /// <summary>
    /// 淡入
    /// </summary>
    private System.Collections.IEnumerator FadeIn(float duration)
    {
        _isFading = true;
        audioSource.volume = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = volume;
        _isFading = false;
    }

    /// <summary>
    /// 淡出
    /// </summary>
    private System.Collections.IEnumerator FadeOut(float duration)
    {
        _isFading = true;
        float startVolume = audioSource.volume;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        _isFading = false;
    }

    /// <summary>
    /// 等待音频播放完成
    /// </summary>
    private System.Collections.IEnumerator WaitForAudioFinish(float clipLength)
    {
        // 等待音频播放完（加上淡入时间）
        yield return new WaitForSeconds(clipLength + fadeInDuration);

        if (audioSource != null && !audioSource.isPlaying)
        {
            if (debugMode)
                Debug.Log("[场景音频] ✅ 音频播放完成");

            onAudioFinished?.Invoke();
        }
    }

    /// <summary>
    /// 交叉淡入淡出：停止当前音频，播放新音频
    /// </summary>
    public void CrossFade(AudioClip newClip, float crossFadeDuration = 1f)
    {
        StartCoroutine(CrossFadeCoroutine(newClip, crossFadeDuration));
    }

    private System.Collections.IEnumerator CrossFadeCoroutine(AudioClip newClip, float duration)
    {
        if (newClip == null) yield break;

        float elapsed = 0f;

        // 淡出当前音频
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            audioSource.Stop();
        }

        // 播放新音频并淡入
        audioSource.clip = newClip;
        audioSource.volume = 0f;
        audioSource.loop = loop;
        audioSource.Play();

        if (debugMode)
            Debug.Log($"[场景音频] 🔄 交叉淡入: \"{newClip.name}\"");

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, volume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = volume;
    }
}
