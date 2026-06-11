using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Death : MonoBehaviour
{
    public float targetHeight = -10f;
    public GameObject PlayerPo;
    PlayerMovement_runhhf playerMove;
    CaptureMaterial capture_material;
    public Canvas deathCanvas;
    SkinnedMeshRenderer player1Render;
    SkinnedMeshRenderer playerbodyRender;
    Vector3 playerPo;
    private bool isDead = false;

    private void Awake()
    {
        player1Render = GameObject.Find("body2").GetComponent<SkinnedMeshRenderer>();
        playerbodyRender = GameObject.Find("body1").GetComponent<SkinnedMeshRenderer>();
        if (deathCanvas != null)
        { deathCanvas.enabled = false; }
        playerMove = GetComponent<PlayerMovement_runhhf>();
        capture_material = GetComponent<CaptureMaterial>();
        playerPo = PlayerPo.transform.position;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        playerPo = PlayerPo.transform.position;
        // 仅保留：玩家高度低于目标值 → 死亡
        if (playerPo.y <= targetHeight)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // 显示死亡界面
        if (deathCanvas != null)
            deathCanvas.enabled = true;

        // 暂停游戏
        Time.timeScale = 0f;

        // 禁用玩家控制
        playerMove.enabled = false;
        capture_material.enabled = false;
        TimeCounter.StopTimingOnDeath();

        // 2秒后重启场景
        StartCoroutine(ReloadSceneAfterDelay(2f));
    }

    // 延时重载场景协程
    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}