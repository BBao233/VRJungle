using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraMovement : MonoBehaviour
{
    public GameObject Cam_Object;
    Vector3 cam_position;
    // Start is called before the first frame update
    private void Awake()
    {

        cam_position = Cam_Object.transform.position;
        this.transform.position = cam_position;
    }
    void Start()
    {  

        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        cam_position = Cam_Object.transform.position;
        this.transform.position = cam_position;
    }
}
