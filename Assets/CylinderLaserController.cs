using UnityEngine;

public class CylinderLaserController : MonoBehaviour
{
    [Header("激光组件")]
    public Transform laserCylinder;
    public Transform wristBone;
    public Transform aimBone;

    [Header("参数")]
    public float maxLength = 15f;
    public float thickness = 0.03f;

    private bool isActive = false;

    void Start()
    {
        if (laserCylinder == null) laserCylinder = transform;
        laserCylinder.gameObject.SetActive(false);
    }

    public void EnableLaser()
    {
        isActive = true;
        laserCylinder.gameObject.SetActive(true);
    }

    public void DisableLaser()
    {
        isActive = false;
        laserCylinder.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        // 计算指向方向（手腕→食指）
        Vector3 forwardDir = (wristBone != null && aimBone != null)
            ? (aimBone.position - wristBone.position).normalized
            : transform.forward;

        // 更新视觉：固定长度，始终指向前方
        laserCylinder.rotation = Quaternion.LookRotation(forwardDir);
        laserCylinder.localScale = new Vector3(thickness, thickness, maxLength);

        // 起点从手腕向前偏移 0.1m，避免穿模手掌
        Vector3 origin = wristBone.position + forwardDir * 0.1f;
        laserCylinder.position = origin + forwardDir * (maxLength * 0.5f);
    }
}
