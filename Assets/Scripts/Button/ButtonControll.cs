using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonControll : MonoBehaviour
{  
    public GameObject Player;
    PlayerMovement playerMove;
    CaptureMaterial capture_material;

    // ÓÎÏ·×´Ì¬Ã¶¾Ù
    private enum GameState
    {
        Menu,       
        Playing,    
        Paused      
    }
    private GameState currentState = GameState.Menu;

    private void Awake()
    {
        playerMove = Player.GetComponent<PlayerMovement>();
        capture_material = Player.GetComponent<CaptureMaterial>();
        
    }

    void Start()
    {
       
        Time.timeScale = 1f;
        playerMove.enabled = true;
        capture_material.enabled = true;
        currentState = GameState.Playing;
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                
                ContinueGame();
                
            }
        }
    }

    

    
    public void ContinueGame()
    {   
        if (currentState == GameState.Paused)
        {
            
            Time.timeScale = 1f;
            capture_material.enabled = true;
            playerMove.enabled = true;
            currentState = GameState.Playing;
            TimeCounter.ResumeTiming();
        }
    }

    
    public void PauseGame()
    {
        
        if (currentState == GameState.Playing)
        {
            Time.timeScale = 0f;
            capture_material.enabled = false;
            playerMove.enabled = false;
            currentState = GameState.Paused;
            TimeCounter.PauseTiming();
        }
    }

    
    public void RestartGame()
    {
        
        Time.timeScale = 1f;
        TimeCounter.ResetTiming();
        SceneManager.LoadScene(1);
    }
    public void ExitGame()
    {   Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }


}