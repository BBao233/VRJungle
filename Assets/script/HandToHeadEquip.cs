using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandToHeadEquip : MonoBehaviour
{
    public Transform rightHand;
    public Transform cameraTransform;
    public GameObject nightVisionEffect;

    private bool equipped = false;

    void Update()
    {
        if (equipped) return;

        // 1️⃣ 距离判断
        float distance = Vector3.Distance(rightHand.position, cameraTransform.position);

        if (distance < 0.3f)
        {
            // 2️⃣ 手掌方向判断
            Vector3 handForward = rightHand.forward;
            Vector3 toHead = (cameraTransform.position - rightHand.position).normalized;

            float dot = Vector3.Dot(handForward, toHead);

            if (dot > 0.7f)
            {
                Equip();
            }
        }
    }

    void Equip()
    {
        equipped = true;
        nightVisionEffect.SetActive(true);

        Debug.Log("夜视仪已佩戴");
    }
}
