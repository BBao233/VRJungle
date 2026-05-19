using UnityEngine;
using System.Collections.Generic; // 你的脚本里用了 List，也需要引入这个

public class TargetGenerator : MonoBehaviour
{
    public GameObject targetPrefab;
    public Transform[] spawnPoints;

    // ?? 新增：独立控制总共需要打多少个目标
    public int totalTargetsNeeded = 5;
    private int targetsSpawnedCount = 0;
    private int currentPointIndex = 0;
    private GameObject currentTarget;

    public void StartSequence()
    {
        ClearAll();
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        targetsSpawnedCount = 0;
        currentPointIndex = 0;
        SpawnAtCurrentPoint();
    }

    public bool SpawnNextTarget()
    {
        if (currentTarget != null) { Destroy(currentTarget); currentTarget = null; }

        targetsSpawnedCount++;
        // ?? 判断是否达到总目标数，而不是点位数量
        if (targetsSpawnedCount >= totalTargetsNeeded) return false;

        // 点位循环复用：0->1->0->1...
        currentPointIndex = (currentPointIndex + 1) % spawnPoints.Length;
        SpawnAtCurrentPoint();
        return true;
    }

    private void SpawnAtCurrentPoint()
    {
        currentTarget = Instantiate(targetPrefab, spawnPoints[currentPointIndex].position, spawnPoints[currentPointIndex].rotation);
        currentTarget.SetActive(true);
    }

    public void ClearAll()
    {
        if (currentTarget != null) Destroy(currentTarget);
        currentTarget = null;
    }
}
