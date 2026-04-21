using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchSceneBySpace : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 自动加载下一个场景
            SceneManager.LoadScene("beforeriver");
        }
    }
}