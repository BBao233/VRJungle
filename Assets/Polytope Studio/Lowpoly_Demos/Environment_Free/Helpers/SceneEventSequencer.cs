using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;

/// <summary>
/// 场景事件序列管理器
/// 管理整个剧情流程的事件序列，支持等待、延迟、条件触发等
/// 
/// 使用方式：
/// 1. 在场景中创建空物体，挂载此脚本
/// 2. 在事件步骤列表中配置每一步的操作
/// 3. 通过外部触发器（如VRSceneTrigger）调用 StartSequence() 开始执行
/// </summary>
public class SceneEventSequencer : MonoBehaviour
{
    [System.Serializable]
    public enum EventType
    {
        /// <summary>等待指定秒数</summary>
        Wait,
        /// <summary>角色移动到目标位置</summary>
        CharacterMove,
        /// <summary>播放角色动画（通过Animator触发器）</summary>
        PlayAnimation,
        /// <summary>播放对话</summary>
        PlayDialogue,
        /// <summary>屏幕渐黑</summary>
        FadeToBlack,
        /// <summary>屏幕渐亮</summary>
        FadeFromBlack,
        /// <summary>切换天空盒</summary>
        SwitchSkybox,
        /// <summary>启用/禁用触发器</summary>
        SetTriggerActive,
        /// <summary>启用/禁用游戏物体</summary>
        SetGameObjectActive,
        /// <summary>启用引导标记</summary>
        EnableGuide,
        /// <summary>禁用引导标记</summary>
        DisableGuide,
        /// <summary>自定义UnityEvent回调</summary>
        CustomEvent,
        /// <summary>等待角色到达目标</summary>
        WaitForCharacterArrival,
        /// <summary>等待对话完成</summary>
        WaitForDialogueComplete,
        /// <summary>等待渐黑/渐亮完成</summary>
        WaitForFadeComplete,
    }

    [System.Serializable]
    public class SequenceStep
    {
        [Tooltip("步骤描述（仅用于编辑器备注）")]
        public string description = "";

        [Tooltip("事件类型")]
        public EventType eventType;

        [Header("--- 角色移动 ---")]
        [Tooltip("要移动的角色（CharacterAnimatorController）")]
        public CharacterAnimatorController moveCharacter;
        [Tooltip("移动目标位置")]
        public Vector3 moveTarget;
        [Tooltip("移动速度")]
        public float moveSpeed = 2f;

        [Header("--- 动画 ---")]
        [Tooltip("目标角色Animator")]
        public Animator targetAnimator;
        [Tooltip("Animator触发器名称")]
        public string animatorTrigger = "";

        [Header("--- 对话 ---")]
        [Tooltip("说话者名称")]
        public string speakerName = "";
        [Tooltip("对话内容")]
        public string dialogueText = "";
        [Tooltip("对话显示时间（秒），0=根据文字长度自动计算）")]
        public float dialogueDuration = 0f;

        [Header("--- 渐黑/渐亮 ---")]
        [Tooltip("渐变时间（秒）")]
        public float fadeDuration = 1.5f;

        [Header("--- 天空盒 ---")]
        [Tooltip("目标天空盒材质")]
        public Material targetSkybox;

        [Header("--- 触发器 ---")]
        [Tooltip("目标触发器")]
        public VRSceneTrigger targetTrigger;
        [Tooltip("启用或禁用")]
        public bool setActive = true;

        [Header("--- 游戏物体 ---")]
        [Tooltip("目标游戏物体")]
        public GameObject targetGameObject;

        [Header("--- 等待 ---")]
        [Tooltip("等待时间（秒）")]
        public float waitTime = 1f;

        [Header("--- 引导 ---")]
        [Tooltip("引导目标位置")]
        public Vector3 guideTarget;
        [Tooltip("引导标记物体（如箭头、光柱等）")]
        public GameObject guideMarker;

        [Header("--- 自定义 ---")]
        public UnityEvent customEvent;

        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("=== 事件序列 ===")]
    [Tooltip("事件步骤列表，按顺序执行")]
    public SequenceStep[] sequenceSteps;

    [Header("=== 设置 ===")]
    [Tooltip("是否在场景加载时自动开始")]
    public bool autoStart = false;
    [Tooltip("自动开始延迟（秒）")]
    public float autoStartDelay = 0f;

    [Header("=== 引用 ===")]
    [Tooltip("对话管理器（如果场景中有）")]
    public DialogueManager dialogueManager;
    [Tooltip("屏幕渐变控制器")]
    public ScreenFadeController fadeController;
    [Tooltip("引导控制器")]
    public PlayerGuideController guideController;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private int _currentStep = -1;
    private bool _isRunning = false;
    private Coroutine _sequenceCoroutine;

    /// <summary>
    /// 序列是否正在执行
    /// </summary>
    public bool IsRunning => _isRunning;

    void Start()
    {
        // 自动查找引用（如果未手动指定）
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (fadeController == null)
            fadeController = FindFirstObjectByType<ScreenFadeController>();
        if (guideController == null)
            guideController = FindFirstObjectByType<PlayerGuideController>();

        if (autoStart)
        {
            if (autoStartDelay > 0)
                Invoke(nameof(StartSequence), autoStartDelay);
            else
                StartSequence();
        }
    }

    /// <summary>
    /// 开始执行事件序列
    /// </summary>
    public void StartSequence()
    {
        if (_isRunning)
        {
            Debug.LogWarning("[事件序列] 序列已在执行中！");
            return;
        }

        _isRunning = true;
        _currentStep = -1;

        if (debugMode)
            Debug.Log($"[事件序列] 🎬 开始执行序列，共 {sequenceSteps.Length} 步");

        _sequenceCoroutine = StartCoroutine(RunSequence());
    }

    /// <summary>
    /// 停止序列
    /// </summary>
    public void StopSequence()
    {
        _isRunning = false;
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }
        if (debugMode)
            Debug.Log("[事件序列] ⏹ 序列已停止");
    }

    /// <summary>
    /// 跳到指定步骤
    /// </summary>
    public void JumpToStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= sequenceSteps.Length)
        {
            Debug.LogError($"[事件序列] 步骤索引 {stepIndex} 超出范围！");
            return;
        }

        StopSequence();
        _currentStep = stepIndex - 1;
        _isRunning = true;
        _sequenceCoroutine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        while (_currentStep < sequenceSteps.Length - 1 && _isRunning)
        {
            _currentStep++;
            SequenceStep step = sequenceSteps[_currentStep];
            step.isCompleted = false;

            if (debugMode)
                Debug.Log($"[事件序列] ▶ 步骤 {_currentStep + 1}/{sequenceSteps.Length}: {step.description} ({step.eventType})");

            yield return ExecuteStep(step);

            step.isCompleted = true;

            if (debugMode)
                Debug.Log($"[事件序列] ✅ 步骤 {_currentStep + 1} 完成");
        }

        _isRunning = false;

        if (debugMode)
            Debug.Log("[事件序列] 🎬 序列执行完毕！");
    }

    private IEnumerator ExecuteStep(SequenceStep step)
    {
        switch (step.eventType)
        {
            case EventType.Wait:
                yield return new WaitForSeconds(step.waitTime);
                break;

            case EventType.CharacterMove:
                if (step.moveCharacter != null)
                {
                    step.moveCharacter.MoveTo(step.moveTarget, step.moveSpeed);
                }
                else
                {
                    Debug.LogError($"[事件序列] 步骤 \"{step.description}\" 的 moveCharacter 为空！");
                }
                break;

            case EventType.WaitForCharacterArrival:
                if (step.moveCharacter != null)
                {
                    while (!step.moveCharacter.HasReachedTarget)
                    {
                        yield return null;
                    }
                }
                break;

            case EventType.PlayAnimation:
                if (step.targetAnimator != null && !string.IsNullOrEmpty(step.animatorTrigger))
                {
                    step.targetAnimator.SetTrigger(step.animatorTrigger);
                }
                else
                {
                    Debug.LogError($"[事件序列] 步骤 \"{step.description}\" 的Animator或触发器名为空！");
                }
                break;

            case EventType.PlayDialogue:
                if (dialogueManager != null)
                {
                    dialogueManager.ShowDialogue(step.speakerName, step.dialogueText, step.dialogueDuration);
                }
                else
                {
                    Debug.LogError("[事件序列] 未找到DialogueManager！");
                }
                break;

            case EventType.WaitForDialogueComplete:
                if (dialogueManager != null)
                {
                    while (dialogueManager.IsPlaying)
                    {
                        yield return null;
                    }
                }
                break;

            case EventType.FadeToBlack:
                if (fadeController != null)
                {
                    fadeController.FadeToBlack(step.fadeDuration);
                }
                else
                {
                    Debug.LogError("[事件序列] 未找到ScreenFadeController！");
                }
                break;

            case EventType.FadeFromBlack:
                if (fadeController != null)
                {
                    fadeController.FadeFromBlack(step.fadeDuration);
                }
                else
                {
                    Debug.LogError("[事件序列] 未找到ScreenFadeController！");
                }
                break;

            case EventType.WaitForFadeComplete:
                if (fadeController != null)
                {
                    while (fadeController.IsFading)
                    {
                        yield return null;
                    }
                }
                break;

            case EventType.SwitchSkybox:
                if (step.targetSkybox != null)
                {
                    RenderSettings.skybox = step.targetSkybox;
                    if (debugMode)
                        Debug.Log($"[事件序列] 🌅 天空盒已切换: {step.targetSkybox.name}");
                }
                else
                {
                    Debug.LogError($"[事件序列] 步骤 \"{step.description}\" 的天空盒材质为空！");
                }
                break;

            case EventType.SetTriggerActive:
                if (step.targetTrigger != null)
                {
                    step.targetTrigger.SetActive(step.setActive);
                }
                break;

            case EventType.SetGameObjectActive:
                if (step.targetGameObject != null)
                {
                    step.targetGameObject.SetActive(step.setActive);
                }
                break;

            case EventType.EnableGuide:
                if (guideController != null)
                {
                    guideController.ShowGuide(step.guideTarget, step.guideMarker);
                }
                break;

            case EventType.DisableGuide:
                if (guideController != null)
                {
                    guideController.HideGuide();
                }
                break;

            case EventType.CustomEvent:
                step.customEvent?.Invoke();
                break;
        }

        // 步骤间短暂间隔，避免同一帧连续触发
        yield return new WaitForEndOfFrame();
    }
}
