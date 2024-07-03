using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Tilemaps;
using TMPro;
using Unity.VisualScripting;
using Unity.Burst.CompilerServices;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    private BoxCollider2D bc;
    private float xInput;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    public float moveSpeed = 5f;
    public float maxSpeed = 10f;
    Vector2 move;
    Vector2 newVelocity;

    public float jumpStrength = 10f;
    private bool isFacingRight = true;
    [SerializeField] private int maxJumps = 2;
    public int currJumps;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    [SerializeField] private LayerMask ceilingLayer;
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

    public bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();

        capsuleSize = bc.size;
        currJumps = maxJumps;
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        SlopeCheck();
        WallSlide();
        WallJump();
        if (!isWallJumping)
        {
            Flip();
        }

    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ClampVelocity();

    }

    private void CheckInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        move = new Vector2(xInput, 0);

        bool isGrounded = IsGrounded();

        if (isGrounded)
        {
            currJumps = maxJumps;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && currJumps > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpStrength);
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            if (rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            }
            currJumps--;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            gameObject.GetComponent<SpriteRenderer>().sprite = slideSprite;
            bc.size = new Vector2(bc.size.y, bc.size.x);
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y - (float)(bc.size.y / 2));
            Flip();
            if (IsGrounded()) rb.velocity = new Vector2(rb.velocity.x * slideSlowdown, rb.velocity.y);
            isSliding = true;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
            bc.size = new Vector2(bc.size.y, bc.size.x);
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y + (float)(bc.size.x / 2));
            Flip();
            isSliding = false;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Vector3 grappleDirection;
            if (isFacingRight)
            {
                grappleDirection = (Vector2.right + Vector2.up) * 100;
            }
            else
            {
                grappleDirection = (Vector2.left + Vector2.up) * 100;
            }

            RaycastHit2D ray = Physics2D.Raycast(transform.position, grappleDirection, Mathf.Infinity);
            Debug.DrawRay(transform.position, grappleDirection, Color.yellow);

        }
    }

    private void ApplyMovement()
    {
        
        if (Math.Abs(rb.velocity.x) < maxSpeed && !isSliding)
        {
            if (IsGrounded())
            {
                if (!onSlope)
                {
                    rb.AddForce(move * moveSpeed, ForceMode2D.Impulse);
                }
                else
                {
                    Vector2 slopeMovementDirection = new Vector2(slopeNormalPerp.x * -xInput, slopeNormalPerp.y * -xInput).normalized;
                    rb.AddForce(slopeMovementDirection * moveSpeed, ForceMode2D.Impulse);
                }
            }
            else
            {
                rb.AddForce(move * moveSpeed / 4, ForceMode2D.Impulse);
            }
        }
        

    }

    void ClampVelocity()
    {
        float clampedX = Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed);
        float clampedY = Mathf.Clamp(rb.velocity.y, -maxSpeed, maxSpeed);
        rb.velocity = new Vector2(clampedX, clampedY);
    }


    private bool IsGrounded()
    {
        isGrounded = Physics2D.OverlapCapsule(groundCheck.position, new Vector2(wallCheck.position.x * 2, 0.1f) * gameObject.transform.localScale, CapsuleDirection2D.Horizontal, 0, groundLayer);
        return isGrounded;

    }

    private void SlopeCheck()
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
        bool isTouchingWall = Physics2D.OverlapCapsule(wallChecker, new Vector2(0.2f, Math.Abs(groundCheck.position.y)) * gameObject.transform.localScale, CapsuleDirection2D.Vertical, 0, wallLayer);


        return isTouchingWall;
    }

    private void WallSlide()
    {
        if (isWallTouch() && !IsGrounded())
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
