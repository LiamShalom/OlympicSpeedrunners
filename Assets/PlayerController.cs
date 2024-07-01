using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Tilemaps;
using TMPro;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    private CapsuleCollider2D cc;
    public BoxCollider2D standingCollider;
    public BoxCollider2D slidingCollider;
    private float xInput;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    public float moveSpeed = 5;
    public float maxMovementSpeed = 5;
    Vector2 move;
    Vector2 newVelocity;

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
    public Sprite standingSprite;
    public float slideSlowdown;
    private bool isSliding;

    private Vector2 capsuleSize;
    [SerializeField]
    private float slopeCheckDistance;
    private Vector2 slopeNormalPerp;
    private bool onSlope;
    private float slopeSlideAngle;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();

        capsuleSize = cc.size;
    }

    // Update is called once per frame
    void Update()
    {
        checkInput();
        slopeCheck();
        WallSlide();
        WallJump();
        if (!isWallJumping)
        {
            Flip();
        }

    }

    private void FixedUpdate()
    {
        applyMovement();
    }

    private void checkInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        move = new Vector2(xInput, 0);
        if (isGrounded())
        {
            numJumps = 1;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && numJumps > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpStrength);
            numJumps--;
        }

        if (Input.GetKeyUp(KeyCode.UpArrow) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            gameObject.GetComponent<SpriteRenderer>().sprite = slideSprite;
            standingCollider.enabled = false;
            cc.enabled = false;
            slidingCollider.enabled = true;
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y - (float)(slidingCollider.size.y / 2));
            Flip();
            if (isGrounded()) rb.velocity = new Vector2(rb.velocity.x * slideSlowdown, rb.velocity.y);
            isSliding = true;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
            slidingCollider.enabled = false;
            standingCollider.enabled = true;
            cc.enabled = true;
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y + (float)(slidingCollider.size.y / 2));
            Flip();
            isSliding = false;
        }
    }

    private void applyMovement()
    {
        if (Math.Abs(rb.velocity.x) < maxMovementSpeed && !isSliding)
        {
            if (isGrounded() && !onSlope)
            {
                rb.AddForce(move * moveSpeed, ForceMode2D.Impulse);
            }
            else if (isGrounded() && onSlope)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y);
                move = new Vector2(slopeNormalPerp.x, slopeNormalPerp.y);
                rb.AddForce(move * moveSpeed * -xInput, ForceMode2D.Impulse);
            }
            else if (!isGrounded())
            {
                rb.AddForce(move * moveSpeed/2, ForceMode2D.Impulse);
            }
        }
        
    }
        

    private bool isGrounded()
    {
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(wallCheck.position.x * 2, 0.2f) * gameObject.transform.localScale, CapsuleDirection2D.Horizontal, 0, groundLayer);
    }

    private void slopeCheck()
    {
        Vector2 checkPos;
        if (isFacingRight)
        {
            checkPos = transform.position - new Vector3(-capsuleSize.x /2, capsuleSize.y / 2);
        }
        else
        {
            checkPos = transform.position - new Vector3(capsuleSize.x / 2, capsuleSize.y / 2);
        }
        slopeCheckHorizontal(checkPos);
        slopeCheckVertictal(checkPos);
    }

    private void slopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, groundLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, groundLayer);

        if (slopeHitFront)
        {
            onSlope = true;
            slopeSlideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);

        }else if (slopeHitBack){
            onSlope = true;
            slopeSlideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            onSlope = false;
            slopeSlideAngle = 0;
        }
    }

    private void slopeCheckVertictal(Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, groundLayer);
        if (hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeDownAngle != slopeDownAngleOld)
            {
                onSlope = true;
            }
            slopeDownAngleOld = slopeDownAngle;
            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
    }

    private bool isWallTouch()
    {
        Vector2 wallChecker;
        if (isFacingRight)
        {
            wallChecker = wallCheck.position + Vector3.right * 0.1f;

        }
        else
        {
            wallChecker = wallCheck.position + Vector3.left * 0.1f;

        }
        bool isTouchingWall = Physics2D.OverlapCapsule(wallChecker, new Vector2(0.2f, Math.Abs(groundCheck.position.y) * 2) * gameObject.transform.localScale, CapsuleDirection2D.Vertical, 0, wallLayer);


        return isTouchingWall;
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
        if(isFacingRight && xInput < 0f || !isFacingRight && xInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

}
