using UnityEngine;

public class PermanentFloorFrame : MonoBehaviour
{
    [Header("边框设置")]
    public Color frameColor = new Color(1f, 0.5f, 0f);
    public Vector3 frameSize = new Vector3(2f, 0.1f, 2f);
    public float blinkSpeed = 2f;

    [Header("游戏刷新区域（玩家前方）")]
    public Transform playerTransform;
    public float minDistanceFromPlayer = 2f;
    public float maxDistanceFromPlayer = 7f;
    public float lateralRandomRange = 2.5f;
    public float heightOffset = 0.01f;

    [Header("内部引用")]
    public Material frameMaterial;

    private BoxCollider triggerCollider;
    private Material runtimeMaterial;
    private bool isGameStarted = false;

    void Start()
    {
        if (transform.childCount == 0)
            BuildFrameStructure();

        EnsureTriggerCollider();
        InitializeBlinkEffect();
        gameObject.SetActive(false);

        // 自动找玩家
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    void Update()
    {
        UpdateBlinkEffect();
    }

    // 第一次扔球启动
    public void StartGameAndShowFrame()
    {
        if (isGameStarted) return;

        isGameStarted = true;
        gameObject.SetActive(true);
        RandomizePosition();
    }

    // 核心：球进入框内 → 计分 → 刷新位置
    private void OnTriggerEnter(Collider other)
    {
        if (!isGameStarted) return;

        if (other.CompareTag("Ball"))
        {
            Destroy(other.gameObject);
            RandomizePosition();

            // 通知 GameManager 计分
            if (KickBallGameManager.Instance != null)
            {
                KickBallGameManager.Instance.OnBallScored();
            }
        }
    }

    // 🔥 玩家前方随机刷新（你要的功能）
    public void RandomizePosition()
    {
        if (playerTransform == null)
        {
            FallbackRandomPosition();
            return;
        }

        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        float randomDist = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
        Vector3 basePos = playerTransform.position + playerForward * randomDist;

        Vector3 right = Vector3.Cross(playerForward, Vector3.up);
        float offsetX = Random.Range(-lateralRandomRange, lateralRandomRange);
        basePos += right * offsetX;

        transform.position = new Vector3(basePos.x, heightOffset, basePos.z);
    }

    // 找不到玩家时的备用逻辑
    private void FallbackRandomPosition()
    {
        transform.position = new Vector3(
            Random.Range(-3, 3),
            heightOffset,
            Random.Range(1, 5)
        );
    }

    // ------------------------------
    // 以下是边框渲染逻辑（不用动）
    // ------------------------------
    [ContextMenu("构建边框结构")]
    public void BuildFrameStructure()
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        if (frameMaterial == null)
        {
            runtimeMaterial = new Material(Shader.Find("Standard"));
            runtimeMaterial.color = frameColor;
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor("_EmissionColor", frameColor * 0.5f);
            frameMaterial = runtimeMaterial;
        }

        float hx = frameSize.x / 2;
        float hy = frameSize.y / 2;
        float hz = frameSize.z / 2;

        CreateCube(new Vector3(0, hy, hz), new Vector3(frameSize.x, frameSize.y, 0.1f));
        CreateCube(new Vector3(0, hy, -hz), new Vector3(frameSize.x, frameSize.y, 0.1f));
        CreateCube(new Vector3(hx, hy, 0), new Vector3(0.1f, frameSize.y, frameSize.z));
        CreateCube(new Vector3(-hx, hy, 0), new Vector3(0.1f, frameSize.y, frameSize.z));
    }

    void CreateCube(Vector3 localPos, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Frame";
        cube.transform.SetParent(transform);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = scale;
        DestroyImmediate(cube.GetComponent<Collider>());
        cube.GetComponent<Renderer>().material = frameMaterial;
    }

    void EnsureTriggerCollider()
    {
        triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<BoxCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(frameSize.x, 1f, frameSize.z);
        triggerCollider.center = new Vector3(0, 0.5f, 0);
    }

    void InitializeBlinkEffect()
    {
        if (frameMaterial != null)
        {
            runtimeMaterial = new Material(frameMaterial);
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.material = runtimeMaterial;
        }
    }

    void UpdateBlinkEffect()
    {
        Material mat = runtimeMaterial ?? frameMaterial;
        if (mat != null)
        {
            float a = Mathf.PingPong(Time.time * blinkSpeed, 1);
            Color c = frameColor;
            c.a = 0.3f + a * 0.7f;
            mat.color = c;
            mat.SetColor("_EmissionColor", c * 0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 f = playerTransform.forward; f.y = 0; f.Normalize();
            Gizmos.DrawLine(playerTransform.position, playerTransform.position + f * maxDistanceFromPlayer);
            Gizmos.DrawWireSphere(playerTransform.position + f * minDistanceFromPlayer, lateralRandomRange);
            Gizmos.DrawWireSphere(playerTransform.position + f * maxDistanceFromPlayer, lateralRandomRange);
        }
    }
}