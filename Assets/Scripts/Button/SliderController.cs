using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    public GameObject sound_controll;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SliderMenue()
    {   if (sound_controll.activeSelf)
        {

            sound_controll.SetActive(false);
            return;
        }
        else
        { sound_controll.SetActive(true);
            return;
        }
    }
}
