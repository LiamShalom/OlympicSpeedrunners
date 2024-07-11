using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Tilemaps;
using TMPro;
using Unity.VisualScripting;
using Unity.Burst.CompilerServices;
using System.Runtime.CompilerServices;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    private CapsuleCollider2D cc;
    private LineRenderer lr;
    private DistanceJoint2D dj;
    private float xInput;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    public float moveSpeed = 5f;
    public float maxSpeed = 20f;
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
    public float wallSlidingSpeed;

    private float wallJumpDirection;
    public Vector2 wallJumpingPower = new Vector2(2f, 4f);

    public Sprite slideSprite;
    public Sprite standingSprite;
    public Sprite wallSlideSprite;
    public float slideSlowdown;
    private bool isSliding;

    private Vector2 capsuleSize;
    [SerializeField]
    private float slopeCheckDistance;
    private Vector2 slopeNormalPerp;
    private bool onSlope;
    private float slopeSlideAngle;

    [SerializeField] private float slideMinVelocity;

    public bool isGrounded;


    [SerializeField] private float grappleBoost;

    private float totalVelocity;
    [SerializeField] private Vector2 wallBoost;
    [SerializeField] private float fallOffWall;
    Vector3 grappleDirection;
    [SerializeField] private float safeDistanceAboveGround;

    [SerializeField] private State currState;

    enum State{
        STATE_STANDING,
        STATE_JUMPING,
        STATE_SLIDING,
        STATE_WALLSLIDING,
        STATE_GRAPPLE

    };

    //GameAndScoreManager Variables
    public bool isAlive = true;
    public int NumberOfWins = 0;
    public int AmountOfBoost = 0;
    public bool IsOutOfBounds = false;



    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
        dj = GetComponent<DistanceJoint2D>();
        lr = GetComponent<LineRenderer>();
        dj.enabled = false;

        capsuleSize = cc.size;
        currJumps = maxJumps;
    }

    // Update is called once per frame
    void Update()
    {
        checkStateMachine();
        
        if (currState != State.STATE_WALLSLIDING)
        {
            Flip();
        }

    }

    
    private void FixedUpdate()
    {
        if(currState == State.STATE_STANDING) ApplyMovement();

        ClampVelocity();

    }
    

    private void checkStateMachine()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        move = new Vector2(xInput, 0);
        totalVelocity = Math.Abs(rb.velocity.x) + Math.Abs(rb.velocity.y);
        bool isOnGround = IsGrounded();
        bool isOnWall = isWallTouch();
        if (!isOnWall) wallBoost = new Vector2(0f, 100f + rb.velocity.y);
        RaycastHit2D grapplePoint;

        switch (currState)
        {
            case State.STATE_STANDING:
                currJumps = maxJumps;
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    jump();
                }
                if (isOnWall)
                {
                    WallSlide();
                    break;
                }
                if (Input.GetKey(KeyCode.DownArrow) && totalVelocity >= slideMinVelocity)
                {
                    slide();
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    grapplePoint = grapple();
                }
                break;

            case State.STATE_JUMPING:
                if (isOnGround)
                {
                    backtoStanding();
                    break;
                }
                if (isOnWall)
                {
                    WallSlide();
                    break;
                }
                if (Input.GetKeyUp(KeyCode.UpArrow))
                {
                    fall();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    jump();
                }
                if (Input.GetKey(KeyCode.DownArrow) && totalVelocity >= slideMinVelocity)
                {
                    slide();
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    grapplePoint = grapple();
                }
                break;

            case State.STATE_SLIDING:
                if (!Input.GetKey(KeyCode.DownArrow))
                {
                    backtoStanding();
                    gameObject.transform.position += new Vector3(0, capsuleSize.x / 2, 0);
                }
                break;

            case State.STATE_WALLSLIDING:
                if (isOnGround && rb.velocity.y < 0)
                {
                    stopWallSliding();
                    backtoStanding();
                    rb.AddForce(new Vector2(moveSpeed * transform.localScale.x * fallOffWall, 0), ForceMode2D.Impulse);
                    break;
                }
                if (!isOnWall)
                {
                    backtoStanding();
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    jump();
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    grapplePoint = grapple();
                }
                break;

            case State.STATE_GRAPPLE:
                if (isOnGround)
                {
                    stopGrappling();
                }
                if (isOnWall)
                {
                    stopGrappling();
                    WallSlide();
      
                }
                if (!Input.GetKey(KeyCode.Z))
                {
                    stopGrappling();
                }
                break;

            default:
                break;
        }
        if (dj.enabled)
        {
            lr.SetPosition(1, transform.position);
        }
    }

    private void jump()
    {
        if (isWallTouch())
        {
            WallJump();
        }
        else
        {
            if (currJumps > 0)
            {
                currState = State.STATE_JUMPING;
                rb.velocity = new Vector2(rb.velocity.x, jumpStrength);
            }
        }
        
    }

    private void fall()
    {
        if (rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
        currJumps--;
    }
    
    private void slide()
    {
        currState = State.STATE_SLIDING;
        gameObject.GetComponent<SpriteRenderer>().sprite = slideSprite;
        cc.size = new Vector2(capsuleSize.y, capsuleSize.x);
        cc.direction = CapsuleDirection2D.Horizontal;
        gameObject.transform.position -= new Vector3(0, capsuleSize.y / 2, 0);
        if (IsGrounded()) rb.velocity = new Vector2(rb.velocity.x * slideSlowdown, rb.velocity.y);
    }

    private void backtoStanding()
    {
        currState = State.STATE_STANDING;
        gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
        cc.size = new Vector2(capsuleSize.x, capsuleSize.y);
        cc.direction = CapsuleDirection2D.Vertical;
    }

    private void stopWallSliding()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
        gameObject.transform.position += new Vector3(capsuleSize.x / 4, 0, 0) * transform.localScale.x;
        rb.AddForce(new Vector2(fallOffWall, 0) * localScale.x, ForceMode2D.Impulse);
    }

    private RaycastHit2D grapple()
    {
        currState = State.STATE_GRAPPLE;
        if (isFacingRight)
        {
            grappleDirection = Vector2.right + Vector2.up;
        }
        else
        {
            grappleDirection = Vector2.left + Vector2.up;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, grappleDirection, Mathf.Infinity, ceilingLayer);
        //Debug.DrawRay(transform.position, grappleDirection, Color.yellow, 0.5f, false);

        lr.SetPosition(0, hit.point);
        lr.SetPosition(1, transform.position);
        dj.connectedAnchor = hit.point;
        dj.enabled = true;
        lr.enabled = true;
        //rb.velocity = new Vector2(maxSpeed, rb.velocity.y) * grappleBoost * transform.localScale.x;
        return hit;
    }

    private void stopGrappling()
    {
        currState = State.STATE_JUMPING;
        dj.enabled = false;
        lr.enabled = false;
        currJumps = 1;
    }

    private void ApplyMovement()
    {
        
        if (Math.Abs(rb.velocity.x) < maxSpeed)
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
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(cc.size.x, 0.1f) * gameObject.transform.localScale, CapsuleDirection2D.Horizontal, 0, groundLayer);

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
        return Physics2D.OverlapCapsule(wallCheck.position, new Vector2(0.1f, cc.size.y) * gameObject.transform.localScale, CapsuleDirection2D.Vertical, 0, wallLayer);

    }

    private void WallSlide()
    {
        if (isWallTouch())
        {
            currState = State.STATE_WALLSLIDING;
            gameObject.GetComponent<SpriteRenderer>().sprite = wallSlideSprite;
            cc.size = new Vector2(capsuleSize.x / 2, capsuleSize.y);
            gameObject.transform.position += new Vector3(capsuleSize.x / 4, 0, 0) * transform.localScale.x;
            if (rb.velocity.y >= 0) rb.AddForce(wallBoost, ForceMode2D.Impulse);
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
    }

    private void WallJump()
    {
        currState = State.STATE_JUMPING;
        wallJumpDirection = -transform.localScale.x;
        gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
        cc.size = new Vector2(capsuleSize.x, capsuleSize.y);
        rb.velocity = new Vector2(wallJumpDirection * wallJumpingPower.x, wallJumpingPower.y);

        if (transform.localScale.x != wallJumpDirection)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
        currJumps = maxJumps;


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
