using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideTargetZone : MonoBehaviour
{
    [Header("目标箭头")]
    public Animator arrowAnimator;

    [Header("玩家标签")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("有东西进入了触发区：" + other.name + "，Tag：" + other.tag);

        if (other.CompareTag(playerTag))
        {
            arrowAnimator.SetBool("IsTargetReached", true);
            Debug.Log(" 玩家进入，触发隐藏");
        }
    }


}