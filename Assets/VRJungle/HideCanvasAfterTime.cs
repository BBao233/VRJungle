using UnityEngine;

public class HideCanvasAfterTime : MonoBehaviour
{
    [Header("延迟时间（秒）")]
    public float delay = 3f; // 你可以在 Inspector 里改

    [Header("要隐藏的 Canvas")]
    public Canvas targetCanvas;

    void Start()
    {
        Invoke(nameof(HideCanvas), delay);
    }

    void HideCanvas()
    {
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(false);
        }
    }
}