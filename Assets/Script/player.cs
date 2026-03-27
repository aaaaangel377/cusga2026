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
    //public bool isjumping;
    //public bool ismoving;
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

    // 新增：地面检测的宽度
    [SerializeField] private float groundCheckWidth = 0.5f;
    [SerializeField] private float groundCheckOffset = 0f; // 检测点偏移（可选）

    private bool IsGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anime = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        CheckInput();
        //AnimatorControllers();
        groundCheck();
        FlipController();
    }


    private void groundCheck()
    {
        // 方法1：使用 BoxCast（推荐，检测范围更宽）
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

        // 可选：调试可视化
        // Debug.DrawRay(transform.position, Vector2.down * groundcheckDistance, IsGrounded ? Color.green : Color.red);
    }

    // 方法2：使用多个射线（备选方案，如果上面的方法有问题可以用这个）
    private void groundCheckMultipleRays()
    {
        Vector2 leftPoint = new Vector2(transform.position.x - groundCheckWidth / 2, transform.position.y);
        Vector2 rightPoint = new Vector2(transform.position.x + groundCheckWidth / 2, transform.position.y);
        Vector2 centerPoint = transform.position;

        RaycastHit2D leftHit = Physics2D.Raycast(leftPoint, Vector2.down, groundcheckDistance, whatIsGround);
        RaycastHit2D centerHit = Physics2D.Raycast(centerPoint, Vector2.down, groundcheckDistance, whatIsGround);
        RaycastHit2D rightHit = Physics2D.Raycast(rightPoint, Vector2.down, groundcheckDistance, whatIsGround);

        IsGrounded = leftHit.collider != null || centerHit.collider != null || rightHit.collider != null;

        // 调试可视化
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
        }
    }

    /*private void AnimatorControllers()
    {
        anime.SetFloat("yvelocity", rb.velocity.y);
        isjumping = rb.velocity.y > 0;
        anime.SetBool("IsGrounded", IsGrounded);
        ismoving = rb.velocity.x != 0;
        anime.SetBool("isMoving", ismoving);
        //anime.SetBool("isDashing", dashTime > 0);
    }*/
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
        // 更新Gizmos可视化，显示更宽的检测范围
        Gizmos.color = IsGrounded ? Color.green : Color.red;

        // 方法1的BoxCast可视化
        Vector2 boxSize = new Vector2(groundCheckWidth, 0.1f);
        Vector2 boxOrigin = new Vector2(transform.position.x, transform.position.y - groundcheckDistance / 2);
        Gizmos.DrawWireCube(boxOrigin + Vector2.down * (groundcheckDistance / 2), boxSize);

        // 原来的射线可视化（可选）
        // Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y - groundcheckDistance));
    }

    /*private void Dash()
    {
        dashTime += dashDuration;
    }*/
}