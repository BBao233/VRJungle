using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 营地场景剧情控制器
///
/// 角色定义：characterA = 胖胖 | characterB = 诺亚
///
/// 完整剧情流程：
/// 1. 胖胖（characterA）从远处走过来（走路时播对话索引0）
/// 2. 胖胖到达火堆旁停下 → 播放对话索引1
/// 3. 诺亚（characterB）说话（索引0）
/// 4. 屏幕渐黑（睡觉）→ 角色移动到新位置
/// 5. 天空切换为白天 → 屏幕渐亮
/// 6. 诺亚说第二段话（索引1）
/// 7. 胖胖说第三段话（索引2）
/// 8. 诺亚说第三段话（索引2）
/// 9. 箭头引导
/// </summary>
public class CampSceneController : MonoBehaviour
{
    [Header("=== 角色引用 ===")]
    [Tooltip("角色A = 胖胖（走路的那个）")]
    public CharacterAnimatorController characterA;
    [Tooltip("角色A的对话组件")]
    public CharacterDialogue characterADialogue;

    [Tooltip("角色B = 诺亚（火堆旁的）")]
    public CharacterAnimatorController characterB;
    [Tooltip("角色B的对话组件")]
    public CharacterDialogue characterBDialogue;

    [Header("=== 触发器引用 ===")]
    public VRSceneTrigger triggerPlayerPosition;

    [Header("=== 管理器引用 ===")]
    public ScreenFadeController fadeController;
    public SkyboxSwitcher skyboxSwitcher;

    [Header("=== 胖胖走路位置设置 ===")]
    [Tooltip("胖胖的起始位置（远处）")]
    public Vector3 characterAStartPosition;
    [Tooltip("胖胖的目标位置（火堆旁）")]
    public Vector3 characterATargetPosition;
    [Tooltip("胖胖移动速度")]
    public float characterAMoveSpeed = 1.5f;

    [Header("=== 第一段剧情 - 对话配置（睡觉前） ===")]
    [Tooltip("胖胖走路时的对话索引")]
    public int characterAWalkDialogueIndex = 0;
    [Tooltip("胖胖到火堆后的对话索引")]
    public int characterACampDialogueIndex = 1;
    [Tooltip("诺亚睡觉前说的对话索引")]
    public int characterBDialogueIndex = 0;

    [Header("=== 睡觉设置 ===")]
    public float fadeToBlackDuration = 2f;
    public float sleepDuration = 3f;
    public float fadeFromBlackDuration = 2f;

    [Header("=== 睡觉后角色位置改变 ===")]
    public Transform playerRootTransform;
    public bool movePlayer = false;
    public Vector3 playerWakeUpPosition;
    [Tooltip("勾上 → 睡觉后移动胖胖（characterA）")]
    public bool moveCharacterA = false;
    [Tooltip("胖胖醒来后的新位置")]
    public Vector3 characterASecondPosition;
    [Tooltip("勾上 → 睡觉后移动诺亚（characterB）")]
    public bool moveCharacterB = false;
    [Tooltip("诺亚醒来后的新位置")]
    public Vector3 characterBSecondPosition;

    [Header("=== 醒来等待设置 ===")]
    [Tooltip("醒来渐亮后等待几秒再开始对话")]
    public float wakeUpWaitTime = 2f;

    [Header("=== 地面高度设置 ===")]
    public bool autoGroundHeight = true;
    public float groundOffset = 0f;
    public LayerMask groundLayer = 0;

    [Header("=== 第二段剧情 - 对话配置（睡觉后） ===")]
    [Tooltip("诺亚醒来后第一段对话索引")]
    public int characterBSecondDialogueIndex = 1;
    [Tooltip("胖胖醒来后的对话索引")]
    public int characterASecondDialogueIndex = 2;
    [Tooltip("诺亚最后一段对话索引（新增）")]
    public int noyaThirdDialogueIndex = 2;

    [Header("=== 箭头引导 ===")]
    [Tooltip("第一个箭头物体（场景开始显示，走进第一触发器后消失）")]
    public GameObject firstArrowObject;
    [Tooltip("第二个箭头物体（角色走到引导位置后显示，走进第二触发器后消失）")]
    public GameObject secondArrowObject;
    [Tooltip("第二次触发器引用")]
    public VRSceneTrigger secondGuideTrigger;
    [Tooltip("第二次触发器目标位置（箭头指向这里）")]
    public Vector3 secondGuideTargetPosition;

    [Header("=== 引导时角色移动设置 ===")]
    [Tooltip("对话结束后等待几秒再开始走路")]
    public float guideWaitTime = 2f;
    [Tooltip("角色走向目的地的速度")]
    public float guideMoveSpeed = 2f;
    [Tooltip("胖胖引导阶段的目标位置")]
    public Vector3 characterAGuideTargetPosition;
    [Tooltip("诺亚引导阶段的目标位置")]
    public Vector3 characterBGuideTargetPosition;

    [Header("=== 玩家到达引导目标后 ===")]
    [Tooltip("需要移开的草丛/灌木")]
    public GameObject bushObject;
    [Tooltip("草丛移开后的目标位置")]
    public Vector3 bushOpenPosition;
    [Tooltip("草丛移动动画时间（秒）")]
    public float bushMoveDuration = 0.5f;
    [Tooltip("诺亚Pick动作保持时间（秒）")]
    public float pickActionDuration = 1.5f;

    [Header("=== Pick后对话配置 ===")]
    [Tooltip("诺亚Pick后第一段对话索引")]
    public int noyaPostPickDialogueIndex = 3;
    [Tooltip("胖胖在诺亚之后的对话索引")]
    public int pandaPostPickDialogueIndex = 3;
    [Tooltip("诺亚最后一段对话索引（说完后渐黑）")]
    public int noyaFinalDialogueIndex = 3;

    [Header("=== 结尾渐黑设置 ===")]
    [Tooltip("最后一段对话结束后等待几秒再渐黑")]
    public float endWaitTime = 3f;

    [Header("=== 跳转场景 ===")]
    [Tooltip("剧情结束后自动加载的场景名称（留空不跳转）")]
    public string nextSceneName = "";
    [Tooltip("剧情结束后自动加载的场景Build Index（-1 = 不使用索引）")]
    public int nextSceneBuildIndex = -1;

    [Header("=== 调试 ===")]
    public bool debugMode = true;
    public bool verboseLogging = true;

    private bool _hasStarted = false;
    private bool _bushMoved = false;

    void Start()
    {
        // 自动查找对话组件
        if (characterADialogue == null && characterA != null)
            characterADialogue = characterA.GetComponent<CharacterDialogue>();
        if (characterBDialogue == null && characterB != null)
            characterBDialogue = characterB.GetComponent<CharacterDialogue>();

        // 初始化胖胖起始位置
        if (characterA != null && characterAStartPosition != Vector3.zero)
            characterA.transform.position = characterAStartPosition;

        // 绑定触发器
        if (triggerPlayerPosition != null)
            triggerPlayerPosition.onTriggerEnter.AddListener(OnPlayerReachedPosition);
        else
            Debug.LogError("[营地场景] 未设置触发器 triggerPlayerPosition！");

        if (secondGuideTrigger != null)
            secondGuideTrigger.onTriggerEnter.AddListener(OnPlayerReachedSecondGuide);

        // 初始化箭头显隐
        if (firstArrowObject != null)
            firstArrowObject.SetActive(true);   // 第一个箭头默认显示
        if (secondArrowObject != null)
            secondArrowObject.SetActive(false); // 第二个箭头默认隐藏
    }

    private void OnPlayerReachedPosition()
    {
        if (_hasStarted) return;
        _hasStarted = true;

        // 隐藏第一个箭头
        if (firstArrowObject != null)
            firstArrowObject.SetActive(false);

        if (debugMode) Debug.Log("[营地场景] 🎬 VR玩家到达指定位置，开始剧情！");
        StartCoroutine(RunCampScene());
    }

    private void OnPlayerReachedSecondGuide()
    {
        if (_bushMoved) return;
        _bushMoved = true;

        if (debugMode) Debug.Log("[营地场景] ✅ 玩家到达第二段引导目标位置！");

        // 隐藏第二个箭头
        if (secondArrowObject != null)
            secondArrowObject.SetActive(false);

        // 开始 Pick → 对话 → 渐黑 完整序列
        StartCoroutine(RunPostPickSequence());
    }

    private IEnumerator RunPostPickSequence()
    {
        // ═══════════════════════════════════════════
        // 阶段10：草丛移开 + 诺亚Pick动作
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段10：草丛移开，诺亚Pick");

        // 草丛移开
        if (bushObject != null && bushOpenPosition != Vector3.zero)
            yield return StartCoroutine(MoveBushCoroutine());

        // 诺亚做Pick动作
        if (characterB != null)
        {
            characterB.SetAnimatorBool("IsPick", true);
            if (debugMode) Debug.Log("[营地场景] 🎬 诺亚做Pick动作");
        }

        // 等待Pick动画播完
        yield return new WaitForSeconds(pickActionDuration);

        // 重置Pick，回到Idle
        if (characterB != null)
            characterB.SetAnimatorBool("IsPick", false);

        yield return new WaitForSeconds(0.5f);

        // ═══════════════════════════════════════════
        // 阶段11：诺亚说一段话（变回talk动作）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段11：诺亚说话");

        if (characterBDialogue != null)
        {
            characterBDialogue.PlayDialogue(noyaPostPickDialogueIndex);
            float timeout = 30f;
            while (characterBDialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { characterBDialogue.StopDialogue(); Debug.LogWarning("[营地场景] ⚠️ 诺亚Pick后对话超时"); }
        }

        yield return new WaitForSeconds(0.5f);

        // ═══════════════════════════════════════════
        // 阶段12：胖胖说一段话
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段12：胖胖说话");

        if (characterADialogue != null)
        {
            characterADialogue.PlayDialogue(pandaPostPickDialogueIndex);
            float timeout = 30f;
            while (characterADialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { characterADialogue.StopDialogue(); Debug.LogWarning("[营地场景] ⚠️ 胖胖Pick后对话超时"); }
        }

        yield return new WaitForSeconds(0.5f);

        // ═══════════════════════════════════════════
        // 阶段13：诺亚再说一段话
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段13：诺亚最后一段话");

        if (characterBDialogue != null)
        {
            characterBDialogue.PlayDialogue(noyaFinalDialogueIndex);
            float timeout = 30f;
            while (characterBDialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { characterBDialogue.StopDialogue(); Debug.LogWarning("[营地场景] ⚠️ 诺亚最后对话超时"); }
        }

        // ═══════════════════════════════════════════
        // 阶段14：等待 → 渐黑（结束）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log($"[营地场景] 📍 阶段14：等待 {endWaitTime} 秒后渐黑");

        yield return new WaitForSeconds(endWaitTime);

        if (fadeController != null)
        {
            fadeController.FadeToBlack(fadeToBlackDuration);
            while (fadeController.IsFading) yield return null;
        }

        if (debugMode) Debug.Log("[营地场景] 🎬 全部剧情结束 - 渐黑完成");

        // 跳转场景
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (debugMode) Debug.Log($"[营地场景] 🚀 跳转到场景: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
        else if (nextSceneBuildIndex >= 0)
        {
            if (debugMode) Debug.Log($"[营地场景] 🚀 跳转到场景索引: {nextSceneBuildIndex}");
            SceneManager.LoadScene(nextSceneBuildIndex);
        }
    }

    private IEnumerator MoveBushCoroutine()
    {
        Vector3 startPos = bushObject.transform.position;
        float elapsed = 0f;
        while (elapsed < bushMoveDuration)
        {
            elapsed += Time.deltaTime;
            bushObject.transform.position = Vector3.Lerp(startPos, bushOpenPosition, elapsed / bushMoveDuration);
            yield return null;
        }
        bushObject.transform.position = bushOpenPosition;
        if (debugMode) Debug.Log("[营地场景] 🌿 草丛已移开");
    }

    private IEnumerator RunCampScene()
    {
        // ═══════════════════════════════════════════
        // 阶段1：胖胖从远处走过来（characterA）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段1：胖胖开始走过来");

        if (characterA != null)
            characterA.MoveTo(characterATargetPosition, characterAMoveSpeed);

        if (characterADialogue != null)
            characterADialogue.PlayDialogue(characterAWalkDialogueIndex);

        if (characterA != null)
        {
            while (!characterA.HasReachedTarget) yield return null;
        }

        if (debugMode) Debug.Log("[营地场景] ✅ 胖胖已到达火堆旁");

        // ═══════════════════════════════════════════
        // 阶段2：胖胖到火堆后说话
        // ═══════════════════════════════════════════
        if (characterA != null)
        {
            characterA.SetWalking(false);
            characterA.SetTalking(false);
        }

        // 等走路对话播完
        if (characterADialogue != null)
        {
            float timeout = 30f;
            while (characterADialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { Debug.LogWarning("[营地场景] ⚠️ 胖胖走路对话超时"); characterADialogue.StopDialogue(); }
        }

        yield return new WaitForSeconds(1f);

        // 胖胖火堆对话
        if (debugMode) Debug.Log("[营地场景] 📍 阶段2：胖胖开始说话");

        if (characterADialogue != null)
        {
            characterADialogue.PlayDialogue(characterACampDialogueIndex);
            float timeout = 30f;
            while (characterADialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { Debug.LogWarning("[营地场景] ⚠️ 胖胖说话超时"); characterADialogue.StopDialogue(); }
        }

        // ═══════════════════════════════════════════
        // 阶段3：诺亚说话（睡觉前，characterB）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段3：诺亚开始说话（睡觉前）");

        if (characterBDialogue != null)
        {
            characterBDialogue.PlayDialogue(characterBDialogueIndex);
            float timeout = 30f;
            while (characterBDialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { Debug.LogWarning("[营地场景] ⚠️ 诺亚说话超时"); characterBDialogue.StopDialogue(); }
        }

        // ═══════════════════════════════════════════
        // 阶段4：屏幕渐黑（睡觉）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段4：渐黑，睡觉");

        if (fadeController != null)
        {
            fadeController.FadeToBlack(fadeToBlackDuration);
            while (fadeController.IsFading) yield return null;
        }

        yield return new WaitForSeconds(sleepDuration);

        // ═══════════════════════════════════════════
        // 阶段4.5：移动角色和玩家位置
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 移动角色位置");

        yield return new WaitForFixedUpdate();

        // 移动玩家
        if (movePlayer && playerRootTransform != null && playerWakeUpPosition != Vector3.zero)
        {
            Rigidbody prb = playerRootTransform.GetComponent<Rigidbody>();
            if (prb != null) { prb.velocity = Vector3.zero; prb.angularVelocity = Vector3.zero; prb.isKinematic = true; }
            playerRootTransform.position = playerWakeUpPosition;
            if (prb != null) prb.isKinematic = false;
        }

        // 移动胖胖 (characterA)
        if (moveCharacterA && characterA != null)
        {
            characterA.ResetState();
            Vector3 tp = characterASecondPosition;
            if (autoGroundHeight) { float gy = GetGroundHeight(tp); tp.y = gy + groundOffset; }
            Transform ct = characterA.transform;
            Rigidbody rb = characterA.GetComponent<Rigidbody>();
            if (rb != null) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
            ct.SetPositionAndRotation(tp, ct.rotation);
            yield return new WaitForFixedUpdate();
            if (Vector3.Distance(ct.position, tp) > 0.5f) ct.SetPositionAndRotation(tp, ct.rotation);
            if (playerRootTransform != null)
            {
                Vector3 dir = Vector3.ProjectOnPlane(playerRootTransform.position - tp, Vector3.up);
                if (dir.magnitude > 0.01f) ct.rotation = Quaternion.LookRotation(dir);
            }
            if (rb != null && !rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
            characterA.SetIdle(true);
        }

        // 移动诺亚 (characterB)
        if (moveCharacterB && characterB != null)
        {
            characterB.ResetState();
            Vector3 tp = characterBSecondPosition;
            if (autoGroundHeight) { float gy = GetGroundHeight(tp); tp.y = gy + groundOffset; }
            Transform ct = characterB.transform;
            Rigidbody rb = characterB.GetComponent<Rigidbody>();
            if (rb != null) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
            ct.SetPositionAndRotation(tp, ct.rotation);
            yield return new WaitForFixedUpdate();
            if (Vector3.Distance(ct.position, tp) > 0.5f) ct.SetPositionAndRotation(tp, ct.rotation);
            if (playerRootTransform != null)
            {
                Vector3 dir = Vector3.ProjectOnPlane(playerRootTransform.position - tp, Vector3.up);
                if (dir.magnitude > 0.01f) ct.rotation = Quaternion.LookRotation(dir);
            }
            if (rb != null && !rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
            characterB.SetIdle(true);
        }

        // ═══════════════════════════════════════════
        // 阶段5：天空切换 → 渐亮
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段5：切换天空，渐亮");

        if (skyboxSwitcher != null) skyboxSwitcher.SwitchToDay();
        yield return new WaitForSeconds(1f);

        if (fadeController != null)
        {
            fadeController.FadeFromBlack(fadeFromBlackDuration);
            while (fadeController.IsFading) yield return null;
        }

        // 醒后等待几秒再开始对话
        if (debugMode) Debug.Log($"[营地场景] ⏳ 醒后等待 {wakeUpWaitTime} 秒");
        yield return new WaitForSeconds(wakeUpWaitTime);

        // ═══════════════════════════════════════════
        // 阶段6：诺亚说第二段话（characterB）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段6：诺亚说第二段话");

        if (characterBDialogue != null)
        {
            characterBDialogue.PlayDialogue(characterBSecondDialogueIndex);
            float timeout = 30f;
            while (characterBDialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { characterBDialogue.StopDialogue(); Debug.LogWarning("[营地场景] ⚠️ 诺亚第二段超时"); }
        }

        yield return new WaitForSeconds(0.5f);

        // ═══════════════════════════════════════════
        // 阶段7：胖胖说第三段话（characterA）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段7：胖胖说话");

        if (characterADialogue != null)
        {
            characterADialogue.PlayDialogue(characterASecondDialogueIndex);
            float timeout = 30f;
            while (characterADialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { characterADialogue.StopDialogue(); Debug.LogWarning("[营地场景] ⚠️ 胖胖第二段超时"); }
        }

        yield return new WaitForSeconds(0.5f);

        // ═══════════════════════════════════════════
        // 阶段8：诺亚说第三段话（characterB，新增）
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log("[营地场景] 📍 阶段8：诺亚说第三段话");

        if (characterBDialogue != null)
        {
            characterBDialogue.PlayDialogue(noyaThirdDialogueIndex);
            float timeout = 30f;
            while (characterBDialogue.IsPlaying && timeout > 0f) { yield return null; timeout -= Time.deltaTime; }
            if (timeout <= 0f) { characterBDialogue.StopDialogue(); Debug.LogWarning("[营地场景] ⚠️ 诺亚第三段超时"); }
        }

        // ═══════════════════════════════════════════
        // 阶段9：等待 → 箭头引导 + 角色走向各自目的地 → 回到待机
        // ═══════════════════════════════════════════
        if (debugMode) Debug.Log($"[营地场景] 📍 阶段9：等待 {guideWaitTime} 秒后角色走向目的地");

        yield return new WaitForSeconds(guideWaitTime);

        // 胖胖走向自己的目标位置
        if (characterA != null && characterAGuideTargetPosition != Vector3.zero)
            characterA.MoveTo(characterAGuideTargetPosition, guideMoveSpeed);

        // 诺亚走向自己的目标位置
        if (characterB != null && characterBGuideTargetPosition != Vector3.zero)
            characterB.MoveTo(characterBGuideTargetPosition, guideMoveSpeed);

        // 等待两个角色都到达目的地
        if (characterA != null && characterAGuideTargetPosition != Vector3.zero)
            while (!characterA.HasReachedTarget) yield return null;
        if (characterB != null && characterBGuideTargetPosition != Vector3.zero)
            while (!characterB.HasReachedTarget) yield return null;

        // 两个角色回到待机
        if (debugMode) Debug.Log("[营地场景] 📍 两个角色到达目的地，回到待机");
        if (characterA != null) { characterA.SetWalking(false); characterA.SetIdle(true); }
        if (characterB != null) { characterB.SetWalking(false); characterB.SetIdle(true); }

        // 等待1秒后显示第二个箭头
        yield return new WaitForSeconds(1f);

        if (secondArrowObject != null)
            secondArrowObject.SetActive(true);

        // 启用玩家触发器（此时玩家可走向引导目标）
        if (secondGuideTrigger != null)
            secondGuideTrigger.SetActive(true);

        if (debugMode) Debug.Log("[营地场景] 🎬 引导完成，等待玩家走到触发器位置");
    }

    public void ManualStart() { OnPlayerReachedPosition(); }

    private float GetGroundHeight(Vector3 pos)
    {
        RaycastHit hit;
        Vector3 origin = new Vector3(pos.x, 100f, pos.z);
        bool hitSomething = groundLayer.value != 0
            ? Physics.Raycast(origin, Vector3.down, out hit, 200f, groundLayer)
            : Physics.Raycast(origin, Vector3.down, out hit, 200f);
        if (hitSomething) return hit.point.y;
        Debug.LogWarning($"[营地场景] ⚠️ 未检测到地面！使用Y={pos.y}");
        return pos.y;
    }

    public void ResetScene()
    {
        _hasStarted = false;
        if (characterA != null) characterA.ResetState();
        if (characterB != null) characterB.ResetState();
        if (characterADialogue != null) characterADialogue.StopDialogue();
        if (characterBDialogue != null) characterBDialogue.StopDialogue();
        if (triggerPlayerPosition != null) triggerPlayerPosition.ResetTrigger();
        if (fadeController != null) fadeController.SetClearImmediate();
        if (debugMode) Debug.Log("[营地场景] 🔄 场景已重置");
    }
}
