using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class Death : MonoBehaviour
{
    public float targetHeight=-10f;
    public GameObject PlayerPo;
    PlayerMovement_run playerMove;
    CaptureMaterial capture_material;
    public Canvas deathCanvas;
    SkinnedMeshRenderer player1Render;
    SkinnedMeshRenderer playerbodyRender;
    Vector3 playerPo;
    private bool isDead = false;
    // Start is called before the first frame update
    private void Awake()
    {
        player1Render = GameObject.Find("body2").GetComponent<SkinnedMeshRenderer>();
        playerbodyRender = GameObject.Find("body1").GetComponent<SkinnedMeshRenderer>();
        if (deathCanvas != null)
        { deathCanvas.enabled = false; }
        playerMove=GetComponent<PlayerMovement_run>();
        capture_material=GetComponent<CaptureMaterial>();
        playerPo = PlayerPo.transform.position;
    }

    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
        playerPo = PlayerPo.transform.position;
        if (playerPo.y<=targetHeight)
        {
            Die();
        }
    }
    void OnCollisionEnter(Collision collision)
    {
       
        if (isDead) return;
        MeshRenderer collidedRenderer = collision.gameObject.GetComponent<MeshRenderer>();
        if (collision.gameObject.CompareTag("Ground"))
        {
            
            if (player1Render.sharedMaterial!=collidedRenderer.sharedMaterial)
            {
                Die();
            }
        }
    }
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        
        if (deathCanvas != null)
            deathCanvas.enabled = true;

        
        Time.timeScale = 0f;

        playerMove.enabled = false;
        capture_material.enabled = false;
        TimeCounter.StopTimingOnDeath();

    }
    void OnCollisionStay(Collision collision)
    {

        if (isDead) return;
        MeshRenderer collidedRenderer = collision.gameObject.GetComponent<MeshRenderer>();
        if (collision.gameObject.CompareTag("Ground"))
        {
            
            if (player1Render.sharedMaterial != collidedRenderer.sharedMaterial)
            {
                Die();
            }
        }
    }
}
