using UnityEngine;

public class HandModelSwitcher : MonoBehaviour
{
    [Header("左手模型容器")]
    public GameObject leftDefaultContainer;
    public GameObject leftShootingContainer;

    [Header("右手模型容器")]
    public GameObject rightDefaultContainer;
    public GameObject rightShootingContainer;

    private bool isShootingPoseActive = false;

    // 绑定到 PXR_HandPose 的 On Pose Enter
    public void OnShootingPoseStart()
    {
        if (isShootingPoseActive) return;
        isShootingPoseActive = true;
        SwitchModel(true);
    }

    // 绑定到 PXR_HandPose 的 On Pose Exit
    public void OnShootingPoseEnd()
    {
        if (!isShootingPoseActive) return;
        isShootingPoseActive = false;
        SwitchModel(false);
    }

    private void SwitchModel(bool useShooting)
    {
        Toggle(leftDefaultContainer, leftShootingContainer, useShooting);
        Toggle(rightDefaultContainer, rightShootingContainer, useShooting);
    }

    private void Toggle(GameObject def, GameObject custom, bool showCustom)
    {
        if (def != null) def.SetActive(!showCustom);
        if (custom != null) custom.SetActive(showCustom);
    }
}
