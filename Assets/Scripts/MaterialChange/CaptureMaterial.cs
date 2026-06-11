using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureMaterial : MonoBehaviour
{
    public Material yellowMaterial;
    public Material pinkMaterial;
    public Material greenMaterial;
    SkinnedMeshRenderer player1Render;
    SkinnedMeshRenderer playerbodyRender;
    Animator changeColorAnimator;
    private float NoJudgeTime;

    private void Awake()
    {
        player1Render = GameObject.Find("body2").GetComponent<SkinnedMeshRenderer>();
        playerbodyRender = GameObject.Find("body1").GetComponent<SkinnedMeshRenderer>();
        player1Render.sharedMaterial = pinkMaterial;
        playerbodyRender.sharedMaterial = pinkMaterial;
        changeColorAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 【可选】保留键盘C键，方便测试
        if (Input.GetKeyDown(KeyCode.C))
        {
            TriggerColorChange();
        }
    }

    // -------------------- 手势事件：左手竖大拇指触发变色 --------------------
    public void OnLeftThumbsUp()
    {
        TriggerColorChange();
    }

    void TriggerColorChange()
    {
        if (changeColorAnimator != null)
        {
            PlayerMovement_runhhf.JudgeCanrole(changeColorAnimator, 0.5f);
            changeMaterial();
        }
    }

    void changeMaterial()
    {
        changeColorAnimator.SetTrigger("ChangeColor");

        if (player1Render.sharedMaterial == pinkMaterial)
        {
            player1Render.sharedMaterial = yellowMaterial;
            playerbodyRender.sharedMaterial = yellowMaterial;
        }
        else if (player1Render.sharedMaterial == yellowMaterial)
        {
            player1Render.sharedMaterial = greenMaterial;
            playerbodyRender.sharedMaterial = greenMaterial;
        }
        else
        {
            player1Render.sharedMaterial = pinkMaterial;
            playerbodyRender.sharedMaterial = pinkMaterial;
        }
    }
}