using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;
public class PassThroughManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PXR_Manager.EnableVideoSeeThrough = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
