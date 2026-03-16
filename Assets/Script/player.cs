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
        IsGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundcheckDistance, whatIsGround);
    }
   
    private void CheckInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.K))
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
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x,transform.position.y-groundcheckDistance));
    }
    /*private void Dash()
    {
        dashTime += dashDuration;
    }*/
}