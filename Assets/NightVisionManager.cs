using UnityEngine;
using System.Collections;

public class NightVisionManager : MonoBehaviour
{
    [Header("夜视UI")] public GameObject greenOverlay;
    [Header("提示UI")] public GameObject nightVisionHintUI;
    public GameObject shootingHintUI;
    [Header("关闭提示UI")] public GameObject thumbsUpHintUI;
    [Header("小游戏")] public TargetSpawner spawner;
    [Header("时间")] public float showShootingHintDelay = 3f;
    public float shootingHintDuration = 2f;

    // ?? 新增：拖入 RightHandLaser 物体
    public GameObject rightHandLaserObj;

    private Coroutine flowCoroutine;
    private bool waitingForCloseGesture = false;

    public void TurnOn()
    {
        if (flowCoroutine != null) StopCoroutine(flowCoroutine);
        flowCoroutine = StartCoroutine(NightVisionFlowSequence());

        //  开启夜视时，激活激光物体（但默认不显示，等手势触发）
        if (rightHandLaserObj != null) rightHandLaserObj.SetActive(true);
    }

    public void TurnOff()
    {
        if (greenOverlay != null) greenOverlay.SetActive(false);
        if (nightVisionHintUI != null) nightVisionHintUI.SetActive(false);
        if (shootingHintUI != null) shootingHintUI.SetActive(false);
        if (thumbsUpHintUI != null) thumbsUpHintUI.SetActive(false);
        if (flowCoroutine != null) StopCoroutine(flowCoroutine);

        // ?? 关闭夜视时，彻底禁用激光物体
        if (rightHandLaserObj != null) rightHandLaserObj.SetActive(false);

        waitingForCloseGesture = false;
        Debug.Log("夜视仪已关闭");
    }

    private IEnumerator NightVisionFlowSequence()
    {
        greenOverlay.SetActive(true);
        nightVisionHintUI.SetActive(false);

        yield return new WaitForSeconds(showShootingHintDelay);
        shootingHintUI.SetActive(true);
        yield return new WaitForSeconds(shootingHintDuration);
        shootingHintUI.SetActive(false);

        spawner.StartGame();
        yield return new WaitUntil(() => spawner.IsGameFinished);

        thumbsUpHintUI.SetActive(true);
        waitingForCloseGesture = true;
        Debug.Log("请比出大拇指关闭夜视仪");
        flowCoroutine = null;
    }

    public void OnThumbsUpGesture()
    {
        if (!waitingForCloseGesture) return;
        Debug.Log("检测到关闭手势");
        TurnOff();
    }
}
