using UnityEngine;
using System.Collections;

public class TargetSpawner : MonoBehaviour
{
    [Header("目标Prefab")]
    public GameObject cubePrefab;

    [Header("生成点")]
    public Transform[] spawnPoints;

    [Header("总目标数量")]
    public int totalTargetCount = 5;

    [Header("下一个目标生成延迟")]
    public float nextSpawnDelay = 2f;

    // 当前已生成数量
    private int currentCount = 0;

    // 当前目标
    private GameObject currentTarget;

    // 游戏是否结束
    public bool IsGameFinished { get; private set; }

    // 开始游戏
    public void StartGame()
    {
        currentCount = 0;

        IsGameFinished = false;

        SpawnNextTarget();
    }

    // 生成下一个目标
    public void SpawnNextTarget()
    {
        // 达到总数量
        if (currentCount >= totalTargetCount)
        {
            GameFinished();
            return;
        }

        int index =
            Random.Range(0, spawnPoints.Length);

        currentTarget = Instantiate(
            cubePrefab,
            spawnPoints[index].position,
            Quaternion.identity
        );

        currentCount++;

        Debug.Log("生成目标：" + currentCount);
    }

    // 当前目标被消除
    public void OnTargetDestroyed()
    {
        // 开始延迟生成
        StartCoroutine(SpawnNextAfterDelay());
    }

    // 延迟生成协程
    IEnumerator SpawnNextAfterDelay()
    {
        Debug.Log("等待生成下一个目标");

        // 等待几秒
        yield return new WaitForSeconds(nextSpawnDelay);

        // 生成下一个
        SpawnNextTarget();
    }

    // 游戏结束
    void GameFinished()
    {
        IsGameFinished = true;

        Debug.Log("小游戏结束");
    }
}