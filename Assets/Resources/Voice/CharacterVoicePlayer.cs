using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 角色多音频播放器（优化版）
/// 功能：支持多音频独立延迟/速率/音量/循环，可同时播放，自带调试和容错
/// 使用：挂载到角色物体 → Inspector添加音频条目 → 拖入音频并设置参数 → 勾选PlayOnStart或调用PlayAll()
/// </summary>
[RequireComponent(typeof(AudioSource))] // 确保基础音频组件（仅占位，实际用独立Source）
public class CharacterMultiAudioPlayer : MonoBehaviour
{
    [System.Serializable]
    public class AudioItem
    {
        [Tooltip("音频文件（从Project窗口拖入）")]
        public AudioClip clip;

        [Tooltip("播放延迟（相对于PlayAll调用时刻，单位：秒）")]
        public float delay = 0f;

        [Tooltip("播放速率（0.5=慢放，1=正常，3=快放）")]
        [Range(0.1f, 3f)]
        public float pitch = 1f;

        [Tooltip("音量（0=静音，1=最大）")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("是否循环播放")]
        public bool loop = false;

        [Tooltip("是否启用3D音效（关闭则为2D音效，必能听到）")]
        public bool use3DSound = false;

        [HideInInspector] public AudioSource source; // 独立音频源
        [HideInInspector] public bool isPlaying;     // 播放中标记
        [HideInInspector] public bool isPending;     // 等待延迟标记
    }

    [Header("=== 音频列表（可添加多个） ===")]
    public List<AudioItem> audioItems = new List<AudioItem>();

    [Header("=== 全局设置 ===")]
    [Tooltip("启动时自动播放所有音频")]
    public bool playOnStart = false;

    [Tooltip("默认音量（全局缩放，不覆盖单个音频的音量）")]
    [Range(0f, 1f)]
    public float globalVolume = 1f;

    [Header("=== 调试模式（显示日志） ===")]
    public bool debugMode = true;

    private bool _isPlayingAll; // 是否正在播放全部音频

    void Awake()
    {
        // 检查场景是否有AudioListener
        if (!FindAnyObjectByType<AudioListener>())
        {
            Debug.LogWarning("[音频播放器] 场景中未找到AudioListener！请给主相机添加AudioListener组件！");
        }

        // 初始化所有音频的独立AudioSource
        InitAllAudioSources();
    }

    void Start()
    {
        // 启动自动播放
        if (playOnStart)
        {
            PlayAll();
        }
    }

    /// <summary>
    /// 初始化所有音频的独立AudioSource
    /// </summary>
    private void InitAllAudioSources()
    {
        for (int i = 0; i < audioItems.Count; i++)
        {
            AudioItem item = audioItems[i];
            if (item.clip == null) continue;

            // 为每个音频创建独立的AudioSource
            if (item.source == null)
            {
                item.source = gameObject.AddComponent<AudioSource>();
                item.source.playOnAwake = false;
                item.source.spatialBlend = item.use3DSound ? 1f : 0f; // 2D/3D切换
                item.source.volume = item.volume * globalVolume;
                item.source.pitch = item.pitch;
                item.source.loop = item.loop;
                item.source.clip = item.clip;
            }
        }
    }

    /// <summary>
    /// 播放所有音频（按各自延迟自动触发）
    /// </summary>
    public void PlayAll()
    {
        // 先停止所有播放中的音频
        StopAll();
        _isPlayingAll = true;

        int validAudioCount = 0;
        for (int i = 0; i < audioItems.Count; i++)
        {
            AudioItem item = audioItems[i];
            if (item.clip == null) continue;

            validAudioCount++;
            // 重新初始化（防止参数修改后未生效）
            UpdateAudioItemParams(i);

            if (item.delay <= 0)
            {
                // 无延迟，立即播放
                PlayAt(i);
            }
            else
            {
                // 有延迟，协程等待
                item.isPending = true;
                StartCoroutine(PlayWithDelay(i, item.delay));
                if (debugMode) Debug.Log($"[音频播放器] 音频「{item.clip.name}」将在 {item.delay:F1} 秒后播放");
            }
        }

        if (debugMode)
        {
            if (validAudioCount == 0)
                Debug.LogWarning("[音频播放器] 无有效音频（请检查是否拖入AudioClip）");
            else
                Debug.Log($"[音频播放器] 开始播放所有音频（共 {validAudioCount} 条有效）");
        }
    }

    /// <summary>
    /// 延迟播放协程
    /// </summary>
    private IEnumerator PlayWithDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioItem item = audioItems[index];
        if (item != null)
        {
            item.isPending = false;
            PlayAt(index);
        }
    }

    /// <summary>
    /// 播放指定索引的音频
    /// </summary>
    /// <param name="index">音频列表的索引</param>
    public void PlayAt(int index)
    {
        // 索引校验
        if (index < 0 || index >= audioItems.Count)
        {
            Debug.LogWarning($"[音频播放器] 索引 {index} 超出范围（列表共 {audioItems.Count} 条）");
            return;
        }

        AudioItem item = audioItems[index];
        // 音频为空校验
        if (item.clip == null)
        {
            Debug.LogWarning($"[音频播放器] 第 {index} 条音频为空，跳过播放");
            return;
        }

        // 确保AudioSource存在
        if (item.source == null)
        {
            InitAllAudioSources();
            if (item.source == null) return;
        }

        // 播放音频
        item.source.Play();
        item.isPlaying = true;
        if (debugMode)
        {
            Debug.Log($"[音频播放器] 播放音频「{item.clip.name}」" +
                      $"\n- 速率：{item.pitch:F1}x | 音量：{item.volume:F1} | 循环：{item.loop} | 3D音效：{item.use3DSound}");
        }

        // 非循环音频：播放完毕后重置状态
        if (!item.loop)
        {
            StartCoroutine(WaitForAudioEnd(item));
        }
    }

    /// <summary>
    /// 等待音频播放完毕并重置状态
    /// </summary>
    private IEnumerator WaitForAudioEnd(AudioItem item)
    {
        while (item.source != null && item.source.isPlaying)
        {
            yield return null;
        }
        item.isPlaying = false;
        // 检查是否所有音频都播放完毕
        CheckAllAudioDone();
    }

    /// <summary>
    /// 更新音频项的参数（防止Inspector修改后未生效）
    /// </summary>
    private void UpdateAudioItemParams(int index)
    {
        AudioItem item = audioItems[index];
        if (item.source == null || item.clip == null) return;

        item.source.clip = item.clip;
        item.source.pitch = item.pitch;
        item.source.volume = item.volume * globalVolume;
        item.source.loop = item.loop;
        item.source.spatialBlend = item.use3DSound ? 1f : 0f;
    }

    /// <summary>
    /// 检查所有音频是否播放完毕
    /// </summary>
    private void CheckAllAudioDone()
    {
        if (!_isPlayingAll) return;

        foreach (var item in audioItems)
        {
            if (item.isPlaying || item.isPending) return;
        }

        _isPlayingAll = false;
        if (debugMode) Debug.Log("[音频播放器] 所有音频播放完毕");
    }

    /// <summary>
    /// 停止所有音频播放
    /// </summary>
    public void StopAll()
    {
        StopAllCoroutines();
        foreach (var item in audioItems)
        {
            if (item.source != null && item.source.isPlaying)
            {
                item.source.Stop();
            }
            item.isPlaying = false;
            item.isPending = false;
        }
        _isPlayingAll = false;
        if (debugMode) Debug.Log("[音频播放器] 已停止所有音频");
    }

    /// <summary>
    /// 暂停所有音频
    /// </summary>
    public void PauseAll()
    {
        foreach (var item in audioItems)
        {
            if (item.source != null && item.source.isPlaying)
            {
                item.source.Pause();
            }
        }
        if (debugMode) Debug.Log("[音频播放器] 已暂停所有音频");
    }

    /// <summary>
    /// 恢复所有音频播放
    /// </summary>
    public void ResumeAll()
    {
        foreach (var item in audioItems)
        {
            if (item.source != null && !item.source.isPlaying && item.isPlaying)
            {
                item.source.UnPause();
            }
        }
        if (debugMode) Debug.Log("[音频播放器] 已恢复所有音频");
    }

    /// <summary>
    /// 获取当前是否有音频在播放/等待
    /// </summary>
    public bool IsAnyAudioPlaying => audioItems.Exists(item => item.isPlaying || item.isPending);

    /// <summary>
    /// 清理动态创建的AudioSource
    /// </summary>
    void OnDestroy()
    {
        foreach (var item in audioItems)
        {
            if (item.source != null)
            {
                Destroy(item.source);
            }
        }
    }
}