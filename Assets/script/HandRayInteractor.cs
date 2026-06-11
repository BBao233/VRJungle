using UnityEngine;

public class HandGestureDestroy : MonoBehaviour
{
    [Header("目标设置")]
    public string targetTag = "TargetObject";
    public TargetSpawner spawner;

    private bool isGestureActive = false;

    //  手势开始：激活激光 + 立即销毁目标
    public void OnPoseStart()
    {
        isGestureActive = true;
        DestroyTarget();
    }

    // ?? 手势结束
    public void OnPoseEnd()
    {
        isGestureActive = false;
    }

    void DestroyTarget()
    {
        GameObject target = GameObject.FindGameObjectWithTag(targetTag);
        if (target != null)
        {
            target.SetActive(false);
            Debug.Log($"?? 手势触发，目标已消除: {target.name}");
            spawner?.OnTargetDestroyed();
        }
        else
        {
            Debug.Log("?? 场景中未找到目标物体");
        }
    }
}
