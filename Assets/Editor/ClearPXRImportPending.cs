using UnityEditor;
using UnityEngine;

/// <summary>
/// 清除 PICO BuildingBlocks 的 ImportPending 标记
/// 防止运行时触发 GenerateXRHands() 导致报错
/// 使用: Tools > PICO > Clear Import Pending Flag
/// </summary>
public static class ClearPXRImportPending
{
    [MenuItem("Tools/PICO/Clear Import Pending Flag")]
    private static void Clear()
    {
        // 匹配 PXR_Utils 中 ImportPendingKey 的拼接方式:
        // ProjectName(项目文件夹名) + SceneName(当前场景名) + "PXR_BuildingBlocksXRHandTracking"
        string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));
        string sceneName = GetActiveSceneName();
        string key = projectName + sceneName + "PXR_BuildingBlocksXRHandTracking";

        if (EditorPrefs.HasKey(key))
        {
            EditorPrefs.DeleteKey(key);
            Debug.Log($"[清除标记] ✅ 已删除: {key}");
        }
        else
        {
            Debug.Log($"[清除标记] 未找到标记: {key}");

            // 如果精确匹配没找到，再尝试一些变体
            string[] possibleKeys = {
                "PXR_BuildingBlocksXRHandTracking",
                projectName + "PXR_BuildingBlocksXRHandTracking",
                sceneName + "PXR_BuildingBlocksXRHandTracking",
            };
            foreach (var k in possibleKeys)
            {
                if (EditorPrefs.HasKey(k))
                {
                    EditorPrefs.DeleteKey(k);
                    Debug.Log($"[清除标记] ✅ 已删除变体: {k}");
                }
            }
        }

        Debug.Log("[清除标记] 完成。请重新进入 Play Mode 测试。");
    }

    private static string GetActiveSceneName()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        return !string.IsNullOrEmpty(scene.name) ? scene.name : "";
    }
}
