//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class player: MonoBehaviour
//{
//    private Animator anime;
//    private Rigidbody2D rb;
//    public float speed;
//    public float jumpforce;
//    //public bool isjumping;
//    //public bool ismoving;
//    private int facingDir = 1;
//    private bool facingright = true;
//    private float xInput;
//    //[SerializeField] private float dashDuration;
//    //private float dashTime;
//    //[SerializeField] private float dashSpeed;
//    //[SerializeField] private float dashCooldown;
//    //private float dashCooldownTimer;

//    [SerializeField] private float groundcheckDistance;
//    [SerializeField] private LayerMask whatIsGround;
//    private bool IsGrounded;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody2D>();
//        anime = GetComponentInChildren<Animator>();
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        Movement();
//        CheckInput();
//        //AnimatorControllers();
//        groundCheck();
//        FlipController();
//    }


//    private void groundCheck()
//    {
//        IsGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundcheckDistance, whatIsGround);
//    }

//    private void CheckInput()
//    {
//        xInput = Input.GetAxisRaw("Horizontal");
//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            jump();
//        }
//        /*if (Input.GetKeyDown(KeyCode.L)&&dashCooldownTimer<0)
//        {
//            dashCooldownTimer=dashCooldown;
//            Dash();
//        }*/
//    }

//    private void Movement()
//    {
//        /*if(dashTime > 0)
//        {

//            rb.velocity = new Vector2(facingDir * dashSpeed, 0);
//            dashTime -= Time.deltaTime;
//        }*/
//        rb.velocity = new Vector2(xInput * speed, rb.velocity.y);
//        //dashCooldownTimer -= Time.deltaTime;
//    }

//    private void jump()
//    {
//        if (IsGrounded)
//        {
//            rb.velocity = new Vector2(rb.velocity.x, jumpforce);
//        }
//    }

//    /*private void AnimatorControllers()
//    {
//        anime.SetFloat("yvelocity", rb.velocity.y);
//        isjumping = rb.velocity.y > 0;
//        anime.SetBool("IsGrounded", IsGrounded);
//        ismoving = rb.velocity.x != 0;
//        anime.SetBool("isMoving", ismoving);
//        //anime.SetBool("isDashing", dashTime > 0);
//    }*/
//    private void flip()
//    {
//        facingDir = facingDir * -1;
//        facingright = !facingright;
//        transform.Rotate(0, 180, 0);
//    }
//    private void FlipController()
//    {
//        if (rb.velocity.x > 0 && !facingright)
//        {
//            flip();
//        }
//        else if (rb.velocity.x < 0 && facingright)
//        {
//            flip();
//        }
//    }
//    private void OnDrawGizmos()
//    {
//        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x,transform.position.y-groundcheckDistance));
//    }
//    /*private void Dash()
//    {
//        dashTime += dashDuration;
//    }*/
//}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    private Animator anime;
    private Rigidbody2D rb;
    public float speed;
    public float jumpforce;
    public bool isjumping;
    public bool ismoving;
    private bool _footstepsRegistered = false;
    private const string FOOTSTEP_KEY = "player_footsteps";
    private int facingDir = 1;
    private bool facingright = true;
    private float xInput;
    //[SerializeField] private float dashDuration;
    //private float dashTime;
    //[SerializeField] private float dashSpeed;
    //[SerializeField] private float dashCooldown;
    //private float dashCooldownTimer;

    [SerializeField] private float groundcheckDistance;
    [SerializeField] private LayerMask whatIsGround;

    [SerializeField] private float groundCheckWidth = 0.5f;
    //[SerializeField] private float groundCheckOffset = 0f;

    private bool IsGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anime = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        groundCheck();
        Movement();
        FlipController();
        AnimatorControllers();
    }


    private void groundCheck()
    {
        // ����1��ʹ�� BoxCast���Ƽ�����ⷶΧ������
        Vector2 boxSize = new Vector2(groundCheckWidth, 0.1f);
        Vector2 boxOrigin = new Vector2(transform.position.x, transform.position.y - groundcheckDistance / 2);

        RaycastHit2D hit = Physics2D.BoxCast(
            boxOrigin,
            boxSize,
            0f,
            Vector2.down,
            groundcheckDistance,
            whatIsGround
        );

        IsGrounded = hit.collider != null;

        // ��ѡ�����Կ��ӻ�
        // Debug.DrawRay(transform.position, Vector2.down * groundcheckDistance, IsGrounded ? Color.green : Color.red);
    }

    // ����2��ʹ�ö�����ߣ���ѡ�������������ķ�������������������
    private void groundCheckMultipleRays()
    {
        Vector2 leftPoint = new Vector2(transform.position.x - groundCheckWidth / 2, transform.position.y);
        Vector2 rightPoint = new Vector2(transform.position.x + groundCheckWidth / 2, transform.position.y);
        Vector2 centerPoint = transform.position;

        RaycastHit2D leftHit = Physics2D.Raycast(leftPoint, Vector2.down, groundcheckDistance, whatIsGround);
        RaycastHit2D centerHit = Physics2D.Raycast(centerPoint, Vector2.down, groundcheckDistance, whatIsGround);
        RaycastHit2D rightHit = Physics2D.Raycast(rightPoint, Vector2.down, groundcheckDistance, whatIsGround);

        IsGrounded = leftHit.collider != null || centerHit.collider != null || rightHit.collider != null;

        // ���Կ��ӻ�
        Debug.DrawRay(leftPoint, Vector2.down * groundcheckDistance, IsGrounded ? Color.green : Color.red);
        Debug.DrawRay(centerPoint, Vector2.down * groundcheckDistance, IsGrounded ? Color.green : Color.red);
        Debug.DrawRay(rightPoint, Vector2.down * groundcheckDistance, IsGrounded ? Color.green : Color.red);
    }

    private void CheckInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump();
        }
        /*if (Input.GetKeyDown(KeyCode.L)&&dashCooldownTimer<0)
        {
            dashCooldownTimer=dashCooldown;
            Dash();
        }*/
    }

    private void Movement()
    {
        /*if(dashTime > 0)
        {

            rb.velocity = new Vector2(facingDir * dashSpeed, 0);
            dashTime -= Time.deltaTime;
        }*/
        rb.velocity = new Vector2(xInput * speed, rb.velocity.y);
        //dashCooldownTimer -= Time.deltaTime;
    }

    private void jump()
    {
        if (IsGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpforce);
            AudioManager.Instance.PlayOneShotEffect("2 - Jump", AudioManager.Instance.JumpVolume);
        }
    }

    private void AnimatorControllers()
    {
        anime.SetFloat("yvelocity", rb.velocity.y);
        isjumping = rb.velocity.y > 0;
        anime.SetBool("IsGrounded", IsGrounded);
        ismoving = rb.velocity.x != 0;
        anime.SetBool("isMoving", ismoving);
        
        if (ismoving && !_footstepsRegistered&&IsGrounded)
        {
            AudioManager.Instance.RegisterContinuousAudioEffect(
                FOOTSTEP_KEY,
                () => ismoving,
                "1 - Walk"
            );
            _footstepsRegistered = true;
        }
        else if (!ismoving && _footstepsRegistered||!IsGrounded)
        {
            AudioManager.Instance.UnregisterContinuousAudioEffect(FOOTSTEP_KEY);
            _footstepsRegistered = false;
        }
    }
    private void flip()
    {
        facingDir = facingDir * -1;
        facingright = !facingright;
        transform.Rotate(0, 180, 0);
    }
    private void FlipController()
    {
        if (rb.velocity.x > 0 && !facingright)
        {
            flip();
        }
        else if (rb.velocity.x < 0 && facingright)
        {
            flip();
        }
    }

    private void OnDrawGizmos()
    {
        // ����Gizmos���ӻ�����ʾ�����ļ�ⷶΧ
        Gizmos.color = IsGrounded ? Color.green : Color.red;

        // ����1��BoxCast���ӻ�
        Vector2 boxSize = new Vector2(groundCheckWidth, 0.1f);
        Vector2 boxOrigin = new Vector2(transform.position.x, transform.position.y - groundcheckDistance / 2);
        //Gizmos.DrawWireCube(boxOrigin + Vector2.down * (groundcheckDistance / 2), boxSize);

        // ԭ�������߿��ӻ�����ѡ��
        //Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y - groundcheckDistance));
    }

    /*private void Dash()
    {
        dashTime += dashDuration;
    }*/
}