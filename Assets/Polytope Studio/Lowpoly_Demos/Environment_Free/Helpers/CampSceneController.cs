using UnityEngine;
using System.Collections;

/// <summary>
/// 营地场景剧情控制器（v2 — 每个角色自己管理对话和音频）
/// 
/// 完整剧情流程：
/// 1. VR玩家到达指定位置 → 触发角色A从远处走过来（走路时播放音频1）
/// 2. 角色A到达火堆旁停下 → 播放音频2 + 说话动画 + 字幕
/// 3. 角色A说完 → 角色B播放自己的音频 + 说话动画 + 字幕
/// 4. 对话结束 → 屏幕渐黑（睡觉）
/// 5. 一段时间后 → 天空切换为白天
/// 6. 引导VR玩家走到对应的位置
/// 
/// 挂载方式：在场景中创建空物体 "CampSceneController"，挂载此脚本
/// </summary>
public class CampSceneController : MonoBehaviour
{
    [Header("=== 角色引用 ===")]
    [Tooltip("角色A（从远处走过来的角色，需挂载 CharacterAnimatorController + CharacterDialogue")]
    public CharacterAnimatorController characterA;

    [Tooltip("角色A的对话组件（挂载在角色A上）")]
    public CharacterDialogue characterADialogue;

    [Tooltip("角色B（火堆旁的角色，需挂载 CharacterAnimatorController + CharacterDialogue")]
    public CharacterAnimatorController characterB;

    [Tooltip("角色B的对话组件（挂载在角色B上）")]
    public CharacterDialogue characterBDialogue;

    [Header("=== 触发器引用 ===")]
    [Tooltip("触发器：VR玩家走到指定位置后触发剧情")]
    public VRSceneTrigger triggerPlayerPosition;

    [Header("=== 管理器引用 ===")]
    [Tooltip("屏幕渐变控制器")]
    public ScreenFadeController fadeController;

    [Tooltip("天空切换控制器")]
    public SkyboxSwitcher skyboxSwitcher;

    [Tooltip("引导控制器")]
    public PlayerGuideController guideController;

    [Header("=== 位置设置 ===")]
    [Tooltip("角色A的起始位置（远处）")]
    public Vector3 characterAStartPosition;

    [Tooltip("角色A的目标位置（火堆旁）")]
    public Vector3 characterATargetPosition;

    [Tooltip("角色A移动速度")]
    public float characterAMoveSpeed = 1.5f;

    [Tooltip("引导玩家走到的目标位置")]
    public Vector3 playerGuideTarget;

    [Header("=== 角色A对话配置 ===")]
    [Tooltip("角色A走路时的对话索引（在CharacterDialogue的对话列表中）")]
    public int characterAWalkDialogueIndex = 0;

    [Tooltip("角色A停下后的对话索引")]
    public int characterACampDialogueIndex = 1;

    [Header("=== 角色B对话配置 ===")]
    [Tooltip("角色B说话的对话索引")]
    public int characterBDialogueIndex = 0;

    [Header("=== 时间设置 ===")]
    [Tooltip("渐黑时间")]
    public float fadeToBlackDuration = 2f;

    [Tooltip("睡觉时间（保持黑色的时长）")]
    public float sleepDuration = 3f;

    [Tooltip("渐亮时间")]
    public float fadeFromBlackDuration = 2f;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private bool _hasStarted = false;

    void Start()
    {
        // 自动查找对话组件（如果没手动拖入）
        if (characterADialogue == null && characterA != null)
        {
            characterADialogue = characterA.GetComponent<CharacterDialogue>();
        }
        if (characterBDialogue == null && characterB != null)
        {
            characterBDialogue = characterB.GetComponent<CharacterDialogue>();
        }

        // 初始化角色A位置
        if (characterA != null && characterAStartPosition != Vector3.zero)
        {
            characterA.transform.position = characterAStartPosition;
        }

        // 绑定触发器事件
        if (triggerPlayerPosition != null)
        {
            triggerPlayerPosition.onTriggerEnter.AddListener(OnPlayerReachedPosition);
        }
        else
        {
            Debug.LogError("[营地场景] 未设置触发器 triggerPlayerPosition！");
        }
    }

    /// <summary>
    /// VR玩家到达指定位置时触发
    /// </summary>
    private void OnPlayerReachedPosition()
    {
        if (_hasStarted) return;
        _hasStarted = true;

        if (debugMode)
            Debug.Log("[营地场景] 🎬 VR玩家到达指定位置，开始剧情！");

        StartCoroutine(RunCampScene());
    }

    /// <summary>
    /// 营地场景完整剧情流程
    /// </summary>
    private IEnumerator RunCampScene()
    {
        // ═══════════════════════════════════════════
        // 阶段1：角色A从远处走过来（走路动画 + 背景音频）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段1：角色A开始走过来");

        // 角色A开始移动（走路动画自动播放）
        if (characterA != null)
        {
            characterA.MoveTo(characterATargetPosition, characterAMoveSpeed);
        }

        // 角色A播放走路时的对话（音频+字幕，由角色自己管理）
        if (characterADialogue != null)
        {
            characterADialogue.PlayDialogue(characterAWalkDialogueIndex);
        }

        // 等待角色A到达目标
        if (characterA != null)
        {
            while (!characterA.HasReachedTarget)
            {
                yield return null;
            }
        }

        if (debugMode) Debug.Log("[营地场景] ✅ 角色A已到达火堆旁");

        // ═══════════════════════════════════════════
        // 阶段2：角色A停下，等待稳定后开始说话
        // ═══════════════════════════════════════════

        // 清除所有动画状态，强制回到待机
        if (characterA != null)
        {
            characterA.SetWalking(false);
            characterA.SetTalking(false);
        }

        // 等待角色A走路时的对话播放完（如果有）
        if (characterADialogue != null)
        {
            float timeout = 30f;
            while (characterADialogue.IsPlaying && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            if (timeout <= 0f)
            {
                Debug.LogWarning("[营地场景] ⚠️ 等待角色A走路对话超时");
                characterADialogue.StopDialogue();
            }
        }

        // 等待1秒，确保动画完全稳定
        yield return new WaitForSeconds(1f);

        if (debugMode) Debug.Log("[营地场景] 📍 阶段2：角色A开始说话");

        // 角色A播放停下后的对话（音频+字幕+说话动画，全部由角色自己管理）
        if (characterADialogue != null)
        {
            characterADialogue.PlayDialogue(characterACampDialogueIndex);
        }

        // 等待角色A对话播放完
        if (characterADialogue != null)
        {
            float timeout = 30f;
            while (characterADialogue.IsPlaying && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            if (timeout <= 0f)
            {
                Debug.LogWarning("[营地场景] ⚠️ 等待角色A说话对话超时");
                characterADialogue.StopDialogue();
            }
        }

        // ═══════════════════════════════════════════
        // 阶段3：角色B说话
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段3：角色B开始说话");

        // 角色B播放对话（音频+字幕+说话动画，全部由角色自己管理）
        if (characterBDialogue != null)
        {
            characterBDialogue.PlayDialogue(characterBDialogueIndex);
        }

        // 等待角色B对话播放完
        if (characterBDialogue != null)
        {
            float timeout = 30f;
            while (characterBDialogue.IsPlaying && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }
            if (timeout <= 0f)
            {
                Debug.LogWarning("[营地场景] ⚠️ 等待角色B对话超时");
                characterBDialogue.StopDialogue();
            }
        }

        // ═══════════════════════════════════════════
        // 阶段4：屏幕渐黑（睡觉）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段4：屏幕渐黑，睡觉");

        if (fadeController != null)
        {
            fadeController.FadeToBlack(fadeToBlackDuration);

            while (fadeController.IsFading)
            {
                yield return null;
            }
        }

        // 保持黑色（睡觉时间）
        yield return new WaitForSeconds(sleepDuration);

        // ═══════════════════════════════════════════
        // 阶段5：天空切换为白天
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段5：切换天空为白天");

        if (skyboxSwitcher != null)
        {
            skyboxSwitcher.SwitchToDay();
        }

        yield return new WaitForSeconds(1f);

        // ═══════════════════════════════════════════
        // 阶段6：屏幕渐亮，引导玩家移动
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段6：渐亮，引导玩家移动");

        if (fadeController != null)
        {
            fadeController.FadeFromBlack(fadeFromBlackDuration);

            while (fadeController.IsFading)
            {
                yield return null;
            }
        }

        // 显示引导标记
        if (guideController != null)
        {
            guideController.ShowGuide(playerGuideTarget);
        }

        if (debugMode)
            Debug.Log("[营地场景] 🎬 营地场景剧情流程执行完毕！等待玩家走到引导位置...");
    }

    /// <summary>
    /// 手动触发剧情开始（用于测试）
    /// </summary>
    public void ManualStart()
    {
        OnPlayerReachedPosition();
    }

    /// <summary>
    /// 重置场景（用于重新播放）
    /// </summary>
    public void ResetScene()
    {
        _hasStarted = false;

        if (characterA != null)
        {
            characterA.ResetState();
            if (characterAStartPosition != Vector3.zero)
                characterA.transform.position = characterAStartPosition;
        }

        if (characterADialogue != null)
        {
            characterADialogue.StopDialogue();
        }

        if (characterB != null)
        {
            characterB.ResetState();
        }

        if (characterBDialogue != null)
        {
            characterBDialogue.StopDialogue();
        }

        if (triggerPlayerPosition != null)
        {
            triggerPlayerPosition.ResetTrigger();
        }

        if (guideController != null)
        {
            guideController.HideGuide();
        }

        if (fadeController != null)
        {
            fadeController.SetClearImmediate();
        }

        if (debugMode)
            Debug.Log("[营地场景] 🔄 场景已重置");
    }
}
