using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandRayInteractor : MonoBehaviour
{

    public Transform indexFingerTip; // 食指尖（关键）
    public float rayDistance = 10f;
    private bool isGestureActive = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isGestureActive) return;

        Ray ray = new Ray(indexFingerTip.position, indexFingerTip.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.green);

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            if (hit.collider.CompareTag("TargetObject"))
            {
                hit.collider.gameObject.SetActive(false);
            }
        }
    }

    // 给 PXR 手势调用
    public void OnPoseStart()
    {
        isGestureActive = true;
    }

    public void OnPoseEnd()
    {
        isGestureActive = false;
    }
}
