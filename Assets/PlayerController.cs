using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Tilemaps;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    private float horizontal;
    public float moveSpeed = 5;
    public float maxMovementSpeed = 5;
    Vector2 move;

    public float jumpStrength = 5;
    private bool isFacingRight = true;
    public int numJumps = 1;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    private bool isWallSliding;
    public float wallSlidingSpeed;

    private bool isWallJumping = false;
    private float wallJumpDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    public Vector2 wallJumpingPower = new Vector2(2f, 4f);

    public Sprite slideSprite;
    private Vector2 startScale;
    public float slideSlowdown;
    private bool isSliding;


    // Start is called before the first frame update
    void Start()
    {
        startScale = gameObject.transform.localScale;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        move = new Vector2(horizontal, 0);

        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpStrength);    
        }

        if (Input.GetKeyUp(KeyCode.UpArrow) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            //gameObject.GetComponent<SpriteRenderer>().sprite = slideSprite;
            gameObject.transform.localScale = new Vector2(startScale.y, startScale.x);
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y - startScale.y * 2);
            if (!isFacingRight)
            {
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
            if(isGrounded()) rb.velocity = new Vector2(rb.velocity.x * slideSlowdown, rb.velocity.y);
            isSliding = true;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            //gameObject.GetComponent<SpriteRenderer>().sprite = slideSprite;
            gameObject.transform.localScale = startScale;
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y + startScale.y * 2);
            if (!isFacingRight)
            {
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
            isSliding = false;
        }


        WallSlide();
        WallJump();
        if (!isWallJumping)
        {
            Flip();
        }

    }

    private void FixedUpdate()
    {
        if(Math.Abs(rb.velocity.x) < maxMovementSpeed && !isSliding)
        {
            rb.AddForce(move * moveSpeed, ForceMode2D.Impulse);
        }
        
    }

    private bool isGrounded()
    {
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(7f * transform.localScale.x, 1.5f * transform.localScale.y), CapsuleDirection2D.Horizontal, 0, groundLayer);
    }

    private bool isWallTouch()
    {
        return Physics2D.OverlapCapsule(wallCheck.position, new Vector2(1.5f * transform.localScale.x, 7f * transform.localScale.y), CapsuleDirection2D.Vertical, 0, wallLayer);
    }

    private void WallSlide()
    {
        if (isWallTouch() && !isGrounded())
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0;

            if(transform.localScale.x != wallJumpDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }

        
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        if(isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

}
