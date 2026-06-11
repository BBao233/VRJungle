using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// 一键创建完整的多状态Animator控制器
/// Idle使用角色自身FBX动画，Walk/Talk加载单独的动画文件
/// </summary>
public class CreateFixedControllers : EditorWindow
{
    [MenuItem("Tools/创建Fixed控制器")]
    static void CreateControllers()
    {
        // 先配置FBX动画循环设置
        ConfigureLooping();

        // 再创建控制器
        CreatePandaController();
        CreateNoyaController();

        AssetDatabase.Refresh();
        Debug.Log("🎉 创建完成！请在角色Animator组件中指定新控制器。");
    }

    /// <summary>
    /// 配置FBX动画片段开启循环播放
    /// </summary>
    static void ConfigureLooping()
    {
        ConfigureFBXLoop("Assets/Resources/roles/胖胖/Meshy_AI_Armored_Panda_biped 1/Meshy_AI_Armored_Panda_biped_Animation_Big_Wave_Hello_withSkin.fbx");
        ConfigureFBXLoop("Assets/Resources/roles/胖胖/Meshy_AI_Armored_Panda_biped 1/Meshy_AI_Armored_Panda_biped_Animation_Walking_withSkin.fbx");
        ConfigureFBXLoop("Assets/Resources/roles/胖胖/Meshy_AI_Armored_Panda_biped 1/Meshy_AI_Armored_Panda_biped_Animation_Stand_and_Chat_withSkin.fbx");
        ConfigureFBXLoop("Assets/Resources/roles/诺亚/Meshy_AI_诺亚_biped_Animation_Confused_Scratch_withSkin.fbx");
    }

    /// <summary>
    /// 设置单个FBX的动画循环
    /// </summary>
    static void ConfigureFBXLoop(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"⚠️ 无法获取FBX导入器: {fbxPath}");
            return;
        }

        // 如果已经有显式clip配置，修改现有配置
        // 如果没有，Unity会自动导入defaultClip
        ModelImporterClipAnimation[] clips = importer.clipAnimations;

        if (clips.Length == 0)
        {
            // 没有显式clip配置，需要创建默认配置
            // 从FBX中获取自动生成的clip名称
            string defaultClipName = GetDefaultClipName(fbxPath);
            if (string.IsNullOrEmpty(defaultClipName))
            {
                Debug.LogWarning($"⚠️ 无法获取默认clip名称: {fbxPath}");
                return;
            }

            clips = new ModelImporterClipAnimation[1];
            clips[0] = new ModelImporterClipAnimation();
            clips[0].name = defaultClipName;
            clips[0].loopTime = true;
            clips[0].loopPose = true;

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log($"✅ 已配置循环: {fbxPath} → clip: {defaultClipName}");
        }
        else
        {
            // 已有显式配置，修改循环设置
            bool changed = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i].loopTime)
                {
                    clips[i].loopTime = true;
                    clips[i].loopPose = true;
                    changed = true;
                    Debug.Log($"✅ 已开启循环: {fbxPath} → clip: {clips[i].name}");
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            else
            {
                Debug.Log($"ℹ️ 循环已开启，无需修改: {fbxPath}");
            }
        }
    }

    /// <summary>
    /// 获取FBX默认clip名称（第一个AnimationClip的名字）
    /// </summary>
    static string GetDefaultClipName(string fbxPath)
    {
        // 加载FBX中的所有AnimationClip
        Object[] clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object obj in clips)
        {
            if (obj is AnimationClip clip)
            {
                return clip.name;
            }
        }
        return null;
    }

    static void CreatePandaController()
    {
        // ===== 胖胖 =====
        string pandaFolder = "Assets/Resources/roles/胖胖/Meshy_AI_Armored_Panda_biped 1";

        // 1. Idle → 使用胖胖自身FBX的动画（Big_Wave_Hello）
        AnimationClip pandaIdleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            pandaFolder + "/Meshy_AI_Armored_Panda_biped_Animation_Big_Wave_Hello_withSkin.fbx");
        if (pandaIdleClip == null)
        {
            Debug.LogError("❌ 胖胖Idle动画加载失败！");
            LogClipsInFBX("Big_Wave_Hello");
            return;
        }
        Debug.Log($"✅ 胖胖Idle动画: {pandaIdleClip.name} (长度: {pandaIdleClip.length:F2}秒)");

        // 2. Walk → 加载Walking动画
        AnimationClip pandaWalkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            pandaFolder + "/Meshy_AI_Armored_Panda_biped_Animation_Walking_withSkin.fbx");
        if (pandaWalkClip == null)
        {
            Debug.LogError("❌ 胖胖Walk动画加载失败！");
            LogClipsInFBX("Walking");
        }
        else
        {
            Debug.Log($"✅ 胖胖Walk动画: {pandaWalkClip.name} (长度: {pandaWalkClip.length:F2}秒)");
        }

        // 3. Talk → 加载Stand_and_Chat动画
        AnimationClip pandaTalkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            pandaFolder + "/Meshy_AI_Armored_Panda_biped_Animation_Stand_and_Chat_withSkin.fbx");
        if (pandaTalkClip == null)
        {
            Debug.LogError("❌ 胖胖Talk动画加载失败！");
            LogClipsInFBX("Stand_and_Chat");
        }
        else
        {
            Debug.Log($"✅ 胖胖Talk动画: {pandaTalkClip.name} (长度: {pandaTalkClip.length:F2}秒)");
        }

        string pandaControllerPath = pandaFolder + "/PandaController_FromScript.controller";
        if (File.Exists(pandaControllerPath))
        {
            AssetDatabase.DeleteAsset(pandaControllerPath);
            AssetDatabase.Refresh();
        }

        var pandaController = AnimatorController.CreateAnimatorControllerAtPath(pandaControllerPath);
        var pandaLayer = pandaController.layers[0];
        var pandaStateMachine = pandaLayer.stateMachine;

        pandaStateMachine.defaultState = null;

        var idleState = pandaStateMachine.AddState("Idle");
        idleState.motion = pandaIdleClip;
        idleState.writeDefaultValues = true;

        var walkState = pandaStateMachine.AddState("Walk");
        if (pandaWalkClip != null) walkState.motion = pandaWalkClip;
        walkState.writeDefaultValues = true;

        var talkState = pandaStateMachine.AddState("Talk");
        if (pandaTalkClip != null) talkState.motion = pandaTalkClip;
        talkState.writeDefaultValues = true;

        pandaStateMachine.defaultState = idleState;

        // 添加参数（与 MayaDialogueTrigger 的 walkTrigger=isW、idleTrigger=isId 对应）
        pandaController.AddParameter("isId", AnimatorControllerParameterType.Bool);
        pandaController.AddParameter("isW", AnimatorControllerParameterType.Bool);
        pandaController.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
        pandaController.AddParameter("Speed", AnimatorControllerParameterType.Float);
        pandaController.parameters[0].defaultBool = true;

        // 创建过渡
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isW");
        idleToWalk.duration = 0.25f;
        idleToWalk.hasExitTime = false;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isId");
        walkToIdle.duration = 0.25f;
        walkToIdle.hasExitTime = false;

        var idleToTalk = idleState.AddTransition(talkState);
        idleToTalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsTalking");
        idleToTalk.duration = 0.25f;
        idleToTalk.hasExitTime = false;

        var talkToIdle = talkState.AddTransition(idleState);
        talkToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isId");
        talkToIdle.duration = 0.25f;
        talkToIdle.hasExitTime = false;

        var walkToTalk = walkState.AddTransition(talkState);
        walkToTalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsTalking");
        walkToTalk.duration = 0.25f;
        walkToTalk.hasExitTime = false;

        EditorUtility.SetDirty(pandaController);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 胖胖完整控制器创建成功: {pandaControllerPath}");
    }

    static void CreateNoyaController()
    {
        string noyaFolder = "Assets/Resources/roles/诺亚";

        AnimationClip noyaClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            noyaFolder + "/Meshy_AI_诺亚_biped_Animation_Confused_Scratch_withSkin.fbx");
        if (noyaClip == null)
        {
            Debug.LogError("❌ 诺亚动画加载失败！");
            LogClipsInFBX("Confused_Scratch");
            return;
        }
        Debug.Log($"✅ 诺亚动画: {noyaClip.name} (长度: {noyaClip.length:F2}秒)");

        string noyaControllerPath = noyaFolder + "/NoyaController_FromScript.controller";
        if (File.Exists(noyaControllerPath))
        {
            AssetDatabase.DeleteAsset(noyaControllerPath);
            AssetDatabase.Refresh();
        }

        var noyaController = AnimatorController.CreateAnimatorControllerAtPath(noyaControllerPath);
        var noyaLayer = noyaController.layers[0];
        var noyaStateMachine = noyaLayer.stateMachine;

        noyaStateMachine.defaultState = null;

        var idleState = noyaStateMachine.AddState("Idle");
        idleState.motion = noyaClip;
        idleState.writeDefaultValues = true;

        var walkState = noyaStateMachine.AddState("Walk");
        walkState.motion = noyaClip;
        walkState.writeDefaultValues = true;

        var talkState = noyaStateMachine.AddState("Talk");
        talkState.motion = noyaClip;
        talkState.writeDefaultValues = true;

        noyaStateMachine.defaultState = idleState;

        noyaController.AddParameter("isId", AnimatorControllerParameterType.Bool);
        noyaController.AddParameter("isW", AnimatorControllerParameterType.Bool);
        noyaController.AddParameter("IsTalking", AnimatorControllerParameterType.Bool);
        noyaController.AddParameter("Speed", AnimatorControllerParameterType.Float);
        noyaController.parameters[0].defaultBool = true;

        var i2w = idleState.AddTransition(walkState);
        i2w.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isW");
        i2w.duration = 0.25f;

        var w2i = walkState.AddTransition(idleState);
        w2i.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isId");
        w2i.duration = 0.25f;

        var i2t = idleState.AddTransition(talkState);
        i2t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsTalking");
        i2t.duration = 0.25f;

        var t2i = talkState.AddTransition(idleState);
        t2i.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isId");
        t2i.duration = 0.25f;

        var w2t = walkState.AddTransition(talkState);
        w2t.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsTalking");
        w2t.duration = 0.25f;

        EditorUtility.SetDirty(noyaController);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 诺亚完整控制器创建成功: {noyaControllerPath}");
    }

    static void LogClipsInFBX(string searchTerm)
    {
        string[] guids = AssetDatabase.FindAssets($"{searchTerm} t:AnimationClip");
        Debug.Log($"搜索 '{searchTerm}' 动画片段，找到 {guids.Length} 个结果");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"  - {path}");
        }
    }
}
