using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HideArrowState : StateMachineBehaviour
{
    private LineRenderer lineRenderer;
    private Renderer[] renderers;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        lineRenderer = animator.GetComponent<LineRenderer>();
        renderers = animator.GetComponentsInChildren<Renderer>(true);

        if (lineRenderer != null) lineRenderer.enabled = false;
        foreach (var r in renderers) r.enabled = false;
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (lineRenderer != null) lineRenderer.enabled = true;
        foreach (var r in renderers) r.enabled = true;
    }
}