using UnityEngine;

public class HandLaserController : MonoBehaviour
{
    public LineRenderer laserLine;
    public float maxDistance = 15f;
    public LayerMask hitLayer;
    private bool isActive = false;

    void Start()
    {
        if (laserLine == null) laserLine = GetComponent<LineRenderer>();
        laserLine.enabled = false;
    }

    public void EnableLaser() => isActive = true;
    public void DisableLaser() => isActive = false;

    void Update()
    {
        if (!isActive || laserLine == null) return;

        // 每帧更新起点和终点，实现“持续跟随”
        laserLine.SetPosition(0, Vector3.zero);

        Vector3 forward = transform.forward;
        if (Physics.Raycast(transform.position, forward, out RaycastHit hit, maxDistance, hitLayer))
        {
            laserLine.SetPosition(1, transform.InverseTransformPoint(hit.point));
        }
        else
        {
            laserLine.SetPosition(1, new Vector3(0, 0, maxDistance));
        }
    }
}
