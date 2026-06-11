using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement2 : MonoBehaviour
{
    private static Dictionary<Animator, float> noJudgeUntilTimes = new Dictionary<Animator, float>();
    bool is_rolePlay;
    bool is_tumblePlay;
    public float LowSpeed = 1.5f;
    public float UpSpeed = 2f;
    Transform playerTransform;
    Vector3 playerPosition;
    Animator playerAnimator;
    public GameObject groundcheck;
    public float jumpForce = 4f;
    Rigidbody player_rb;
    bool isLand;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool wasGrounded;
    public float maxJumpForceMultiplier = 2f;
    public float maxChargeTime = 0.5f;
    private bool isChargingJump = false;
    private float chargeStartTime = 0f;
    private bool canDoubleJump = true;
    // Start is called before the first frame update
    private void Awake()
    {
        playerTransform = this.transform;
        playerAnimator = GetComponentInChildren<Animator>();
        player_rb = GetComponent<Rigidbody>();
        groundLayer = LayerMask.GetMask("Ground");


    }
    void Start()
    {
        playerPosition = transform.position;
        wasGrounded = true;
    }

    // Update is called once per frame
    void Update()
    {
        IsGroundedLand();
        Movement();
        HandleJumpInput();
        playerAnimator.SetBool("IsGrounded", isLand);
        if (isLand && !wasGrounded)
        {
            playerAnimator.SetTrigger("Land");
        }
        wasGrounded = isLand;

    }
    void Movement()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? UpSpeed : LowSpeed;
        playerAnimator.SetBool("IsSpeedUp", Input.GetKey(KeyCode.LeftShift));


        Vector3 move = Vector3.left * speed;
        player_rb.velocity = new Vector3(move.x, player_rb.velocity.y, move.z);
        if (isLand)
        {
            playerAnimator.SetBool("IsSpeedUp", Input.GetKey(KeyCode.LeftShift));
        }
        else
        {
            playerAnimator.SetBool("IsSpeedUp", false);
        }
    }
    /*void Jump()
    {
        if(Input.GetKeyDown(KeyCode.Space)&&isLand)
        {   isLand=false;
            playerAnimator.SetTrigger("Jump");
            player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
        }
    }*/
    void IsGroundedLand()
    {
        bool wasLand = isLand;
        isLand = Physics.CheckSphere(groundcheck.transform.position, groundCheckRadius, groundLayer);
        if (isLand && !wasLand)
        {
            canDoubleJump = true;
        }
    }
    void HandleJumpInput()
    {

        if (!isLand && canDoubleJump && Input.GetKeyDown(KeyCode.Space))
        {

            if (isChargingJump) CancelCharge();


            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            playerAnimator.SetTrigger("Jump");
            canDoubleJump = false;
            return;
        }

        if (isLand && !isChargingJump && Input.GetKeyDown(KeyCode.Space))
        {
            isChargingJump = true;
            chargeStartTime = Time.time;
        }

        if (isChargingJump && !isLand)
        {
            CancelCharge();
        }

        if (isChargingJump && Input.GetKeyUp(KeyCode.Space))
        {
            float chargeDuration = Time.time - chargeStartTime;
            float t = Mathf.Clamp01(chargeDuration / maxChargeTime);
            float finalJumpForce = Mathf.Lerp(jumpForce, jumpForce * maxJumpForceMultiplier, t);
            player_rb.velocity = new Vector3(player_rb.velocity.x, 0f, player_rb.velocity.z);
            player_rb.AddForce(Vector3.up * finalJumpForce, ForceMode.Impulse);

            playerAnimator.SetTrigger("Jump");

            CancelCharge();
        }
    }


    void CancelCharge()
    {
        isChargingJump = false;

    }
    public static void JudgeCanrole(Animator playerAnimator, float cooldownAfterRoll)
    {
        if (playerAnimator == null) return;

        bool isTumblePlay = playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Jump_tumble");
        float currentTime = Time.time;


        if (!noJudgeUntilTimes.ContainsKey(playerAnimator))
            noJudgeUntilTimes[playerAnimator] = 0f;

        if (isTumblePlay)
        {

            noJudgeUntilTimes[playerAnimator] = currentTime + cooldownAfterRoll;
            playerAnimator.SetBool("Canrole", false);
        }
        else
        {

            if (currentTime < noJudgeUntilTimes[playerAnimator])
            {

                return;
            }
            else
            {

                playerAnimator.SetBool("Canrole", true);
            }
        }
    }


}
