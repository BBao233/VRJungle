using UnityEngine;

public class FingerBeamCaster : MonoBehaviour
{
    [Header("?? 骨骼锚点")]
    public Transform wristAnchor;   // 右手腕
    public Transform indexAnchor;   // 食指关节

    [Header("?? 激光视觉参数")]
    public float beamLength = 6f;
    public float beamWidth = 0.025f;
    public Transform beamMesh;      // 圆柱体 Capsule

    private bool isBeamActive = false;

    public void IgniteBeam()
    {
        isBeamActive = true;
        if (beamMesh != null) beamMesh.gameObject.SetActive(true);
    }

    public void ExtinguishBeam()
    {
        isBeamActive = false;
        if (beamMesh != null) beamMesh.gameObject.SetActive(false);
    }

    void Update()
    {
        // 仅负责视觉跟随，无任何物理/射线检测
        if (isBeamActive && beamMesh != null && wristAnchor != null && indexAnchor != null)
        {
            Vector3 aimDir = (indexAnchor.position - wristAnchor.position).normalized;
            Vector3 startPos = wristAnchor.position + aimDir * 0.08f; // 起点前移防穿模手掌

            beamMesh.rotation = Quaternion.LookRotation(aimDir);
            beamMesh.localScale = new Vector3(beamWidth, beamWidth, beamLength);
            beamMesh.position = startPos + aimDir * (beamLength * 0.5f);
        }
    }
}
