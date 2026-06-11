using UnityEngine;

/// <summary>
/// 3D引导箭头（信标式）—— 在目标位置升起一个发光的箭头
///
/// 功能：
/// - 可设置目标位置，箭头自动移动到目标上方
/// - 带上下浮动动画
/// - 缓慢旋转，方便VR中从各个角度看到
/// - Show/Hide 渐显渐隐
/// - 使用Unity原生圆柱+圆锥构建，无需额外资源
/// </summary>
public class GuideArrow : MonoBehaviour
{
    [Header("=== 位置设置 ===")]
    [Tooltip("箭头指向的目标位置（地面坐标）")]
    public Vector3 targetPosition;
    [Tooltip("箭头悬浮高度（距地面）")]
    public float hoverHeight = 2.5f;
    [Tooltip("是否自动调整Y轴到Ground")]
    public bool autoGroundHeight = true;
    [Tooltip("地面检测层级")]
    public LayerMask groundLayer = 0;

    [Header("=== 动画设置 ===")]
    [Tooltip("上下浮动速度")]
    public float bobSpeed = 1.5f;
    [Tooltip("上下浮动幅度")]
    public float bobHeight = 0.4f;
    [Tooltip("旋转速度（度/秒）")]
    public float rotateSpeed = 30f;
    [Tooltip("渐显/渐隐时间")]
    public float fadeDuration = 0.5f;

    [Header("=== 外观 ===")]
    [Tooltip("箭头颜色")]
    public Color arrowColor = new Color(1f, 0.8f, 0f); // 金色
    [Tooltip("箭头大小")]
    public float arrowScale = 1f;

    [Header("=== 调试 ===")]
    public bool debugMode = true;

    private GameObject _arrowRoot;
    private Renderer[] _renderers;
    private Material _arrowMaterial;
#pragma warning disable 0414
    private bool _isVisible = true;
#pragma warning restore 0414
    private float _currentFade = 1f;
    private Coroutine _fadeCoroutine;
    private float _baseY;
    private bool _isInitialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (_isInitialized) return;

        // 创建箭头视觉
        BuildArrowVisual();

        // 设置初始位置
        if (targetPosition != Vector3.zero)
        {
            UpdatePosition();
        }

        _isInitialized = true;

        if (debugMode)
            Debug.Log("[引导箭头] ✅ 初始化完成");
    }

    /// <summary>
    /// 使用Unity原生几何体构建箭头
    /// </summary>
    void BuildArrowVisual()
    {
        _arrowRoot = new GameObject("Arrow_Visual");
        _arrowRoot.transform.SetParent(transform, false);

        // === 杆（圆柱） ===
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Arrow_Pole";
        pole.transform.SetParent(_arrowRoot.transform, false);
        pole.transform.localScale = new Vector3(0.08f * arrowScale, 0.5f * arrowScale, 0.08f * arrowScale);
        pole.transform.localPosition = new Vector3(0, 0.5f * arrowScale, 0);

        // === 尖头（圆锥） ===
        // 用Cylinder压成圆锥（顶部缩为0）
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = "Arrow_Cone";
        cone.transform.SetParent(_arrowRoot.transform, false);
        cone.transform.localScale = new Vector3(0.25f * arrowScale, 0.25f * arrowScale, 0.25f * arrowScale);
        cone.transform.localPosition = new Vector3(0, 1.0f * arrowScale, 0);

        // 将圆柱顶部顶点收拢成圆锥
        Mesh coneMesh = cone.GetComponent<MeshFilter>().mesh;
        Vector3[] verts = coneMesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            // 顶部（y ≈ 0.5）的所有顶点收缩到中心
            if (verts[i].y > 0.3f)
            {
                verts[i].x = 0;
                verts[i].z = 0;
                verts[i].y = 0.5f;
            }
        }
        coneMesh.vertices = verts;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();

        // === 底圈（环） ===
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Arrow_Ring";
        ring.transform.SetParent(_arrowRoot.transform, false);
        ring.transform.localScale = new Vector3(0.35f * arrowScale, 0.03f * arrowScale, 0.35f * arrowScale);
        ring.transform.localPosition = new Vector3(0, 0.02f * arrowScale, 0);

        // === 材质 ===
        _renderers = _arrowRoot.GetComponentsInChildren<Renderer>();
        // 创建发光材质
        Shader shader = Shader.Find("Standard");
        if (shader != null)
        {
            _arrowMaterial = new Material(shader);
            _arrowMaterial.color = arrowColor;
            _arrowMaterial.SetFloat("_Metallic", 0.3f);
            _arrowMaterial.SetFloat("_Glossiness", 0.6f);
            _arrowMaterial.EnableKeyword("_EMISSION");
            _arrowMaterial.SetColor("_EmissionColor", arrowColor * 0.8f);

            foreach (var r in _renderers)
            {
                r.material = _arrowMaterial;
            }
        }

        // 初始不可见
        _arrowRoot.SetActive(false);

        if (debugMode)
            Debug.Log("[引导箭头] 🔨 箭头视觉已构建");
    }

    void Update()
    {
        if (_arrowRoot == null || !_arrowRoot.activeSelf) return;

        // 上下浮动
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        Vector3 pos = _arrowRoot.transform.localPosition;
        pos.y = _baseY + bob;
        _arrowRoot.transform.localPosition = pos;

        // 缓慢旋转
        _arrowRoot.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
    }

    /// <summary>
    /// 设置目标位置
    /// </summary>
    public void SetTarget(Vector3 position)
    {
        targetPosition = position;
        if (_isInitialized)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 更新箭头位置到目标上方
    /// </summary>
    void UpdatePosition()
    {
        Vector3 pos = targetPosition;

        // 自动检测地面高度
        if (autoGroundHeight)
        {
            float groundY = GetGroundHeight(pos);
            pos.y = groundY;
        }

        transform.position = pos;

        // 箭头根物体在本地坐标中抬高
        if (_arrowRoot != null)
        {
            _arrowRoot.transform.localPosition = new Vector3(0, 0, 0);
            _baseY = hoverHeight;
            _arrowRoot.transform.localPosition = new Vector3(0, _baseY, 0);
        }
    }

    /// <summary>
    /// 显示箭头（渐显）
    /// </summary>
    public void Show()
    {
        if (!_isInitialized) Initialize();

        _isVisible = true;

        if (_arrowRoot != null)
        {
            _arrowRoot.SetActive(true);
            // 更新位置
            UpdatePosition();
        }

        // 渐显动画
        StartFade(1f);

        if (debugMode)
            Debug.Log($"[引导箭头] 👁 显示 → {targetPosition}");
    }

    /// <summary>
    /// 隐藏箭头（渐隐）
    /// </summary>
    public void Hide()
    {
        _isVisible = false;

        StartFade(0f);

        if (debugMode)
            Debug.Log("[引导箭头] 👁 隐藏");
    }

    /// <summary>
    /// 渐显/渐隐
    /// </summary>
    void StartFade(float targetAlpha)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha));
    }

    System.Collections.IEnumerator FadeCoroutine(float targetAlpha)
    {
        float startAlpha = _currentFade;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _currentFade = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            ApplyAlpha(_currentFade);
            yield return null;
        }

        _currentFade = targetAlpha;
        ApplyAlpha(_currentFade);

        // 完全透明时隐藏物体
        if (targetAlpha <= 0f && _arrowRoot != null)
        {
            _arrowRoot.SetActive(false);
        }

        _fadeCoroutine = null;
    }

    void ApplyAlpha(float alpha)
    {
        if (_arrowMaterial == null || _renderers == null) return;

        Color color = arrowColor;
        color.a = alpha;
        _arrowMaterial.color = color;

        // 发射光强度随alpha变化
        Color emission = arrowColor * (0.8f * alpha);
        _arrowMaterial.SetColor("_EmissionColor", emission);
    }

    float GetGroundHeight(Vector3 pos)
    {
        RaycastHit hit;
        Vector3 origin = new Vector3(pos.x, 100f, pos.z);
        bool hitSomething = groundLayer.value != 0
            ? Physics.Raycast(origin, Vector3.down, out hit, 200f, groundLayer)
            : Physics.Raycast(origin, Vector3.down, out hit, 200f);
        if (hitSomething) return hit.point.y;
        return pos.y;
    }

    void OnDestroy()
    {
        if (_arrowMaterial != null)
            Destroy(_arrowMaterial);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Vector3 pos = targetPosition;
            pos.y += hoverHeight;
            Gizmos.DrawWireSphere(pos, 0.5f);
            Gizmos.DrawLine(targetPosition, pos);

            // 箭头图标
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.ArrowHandleCap(0, pos, Quaternion.identity, 1f, EventType.Repaint);
        }
    }
#endif
}
