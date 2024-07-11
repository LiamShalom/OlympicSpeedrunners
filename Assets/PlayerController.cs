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
    public float slideSlowdown;
    private bool isSliding;

    private Vector2 capsuleSize;
    [SerializeField]
    private float slopeCheckDistance;
    private Vector2 slopeNormalPerp;
    private bool onSlope;
    private float slopeSlideAngle;

    public bool isGrounded;

    //GameAndScoreManager Variables
    public bool isAlive = true;
    public int NumberOfWins = 0;
    public int AmountOfBoost = 0;

    // Animator
    public Animator animator; 

    



    [SerializeField] private float grappleBoost;


    private State currState;

    enum State{
        STATE_STANDING,
        STATE_RUNNING,
        STATE_JUMPING,
        STATE_SLIDING,
        STATE_WALLSLIDING,
        STATE_GRAPPLE

    };



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
        //CheckInput();
        checkStateMachine();
        SlopeCheck();
        //WallSlide();
        //WallJump();
        if (currState != State.STATE_WALLSLIDING)
        {
            Flip();
        }

        //Animator variables
        
        animator.SetBool("isRunning", currState == State.STATE_RUNNING);


    }

    private void FixedUpdate()
    {
        ApplyMovement();

        ClampVelocity();

    }

    private void checkStateMachine()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        move = new Vector2(xInput, 0);
        bool isOnGround = IsGrounded();
        bool isOnWall = isWallTouch();
        if (dj.enabled)
        {
            lr.SetPosition(1, transform.position);
        }
        switch (currState)
        {
            case State.STATE_RUNNING:
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
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    slide();
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    grapple();
                }
                break;

            case State.STATE_JUMPING:
                if (isOnGround)
                {
                    currState = State.STATE_STANDING;
                    break;
                }
                if (isOnWall)
                {
                    currState = State.STATE_WALLSLIDING;
                    break;
                }
                if (Input.GetKeyUp(KeyCode.UpArrow))
                {
                    fall();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    slide();
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    grapple();
                }
                break;
            case State.STATE_SLIDING:
                if (Input.GetKeyUp(KeyCode.DownArrow))
                {
                    stopSliding();
                }
                break;
            case State.STATE_WALLSLIDING:
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    slide();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    jump();
                }
                break;
            case State.STATE_GRAPPLE:
                if (!Input.GetKey(KeyCode.Z))
                {
                    stopGrappling();
                }
                break;
            default:
                break;
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
        cc.size = new Vector2(cc.size.y, cc.size.x);
        cc.direction = CapsuleDirection2D.Horizontal;
        gameObject.transform.position = new Vector2(transform.position.x, transform.position.y - (float)(cc.size.y / 2));
        Flip();
        if (IsGrounded()) rb.velocity = new Vector2(rb.velocity.x * slideSlowdown, rb.velocity.y);
        isSliding = true;
    }

    private void stopSliding()
    {
        currState = State.STATE_STANDING;
        gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
        cc.size = new Vector2(cc.size.y, cc.size.x);
        cc.direction = CapsuleDirection2D.Vertical;
        gameObject.transform.position = new Vector2(transform.position.x, transform.position.y + (float)(cc.size.x / 2));
        Flip();
        isSliding = false;
    }

    private void grapple()
    {
        currState = State.STATE_GRAPPLE;
        Vector3 grappleDirection;
            if (isFacingRight)
            {
                grappleDirection = (Vector2.right + Vector2.up) * 100;
            }
            else
            {
                grappleDirection = (Vector2.left + Vector2.up) * 100;
            }

            RaycastHit2D hit = Physics2D.Raycast(transform.position, grappleDirection, Mathf.Infinity, ceilingLayer);
            //Debug.DrawRay(transform.position, grappleDirection, Color.yellow, 0.5f, false);

            lr.SetPosition(0, hit.point);
            lr.SetPosition(1, transform.position);
            dj.connectedAnchor = hit.point;
            dj.enabled = true;
            lr.enabled = true;
            rb.velocity = new Vector2(maxSpeed, rb.velocity.y) * grappleBoost;


    }

    private void stopGrappling()
    {
        dj.enabled = false;
        lr.enabled = false;
        currJumps = 1;
        if (IsGrounded())
        {
            currState = State.STATE_STANDING;
        }
        else
        {
            currState = State.STATE_JUMPING;
        }
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
            cc.size = new Vector2(cc.size.y, cc.size.x);
            cc.direction = CapsuleDirection2D.Horizontal;
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y - (float)(cc.size.y / 2));
            Flip();
            if (IsGrounded()) rb.velocity = new Vector2(rb.velocity.x * slideSlowdown, rb.velocity.y);
            isSliding = true;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
            cc.size = new Vector2(cc.size.y, cc.size.x);
            cc.direction = CapsuleDirection2D.Vertical;
            gameObject.transform.position = new Vector2(transform.position.x, transform.position.y + (float)(cc.size.x / 2));
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

            RaycastHit2D hit = Physics2D.Raycast(transform.position, grappleDirection, Mathf.Infinity, ceilingLayer);
            //Debug.DrawRay(transform.position, grappleDirection, Color.yellow, 0.5f, false);

            lr.SetPosition(0, hit.point);
            lr.SetPosition(1, transform.position);
            dj.connectedAnchor = hit.point;
            dj.enabled = true;
            lr.enabled = true;
            rb.velocity = new Vector2(maxSpeed, rb.velocity.y) * grappleBoost;
        }
        if (dj.enabled)
        {
            lr.SetPosition(1, transform.position);
        }
    }

    private void ApplyMovement()
    {
        
        if (Math.Abs(rb.velocity.x) < maxSpeed && !isSliding)
        {
            if (IsGrounded())
            {
                currState = State.STATE_RUNNING;
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
                currState = State.STATE_JUMPING;
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
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(wallCheck.position.x * 2, 0.1f) * gameObject.transform.localScale, CapsuleDirection2D.Horizontal, 0, groundLayer);

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
<<<<<<< Updated upstream
    { 
        return Physics2D.OverlapCapsule(wallCheck.position, new Vector2(0.1f, cc.size.y) * gameObject.transform.localScale, CapsuleDirection2D.Vertical, 0, wallLayer);

=======
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
        bool isTouchingWall = Physics2D.OverlapCapsule(wallChecker, new Vector2(0.1f, Math.Abs(groundCheck.position.y) * 2) * gameObject.transform.localScale, CapsuleDirection2D.Vertical, 0, wallLayer);


        return isTouchingWall;
>>>>>>> Stashed changes
    }

    private void WallSlide()
    {
        if (isWallTouch() && !IsGrounded())
        {
            currState = State.STATE_WALLSLIDING;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
    }

    private void WallJump()
    {
        currState = State.STATE_JUMPING;
        wallJumpDirection = -transform.localScale.x;
        rb.velocity = new Vector2(wallJumpDirection * wallJumpingPower.x, wallJumpingPower.y);

        if (transform.localScale.x != wallJumpDirection)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
            grappleBoost *= -1;
        }
        currJumps = maxJumps;


    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == wallLayer)
        {
            rb.velocity = new Vector2(0, rb.velocity.x);
        }
    }
    private void Flip()
    {
        if(isFacingRight && xInput < 0f || !isFacingRight && xInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
            grappleBoost *= -1;
        }
    }

}
