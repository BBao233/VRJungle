using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeCounter : MonoBehaviour
{
    public GameObject player;
    public TextMeshProUGUI time_count;
    private static Death deathJudge;


    private static float totalTime = 0f;
    private static bool isTiming = false;
    private static bool isPaused = false;
    private static bool isDead = false;
    private static bool hasStarted = false;


    public static void PauseTiming()
    {
        if (!isDead && isTiming)
        {
            isPaused = true;
            isTiming = false;
        }
    }


    public static void ResumeTiming()
    {
        if (!isDead && !isTiming && isPaused)
        {
            isPaused = false;
            isTiming = true;
        }
    }


    public static void ResetTiming()
    {
        totalTime = 0f;
        isTiming = true;
        isPaused = false;
        isDead = false;
        hasStarted = true;
    }


    public static void StopTimingOnDeath()
    {
        if (!isDead)
        {
            isDead = true;
            isTiming = false;
            isPaused = false;
        }
    }



    private void Awake()
    {

        if (player != null && deathJudge == null)
            deathJudge = player.GetComponent<Death>();
    }

    private void Start()
    {

        if (!hasStarted && SceneManager.GetActiveScene().name == "level1")
        {
            ResetTiming();
        }


        UpdateDisplay();
    }

    private void Update()
    {

        if (isTiming && !isPaused && !isDead)
        {

            totalTime += Time.unscaledDeltaTime;
            UpdateDisplay();
        }
    }


    private void UpdateDisplay()
    {
        if (time_count == null) return;

        int minutes = Mathf.FloorToInt(totalTime / 60f);
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        time_count.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}