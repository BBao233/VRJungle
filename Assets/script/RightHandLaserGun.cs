using UnityEngine;

public class RightHandLaserGun : MonoBehaviour
{
    [Header("?? Pico 追踪骨骼 (必填)")]
    [Tooltip("必须拖入 PXR_Hand 驱动的骨骼，如 right_index_tip 或 right_wrist")]
    public Transform trackedBone;

    [Header("?? 激光枪模型")]
    public GameObject laserModel;

    [Header("?? 握持偏移微调")]
    public Vector3 holdOffset = new Vector3(0, 0, 0.15f);
    public Vector3 holdRotation = new Vector3(0, 0, 0);

    private bool isActive = false;

    /// <summary>绑定到 PXR_HandPose 的 On Pose Enter</summary>
    public void OnGestureStart()
    {
        if (isActive || trackedBone == null || laserModel == null) return;

        // 1. 激活模型
        laserModel.SetActive(true);

        // 2. ?? 运行时绑定到被 SDK 驱动的骨骼 (false 保留局部坐标)
        laserModel.transform.SetParent(trackedBone, false);

        // 3. 应用握持偏移
        laserModel.transform.localPosition = holdOffset;
        laserModel.transform.localRotation = Quaternion.Euler(holdRotation);

        isActive = true;
        Debug.Log("[激光枪] 已绑定追踪骨骼，开始实时跟随。");
    }

    /// <summary>绑定到 PXR_HandPose 的 On Pose Exit</summary>
    public void OnGestureEnd()
    {
        if (!isActive) return;

        // 1. 隐藏模型
        laserModel.SetActive(false);

        // 2. 解除父子级，防止下次激活时坐标累积错乱
        laserModel.transform.SetParent(null);

        isActive = false;
    }
}
