using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BegainControll : MonoBehaviour
{   
     public AudioClip audioClip;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void LoadScene()
    { 
        UnityEngine.SceneManagement.SceneManager.LoadScene("level1");
    }
    public void ExitGame()
    {
#if UNITY_EDITOR
        // 在Unity编辑器中停止运行
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 在打包后的游戏中退出应用程序
            Application.Quit();
#endif
    }

}
