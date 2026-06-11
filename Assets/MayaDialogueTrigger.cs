using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 玛雅遗迹对话触发器脚本（完整版）
/// 步骤1-7：踢球前剧情
/// 步骤8-14：踢球后剧情
/// </summary>
public class MayaDialogueTrigger : MonoBehaviour
{
    [Header("语音播放源")]
    [Tooltip("语音从哪个物体播放（不填则自动查找 Main Camera）")]
    public GameObject audioSourceTarget;

    // 角色模型
    public GameObject noah;
    public GameObject pangpang;
    public GameObject highPriest;

    // ====== 踢球前语音 ======
    [Header("诺亚的语音（步骤1、3、5）")]
    public AudioClip noahStep1Audio;
    public AudioClip noahStep3Audio;
    public AudioClip noahStep5Audio;

    [Header("胖胖的语音（步骤2、4、7）")]
    public AudioClip pangpangStep2Audio;
    public AudioClip pangpangStep4Audio;
    public AudioClip pangpangStep7Audio;

    [Header("大祭司的语音（步骤6）")]
    public AudioClip priestStep6Audio;

    // ====== 踢球后语音 ======
    [Header("胖胖的语音（步骤8）")]
    public AudioClip pangpangStep8Audio;   // "哈哈，这个游戏真不错..."

    [Header("大祭司的语音（步骤9、11）")]
    public AudioClip priestStep9Audio;     // "你们真厉害..."
    public AudioClip priestStep11Audio;    // "遥远的未来？..."

    [Header("诺亚的语音（步骤10、13）")]
    public AudioClip noahStep10Audio;      // "呃呃...来自遥远的未来"
    public AudioClip noahStep13Audio;      // "你可别笑了..."

    [Header("胖胖的语音（步骤12、14）")]
    public AudioClip pangpangStep12Audio;  // "大祭司真有趣..."
    public AudioClip pangpangStep14Audio;  // "你们快看，发光了！"

    // 触发器
    public Collider ruinTrigger;
    public Collider priestTrigger;

    [Header("触发物体设置")]
    [Tooltip("能触发 ruinTrigger 的物体（不填则默认用 Tag: Player）")]
    public GameObject ruinTriggerTarget;
    [Tooltip("能触发 priestTrigger 的物体（不填则默认用 Tag: Player）")]
    public GameObject priestTriggerTarget;

    // 箭头
    public LineRenderer firstArrowLine;
    public LineRenderer secondArrowLine;
    public LineRenderer postGameArrow;     // 踢球后的箭头（最后一句结束后显示）

    [Header("播放设置")]
    public float dialogueDelay = 1.0f;

    [Header("语音播放设置")]
    [Tooltip("语音播放速度倍率（1.0=原速，1.5=1.5倍速，0.8=慢速）")]
    [Range(0.5f, 2.0f)]
    public float dialogueAudioSpeed = 1.0f;
    [Tooltip("对话音量（0.0~1.0）")]
    [Range(0f, 1f)]
    public float dialogueVolume = 1.0f;

    [Header("移动设置")]
    public Transform noahTargetPosition;
    public Transform pangpangTargetPosition;
    public float moveSpeed = 2.0f;
    public float stopDistance = 0.5f;

    [Header("动画设置")]
    [Tooltip("走路动画的 Bool 参数名称")]
    public string walkTrigger = "isW";

    [Tooltip("待机动画的 Bool 参数名称")]
    public string idleTrigger = "isId";

    [Header("转向设置")]
    public bool turnToPriestOnArrive = true;

    // ====== 踢球小游戏设置 ======
    [Header("踢球小游戏")]
    public string kickBallSceneName = "KickBallScene";  // 踢球场景名称
    public float fadeDuration = 0.5f;                    // 淡入淡出时间

    // ====== 传送设置 ======
    [Header("传送设置")]
    public Collider teleportTrigger;     // 光圈触发器（玩家走进去触发结束）
    public float teleportDelay = 3f;     // 显示结束画面后多久自动退出
    public string nextSceneName;         // 传送后的场景（可选）

    [Header("传送触发目标")]
    [Tooltip("能触发传送光圈的目标（留空则接受任何碰撞体）")]
    public GameObject teleportTriggerTarget;

    [Header("结束画面")]
    public float endScreenDelay = 2f;    // 传送后多久显示结束画面

    [Header("状态保存")]
    public Transform playerPositionAfterKickBall;  // 踢球回来后玩家位置
    public Transform noahPositionAfterKickBall;    // 踢球回来后诺亚位置
    public Transform pangpangPositionAfterKickBall;// 踢球回来后胖胖位置

    private AudioSource currentAudioSource;
    private int currentStep = 0;
    private bool isDialoguePlaying = false;

    private bool hasRuinTriggerTriggered = false;
    private bool hasPriestTriggerTriggered = false;
    private bool isPlayerInPriestTrigger = false;

    private bool isMoving = false;
    private bool noahArrived = false;
    private bool pangpangArrived = false;

    private Animator noahAnimator;
    private Animator pangpangAnimator;

    // 淡入淡出
    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;
    private bool showEndScreen = false;

    void Awake()
    {
        // 创建淡出纹理
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, Color.black);
        fadeTexture.Apply();
    }

    void Start()
    {
        if (noah != null) noahAnimator = noah.GetComponent<Animator>();
        if (pangpang != null) pangpangAnimator = pangpang.GetComponent<Animator>();

        // 初始化语音播放源（摄像头）
        InitAudioSourceTarget();

        InitArrowLine();

        // 检查是否是从踢球场景回来的
        if (PlayerPrefs.GetInt("ReturnFromKickBall", 0) == 1)
        {
            PlayerPrefs.SetInt("ReturnFromKickBall", 0);
            PlayerPrefs.Save();
            Debug.Log("【场景】从踢球场景返回，恢复状态并继续后续剧情");
            // 延迟一帧恢复，确保场景完全加载
            Invoke(nameof(RestoreStateAfterKickBall), 0.1f);
        }
        else
        {
            // 正常初始化（第一次进入场景）
            // 隐藏踢球后箭头
            if (postGameArrow != null)
                postGameArrow.enabled = false;
        }

        if (ruinTrigger != null)
        {
            ruinTrigger.isTrigger = true;
            Debug.Log($"【触发器初始化】ruinTrigger 已绑定，目标物体: {ruinTriggerTarget?.name ?? "默认Player"}");

            UnityAction<Collider> enterAction = (col) =>
            {
                Debug.Log($"【触发检测】ruinTrigger 检测到: {col.name}, 目标: {ruinTriggerTarget?.name}, 是否相等: {col.gameObject == ruinTriggerTarget}");

                // 检查是否是指定物体，或者默认用 Player Tag
                bool isValidTarget = (ruinTriggerTarget != null && col.gameObject == ruinTriggerTarget)
                                     || (ruinTriggerTarget == null && col.CompareTag("Player"));

                Debug.Log($"【触发判断】isValidTarget={isValidTarget}, currentStep={currentStep}, hasRuinTriggerTriggered={hasRuinTriggerTriggered}");

                if (isValidTarget && currentStep == 0 && !hasRuinTriggerTriggered)
                {
                    hasRuinTriggerTriggered = true;
                    OnRuinTriggerArrowSwitch();
                    StartDialogueStep(1);
                }
            };
            ruinTrigger.gameObject.AddComponent<TriggerEvent>().onTriggerEnterEvent = enterAction;
        }

        if (priestTrigger != null)
        {
            priestTrigger.isTrigger = true;

            UnityAction<Collider> priestEnterAction = (col) =>
            {
                // 检查是否是指定物体，或者默认用 Player Tag
                bool isValidTarget = (priestTriggerTarget != null && col.gameObject == priestTriggerTarget)
                                     || (priestTriggerTarget == null && col.CompareTag("Player"));

                if (isValidTarget)
                {
                    isPlayerInPriestTrigger = true;
                    TryTriggerPriestDialogue();
                }
            };
            UnityAction<Collider> priestExitAction = (col) =>
            {
                // 检查是否是指定物体，或者默认用 Player Tag
                bool isValidTarget = (priestTriggerTarget != null && col.gameObject == priestTriggerTarget)
                                     || (priestTriggerTarget == null && col.CompareTag("Player"));

                if (isValidTarget)
                {
                    isPlayerInPriestTrigger = false;
                }
            };

            TriggerEvent triggerEvent = priestTrigger.gameObject.AddComponent<TriggerEvent>();
            triggerEvent.onTriggerEnterEvent = priestEnterAction;
            triggerEvent.onTriggerExitEvent = priestExitAction;
        }

        // 绑定光圈触发器
        BindTeleportTrigger();

        // 初始隐藏光圈
        if (teleportTrigger != null)
            teleportTrigger.gameObject.SetActive(false);
    }



    void Update()
    {
        if (isMoving)
        {
            if (noahTargetPosition != null)
                UpdateCharacterMovement(noah, noahTargetPosition.position, ref noahArrived);
            else
                noahArrived = true;

            if (pangpangTargetPosition != null)
                UpdateCharacterMovement(pangpang, pangpangTargetPosition.position, ref pangpangArrived);
            else
                pangpangArrived = true;

            if (noahArrived && pangpangArrived)
            {
                isMoving = false;

                SetAnimatorBool(noahAnimator, walkTrigger, false);
                SetAnimatorBool(noahAnimator, idleTrigger, true);
                SetAnimatorBool(pangpangAnimator, walkTrigger, false);
                SetAnimatorBool(pangpangAnimator, idleTrigger, true);

                if (turnToPriestOnArrive && highPriest != null)
                {
                    TurnToTarget(noah, highPriest.transform.position);
                    TurnToTarget(pangpang, highPriest.transform.position);
                }

                ShowSecondArrow();
                Debug.Log("【移动完成】诺亚和胖胖已到达，切换待机");
            }
        }

        if (isPlayerInPriestTrigger && !hasPriestTriggerTriggered)
        {
            TryTriggerPriestDialogue();
        }
    }

    // ====== 场景切换（踢球小游戏衔接）======

    /// <summary>
    /// 保存当前状态，准备跳转到踢球场景
    /// </summary>
    private void SaveStateBeforeKickBall()
    {
        // 保存角色位置
        if (noah != null)
        {
            PlayerPrefs.SetFloat("NoahPosX", noah.transform.position.x);
            PlayerPrefs.SetFloat("NoahPosY", noah.transform.position.y);
            PlayerPrefs.SetFloat("NoahPosZ", noah.transform.position.z);
        }
        if (pangpang != null)
        {
            PlayerPrefs.SetFloat("PangpangPosX", pangpang.transform.position.x);
            PlayerPrefs.SetFloat("PangpangPosY", pangpang.transform.position.y);
            PlayerPrefs.SetFloat("PangpangPosZ", pangpang.transform.position.z);
        }

        // 保存玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // VR玩家可能是Untagged，尝试通过MainCamera查找
            Camera mainCam = Camera.main;
            if (mainCam != null)
                player = mainCam.gameObject;
        }
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerPosX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPosY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", player.transform.position.z);
        }

        // 保存箭头状态
        PlayerPrefs.SetInt("FirstArrowEnabled", firstArrowLine != null && firstArrowLine.enabled ? 1 : 0);
        PlayerPrefs.SetInt("SecondArrowEnabled", secondArrowLine != null && secondArrowLine.enabled ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("【状态保存】已保存场景状态");
    }

    /// <summary>
    /// 从踢球场景回来后恢复状态
    /// </summary>
    private void RestoreStateAfterKickBall()
    {
        // 恢复角色位置
        if (noah != null)
        {
            float x = PlayerPrefs.GetFloat("NoahPosX", noah.transform.position.x);
            float y = PlayerPrefs.GetFloat("NoahPosY", noah.transform.position.y);
            float z = PlayerPrefs.GetFloat("NoahPosZ", noah.transform.position.z);
            noah.transform.position = new Vector3(x, y, z);
        }
        if (pangpang != null)
        {
            float x = PlayerPrefs.GetFloat("PangpangPosX", pangpang.transform.position.x);
            float y = PlayerPrefs.GetFloat("PangpangPosY", pangpang.transform.position.y);
            float z = PlayerPrefs.GetFloat("PangpangPosZ", pangpang.transform.position.z);
            pangpang.transform.position = new Vector3(x, y, z);
        }

        // 恢复玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // VR玩家可能是Untagged，尝试通过MainCamera查找
            Camera mainCam = Camera.main;
            if (mainCam != null)
                player = mainCam.gameObject;
        }
        if (player != null)
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX", player.transform.position.x);
            float y = PlayerPrefs.GetFloat("PlayerPosY", player.transform.position.y);
            float z = PlayerPrefs.GetFloat("PlayerPosZ", player.transform.position.z);
            player.transform.position = new Vector3(x, y, z);
        }

        // 箭头状态（踢球后两个箭头都隐藏）
        if (firstArrowLine != null)
            firstArrowLine.enabled = false;
        if (secondArrowLine != null)
            secondArrowLine.enabled = false;

        Debug.Log("【状态恢复】已恢复场景状态");

        // 继续后续剧情
        Invoke(nameof(StartPostGameDialogue), 0.5f);
    }

    /// <summary>
    /// 步骤7结束后调用：保存状态 → 淡出 → 加载踢球场景
    /// </summary>
    private void GoToKickBallScene()
    {
        if (string.IsNullOrEmpty(kickBallSceneName))
        {
            Debug.Log("【跳过踢球】未设置踢球场景，直接继续后续剧情");
            StartPostGameDialogue();
            return;
        }

        // 先保存状态
        SaveStateBeforeKickBall();

        if (isFading) return;
        StartCoroutine(FadeAndLoadScene(kickBallSceneName));
    }

    IEnumerator FadeAndLoadScene(string sceneName)
    {
        isFading = true;

        // 淡出
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 加载场景
        SceneManager.LoadScene(sceneName);
        yield return null;

        // 淡入
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isFading = false;
    }

    IEnumerator Fade(float from, float to, float duration)
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

    /// <summary>
    /// 从踢球场景回来后，开始后续剧情
    /// </summary>
    private void StartPostGameDialogue()
    {
        Debug.Log("【后续剧情】开始步骤8");
        currentStep = 7; // 设置为7，这样StartDialogueStep(8)能通过检查
        StartDialogueStep(8);
    }

    // ====== 以下为原有代码（未修改）======

    private void UpdateCharacterMovement(GameObject character, Vector3 targetPos, ref bool arrived)
    {
        if (character == null || arrived)
            return;

        Vector3 characterPos = character.transform.position;
        float distance = Vector3.Distance(characterPos, targetPos);

        if (distance > stopDistance)
        {
            Vector3 direction = (targetPos - characterPos).normalized;
            character.transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
            {
                character.transform.rotation = Quaternion.Slerp(
                    character.transform.rotation,
                    Quaternion.LookRotation(direction),
                    10f * Time.deltaTime);
            }
        }
        else
        {
            arrived = true;
        }
    }

    private void TurnToTarget(GameObject character, Vector3 targetPos)
    {
        if (character == null)
            return;

        Vector3 direction = (targetPos - character.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            character.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void SetAnimatorBool(Animator animator, string paramName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
        {
            Debug.LogWarning($"【动画】animator为空或参数名为空");
            return;
        }

        int paramHash = Animator.StringToHash(paramName);
        if (animator.HasParameter(paramHash, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(paramHash, value);
            Debug.Log($"【动画】设置 {animator.gameObject.name} 的 {paramName} = {value}，当前值: {animator.GetBool(paramHash)}");
        }
        else
        {
            Debug.LogWarning($"【动画】Animator 没有 Bool 参数: {paramName}");
        }
    }

    private void StartMoveToTarget()
    {
        noahArrived = false;
        pangpangArrived = false;
        isMoving = true;

        SetAnimatorBool(noahAnimator, walkTrigger, true);
        SetAnimatorBool(noahAnimator, idleTrigger, false);
        SetAnimatorBool(pangpangAnimator, walkTrigger, true);
        SetAnimatorBool(pangpangAnimator, idleTrigger, false);

        Debug.Log("【开始移动】走路动画+移动同时开始");
    }

    private void TryTriggerPriestDialogue()
    {
        if (currentStep == 5 && !isDialoguePlaying && !hasPriestTriggerTriggered && !isMoving)
        {
            hasPriestTriggerTriggered = true;
            OnPriestTriggerArrowSwitch();
            StartDialogueStep(6);
        }
    }

    public void StartDialogueStep(int step)
    {
        if (step < 1 || step > 14 || isDialoguePlaying || currentStep > step)
            return;

        currentStep = step;
        isDialoguePlaying = true;

        switch (step)
        {
            // ====== 踢球前（步骤1-7）======
            case 1:
                if (noahStep1Audio != null) PlayDialogue(noah, noahStep1Audio);
                else OnDialogueEnd();
                NoahAction1();
                break;
            case 2:
                if (pangpangStep2Audio != null) PlayDialogue(pangpang, pangpangStep2Audio);
                else OnDialogueEnd();
                PangpangAction2();
                break;
            case 3:
                if (noahStep3Audio != null) PlayDialogue(noah, noahStep3Audio);
                else OnDialogueEnd();
                NoahAction3();
                break;
            case 4:
                if (pangpangStep4Audio != null) PlayDialogue(pangpang, pangpangStep4Audio);
                else OnDialogueEnd();
                PangpangAction4();
                break;
            case 5:
                if (noahStep5Audio != null) PlayDialogue(noah, noahStep5Audio);
                else OnDialogueEnd();
                NoahAction5();
                break;
            case 6:
                if (priestStep6Audio != null) PlayDialogue(highPriest, priestStep6Audio);
                else OnDialogueEnd();
                PriestAction6();
                break;
            case 7:
                if (pangpangStep7Audio != null) PlayDialogue(pangpang, pangpangStep7Audio);
                else OnDialogueEnd();
                PangpangAction7();
                break;

            // ====== 踢球后（步骤8-14）======
            case 8:
                if (pangpangStep8Audio != null) PlayDialogue(pangpang, pangpangStep8Audio);
                else OnDialogueEnd();
                break;
            case 9:
                if (priestStep9Audio != null) PlayDialogue(highPriest, priestStep9Audio);
                else OnDialogueEnd();
                break;
            case 10:
                if (noahStep10Audio != null) PlayDialogue(noah, noahStep10Audio);
                else OnDialogueEnd();
                break;
            case 11:
                if (priestStep11Audio != null) PlayDialogue(highPriest, priestStep11Audio);
                else OnDialogueEnd();
                break;
            case 12:
                if (pangpangStep12Audio != null) PlayDialogue(pangpang, pangpangStep12Audio);
                else OnDialogueEnd();
                break;
            case 13:
                if (noahStep13Audio != null) PlayDialogue(noah, noahStep13Audio);
                else OnDialogueEnd();
                break;
            case 14:
                if (pangpangStep14Audio != null) PlayDialogue(pangpang, pangpangStep14Audio);
                else OnDialogueEnd();
                // 步骤14开始时显示光圈
                if (teleportTrigger != null)
                    teleportTrigger.gameObject.SetActive(true);
                break;
        }
    }

    private void PlayDialogue(GameObject character, AudioClip audio)
    {
        if (audio == null)
        {
            OnDialogueEnd();
            return;
        }

        // 从指定的播放源（摄像头）播放语音
        GameObject source = audioSourceTarget;
        if (source == null)
        {
            // 自动查找 Main Camera
            source = GameObject.Find("Main Camera");
            if (source == null)
            {
                Debug.LogWarning("【语音】未找到 Main Camera，请手动设置 audioSourceTarget！");
                OnDialogueEnd();
                return;
            }
        }

        currentAudioSource = source.GetComponent<AudioSource>();
        if (currentAudioSource == null)
        {
            currentAudioSource = source.AddComponent<AudioSource>();
            currentAudioSource.spatialBlend = 0f; // 2D 音效，不随距离衰减
            currentAudioSource.volume = 1f;
            currentAudioSource.playOnAwake = false;
        }

        currentAudioSource.clip = audio;
        currentAudioSource.pitch = dialogueAudioSpeed;
        currentAudioSource.volume = dialogueVolume;
        currentAudioSource.Play();

        Debug.Log($"【语音】从 {source.name} 播放: {character?.name} 的语音");
        Invoke(nameof(OnDialogueEnd), audio.length / dialogueAudioSpeed + dialogueDelay);
    }

    private void InitAudioSourceTarget()
    {
        if (audioSourceTarget == null)
        {
            audioSourceTarget = GameObject.Find("Main Camera");
            if (audioSourceTarget == null)
            {
                // 尝试 Camera Offset/Main Camera
                GameObject cameraOffset = GameObject.Find("Camera Offset");
                if (cameraOffset != null)
                    audioSourceTarget = cameraOffset.transform.Find("Main Camera")?.gameObject;
            }
        }

        if (audioSourceTarget != null)
        {
            if (audioSourceTarget.GetComponent<AudioSource>() == null)
            {
                AudioSource audioSource = audioSourceTarget.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D 音效
                audioSource.volume = 1f;
                audioSource.playOnAwake = false;
            }
            Debug.Log($"【语音】播放源设置为: {audioSourceTarget.name}");
        }
        else
        {
            Debug.LogWarning("【语音】未找到 Main Camera，请在 Inspector 中手动设置 audioSourceTarget");
        }
    }

    private void OnDialogueEnd()
    {
        isDialoguePlaying = false;

        switch (currentStep)
        {
            // ====== 踢球前 ======
            case 1: StartDialogueStep(2); break;
            case 2: StartDialogueStep(3); break;
            case 3: StartDialogueStep(4); break;
            case 4: StartDialogueStep(5); break;
            case 5: StartMoveToTarget(); break;
            case 6: Invoke(nameof(PlayStep7), dialogueDelay); break;
            case 7: GoToKickBallScene(); break;  // 步骤7结束 → 跳踢球场景

            // ====== 踢球后 ======
            case 8: StartDialogueStep(9); break;
            case 9: StartDialogueStep(10); break;
            case 10: StartDialogueStep(11); break;
            case 11: StartDialogueStep(12); break;
            case 12: StartDialogueStep(13); break;
            case 13: StartDialogueStep(14); break;
            case 14:
                // 步骤14结束，显示箭头
                if (postGameArrow != null)
                    postGameArrow.enabled = true;
                Debug.Log("【剧情】全部对话结束，等待玩家走进光圈");
                break;
        }
    }

    private void PlayStep7()
    {
        StartDialogueStep(7);
    }

    #region 箭头指示line
    private void InitArrowLine()
    {
        if (firstArrowLine != null)
            firstArrowLine.enabled = true;
        if (secondArrowLine != null)
            secondArrowLine.enabled = false;
    }

    private void OnRuinTriggerArrowSwitch()
    {
        if (firstArrowLine != null)
            firstArrowLine.enabled = false;
    }

    private void ShowSecondArrow()
    {
        if (secondArrowLine != null)
            secondArrowLine.enabled = true;
    }

    private void OnPriestTriggerArrowSwitch()
    {
        if (secondArrowLine != null)
            secondArrowLine.enabled = false;
    }
    #endregion

    #region 角色动作
    private void NoahAction1() => Debug.Log("诺亚：抬手指向遗迹");
    private void PangpangAction2() => Debug.Log("胖胖：瞪圆眼睛、揉脸颊");
    private void NoahAction3() => Debug.Log("诺亚：拍胖胖肩膀，指向球场");
    private void PangpangAction4() => Debug.Log("胖胖：拽诺亚衣袖，压低声音");
    private void NoahAction5() => Debug.Log("诺亚：点头、做出走的手势");
    private void PriestAction6() => Debug.Log("大祭司：转身、打量两人");
    private void PangpangAction7() => Debug.Log("胖胖：点头、身体前倾");
    #endregion

    #region 传送
    /// <summary>
    /// 绑定光圈触发器
    /// </summary>
    private void BindTeleportTrigger()
    {
        if (teleportTrigger != null)
        {
            teleportTrigger.isTrigger = true;
            var trigger = teleportTrigger.gameObject.AddComponent<TriggerEvent>();
            trigger.onTriggerEnterEvent = (col) =>
            {
                bool isTeleportTarget = (teleportTriggerTarget != null && col.gameObject == teleportTriggerTarget)
                                     || (teleportTriggerTarget == null);
                if (isTeleportTarget && currentStep == 14 && !isDialoguePlaying)
                {
                    Debug.Log("【剧情】玩家走进光圈，传送走了！");
                    TriggerEndSequence();
                }
            };
        }
    }

    /// <summary>
    /// 触发结束流程：淡出 → 跳转场景或显示结束画面
    /// </summary>
    private void TriggerEndSequence()
    {
        if (isFading) return;
        StartCoroutine(EndSequenceCoroutine());
    }

    IEnumerator EndSequenceCoroutine()
    {
        isFading = true;

        // 淡出
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 跳转场景或显示结束画面
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            // 跳转场景后淡入
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
            isFading = false;
        }
        else
        {
            // 显示结束页面，保持黑屏不淡回，永久停留
            showEndScreen = true;
            fadeAlpha = 0f; // 清除淡出遮罩，让结束页面自己的半透明背景显示
            isFading = false;
        }
    }

    private void ShowEndScreen()
    {
        showEndScreen = true;
        Debug.Log("【剧情】本次体验结束");
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

        // 结束画面
        if (showEndScreen)
        {
            // 半透明黑色背景
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
            GUI.color = Color.white;

            // 结束文字
            GUIStyle style = new GUIStyle();
            style.fontSize = 48;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(0, Screen.height / 2 - 60, Screen.width, 80), "本次体验结束", style);

            GUIStyle subStyle = new GUIStyle();
            subStyle.fontSize = 24;
            subStyle.alignment = TextAnchor.MiddleCenter;
            subStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

            GUI.Label(new Rect(0, Screen.height / 2 + 30, Screen.width, 40), "感谢游玩", subStyle);
        }
    }
    #endregion

    private void DialogueEnd()
    {
        Debug.Log("玛雅遗迹对话剧情全部结束");
    }
}

public class TriggerEvent : MonoBehaviour
{
    public UnityAction<Collider> onTriggerEnterEvent;
    public UnityAction<Collider> onTriggerExitEvent;

    private void OnTriggerEnter(Collider other) => onTriggerEnterEvent?.Invoke(other);
    private void OnTriggerExit(Collider other) => onTriggerExitEvent?.Invoke(other);
}

public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, int hash, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == hash && param.type == type)
                return true;
        }
        return false;
    }
}
