using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Tilemaps;
using TMPro;
using Unity.VisualScripting;
using Unity.Burst.CompilerServices;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    private CapsuleCollider2D cc;
    private BoxCollider2D bc;
    private LineRenderer lr;
    private DistanceJoint2D dj;
    public Tilemap tm;
    public float xInput;
    private float slopeDownAngle;
    private float slopeDownAngleOld;
    Vector2 move;
    Vector2 newVelocity;

    public float moveSpeed = 5f;
    public float speed = 0;
    private float MAXSPEED = 12f;
    public float maxSpeed = 10f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    private KeyCode LeftKey = KeyCode.LeftArrow;
    private KeyCode RightKey = KeyCode.RightArrow;
    private KeyCode JumpKey = KeyCode.UpArrow;
    private KeyCode SlideKey = KeyCode.DownArrow;
    private KeyCode GrappleKey = KeyCode.Z;

    public float jumpStrength = 10f;
    public bool isFacingRight = true;
    [SerializeField] private int maxJumps = 2;
    public int currJumps;
    public float jumpStartTime;
    private float jumpTime;

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
    public bool onSlope;
    private float slopeSideAngle;

    [SerializeField] private float slideMinVelocity;

    public bool isGrounded;
    public LayerMask sand;

    [SerializeField] private float grappleBoost;

    private float totalVelocity;
    [SerializeField] private Vector2 wallBoost;
    [SerializeField] private float fallOffWall;
    Vector3 grappleDirection;
    Vector2 grapplePoint;
    public bool grappleRight;
    [SerializeField] private float shortenDistance;

    [SerializeField] private State currState;

    public PhysicsMaterial2D slidingMaterial;

    [SerializeField] private TileBase groundTile;
    [SerializeField] private float obstacleRespawnTime;

    public TileBase obstacle;

    enum State
    {
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

    public Animator animator;
    public float horizontalMovementVelocity = 0f;





    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CapsuleCollider2D>();
        bc = GetComponent<BoxCollider2D>();
        dj = GetComponent<DistanceJoint2D>();
        lr = GetComponent<LineRenderer>();
        dj.enabled = false;

        capsuleSize = cc.size;
        currJumps = maxJumps;
    }

    // Update is called once per frame
    public void Update()
    {

        //animator code

        animator.SetFloat("Speed", Math.Abs(rb.velocity.x));
        animator.SetFloat("FallingSpeed", rb.velocity.y);
        
        if (currState == State.STATE_WALLSLIDING)
        {
            animator.SetBool("isWallClinging", true);
        }
        else
            animator.SetBool("isWallClinging", false);

        if (currState == State.STATE_SLIDING)
        {
            animator.SetBool("isCrouching", true);
        }else
            animator.SetBool("isCrouching", false);

        if (currState == State.STATE_JUMPING)
        {
            animator.SetBool("isJumping", true);
        }
        else
            animator.SetBool("isJumping", false);




        checkStateMachine();
        SlopeCheck();

        if (IsOnSand())
        {
            maxSpeed = MAXSPEED/2;
        }
        else
        {
            maxSpeed = MAXSPEED;
        }

        if (currState != State.STATE_WALLSLIDING)
        {
            Flip();
        }

    }


    private void FixedUpdate()
    {
        if (currState == State.STATE_STANDING || currState == State.STATE_JUMPING) ApplyMovement(move);

        ClampVelocity();

    }


    private void checkStateMachine()
    {
        if (currState != State.STATE_GRAPPLE)
        {
            xInput = Input.GetAxisRaw("Horizontal");
            if (xInput != 0f)
            {
                speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                speed = Mathf.MoveTowards(speed, 0f, deceleration * Time.deltaTime);
            }

            move = new Vector2(xInput, 0).normalized;
        }
        else
        {
            // While grappling, don't allow movement input to change direction
            move = Vector2.zero;
            xInput = 0;
        }

        totalVelocity = Math.Abs(rb.velocity.x) + Math.Abs(rb.velocity.y);
        bool isOnGround = IsGrounded();
        bool isOnWall = isWallTouch();
        if (!isOnWall) wallBoost = new Vector2(0f, 50f + rb.velocity.y);
        if (isOnGround) currJumps = maxJumps;

        switch (currState)
        {
            case State.STATE_STANDING:
                if (!isOnGround)
                {
                    currState = State.STATE_JUMPING;
                    break;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    jump();
          
                }
                if (isOnWall)
                {
                    WallSlide();
                    break;
                }
                if (Input.GetKey(SlideKey) && totalVelocity >= slideMinVelocity)
                {
                    slide();
                }
                break;

            case State.STATE_JUMPING:

                if (isOnWall)
                {
                    WallSlide();
                    break;
                }
                if (isOnGround)
                {
                    backtoStanding();
                    break;
                }
                if (Input.GetKeyUp(JumpKey))
                {
                    fall();
                }
                if (Input.GetKeyDown(JumpKey))
                {
                    jump();
                }
                if (Input.GetKey(SlideKey) && totalVelocity >= slideMinVelocity)
                {
                    slide();
                }
                if (Input.GetKeyDown(GrappleKey))
                {
                    grapple();
                }
                break;

            case State.STATE_SLIDING:
                if (!Input.GetKey(SlideKey) || rb.velocity.x == 0f)
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
                    //rb.AddForce(new Vector2(moveSpeed * transform.localScale.x * fallOffWall, 0), ForceMode2D.Impulse);
                    break;
                }
                if (!isOnWall)
                {
                    backtoStanding();
                    break;
                }
                if (Input.GetKeyDown(JumpKey))
                {
                    jump();
                }
                break;

            case State.STATE_GRAPPLE:
                lr.SetPosition(1, transform.position);

                if (dj.enabled)
                {
                    Vector2 directionToAnchor = dj.connectedAnchor - (Vector2)transform.position;
                    //Debug.Log(directionToAnchor);
                    
                    if(directionToAnchor.y <= 0f)
                    {
                        stopGrappling();
                    }
                    
                    // Calculate the initial perpendicular force direction
                    Vector2 forceDirection = Vector2.Perpendicular(directionToAnchor).normalized;

                    // Ensure the force starts by pointing down and in the facing direction
                    if (isFacingRight) forceDirection *= -1;

                    //Debug.DrawRay(transform.position, forceDirection, Color.red);
                    rb.AddForce(forceDirection * grappleBoost, ForceMode2D.Force);
                }

                if (IsGrounded())
                {
                    stopGrappling();
                    backtoStanding();
                    //float dist = (transform.position.x - dj.connectedAnchor.x) / (transform.position.y - dj.connectedAnchor.y);
                    //float grappleAngle = Mathf.Atan(dist);
                    //float grappleLength = dj.distance * Mathf.Cos(grappleAngle);
                    //Debug.Log("Angle Between Objects: " + grappleAngle);
                    //Debug.Log("Distance Between Objects: " + grappleLength);
                    //StartCoroutine(ShortenGrapple(grappleLength));
                    //rb.velocity = new Vector2(maxSpeed * transform.localScale.x, 0);


                }
                if (isOnWall)
                {
                    stopGrappling();
                    WallSlide();

                }
                if (!Input.GetKey(GrappleKey))
                {
                    stopGrappling();
                }
                break;

            default:
                break;
        }
        
    }


    public void jump()
    {
        
        currState = State.STATE_JUMPING;
        if (isWallTouch())
        {
            WallJump();
        }
        else
        {
            if (currJumps > 0)
            {
                
                rb.velocity = new Vector2(rb.velocity.x, jumpStrength);
            }
        }

    }

    private void fall()
    {
        if (rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
        currJumps--;
    }

    public void slide()
    {
        currState = State.STATE_SLIDING;
        gameObject.GetComponent<SpriteRenderer>().sprite = slideSprite;
        cc.size = new Vector2(capsuleSize.y, capsuleSize.x);
        cc.direction = CapsuleDirection2D.Horizontal;

        if (IsGrounded())
        {
            StartCoroutine(ApplySlideFriction());
        }
    }

    private IEnumerator ApplySlideFriction()
    {
        while (currState == State.STATE_SLIDING && IsGrounded())
        {
            // Apply a small backward force to decelerate the character gradually
            rb.AddForce(new Vector2(-deceleration * Time.deltaTime, 0), ForceMode2D.Force);

            // Break the loop if the character slows down enough or stops sliding
            if (Mathf.Abs(rb.velocity.x) <= slideMinVelocity || !Input.GetKey(SlideKey))
            {
                //backtoStanding();
                yield break;
            }

            // Wait for the next frame
            yield return null;
        }
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
        //Vector3 localScale = transform.localScale;
        //localScale.x *= -1f;
        //transform.localScale = localScale;
        transform.Rotate(0f, 180f, 0f);
        gameObject.transform.position += new Vector3(capsuleSize.x / 4 * (isFacingRight ? 1 : -1), 0, 0);

        // Add force to move the character off the wall
        rb.AddForce(new Vector2(fallOffWall * (isFacingRight ? 1 : -1), 0), ForceMode2D.Impulse);
    }

    public void grapple()
    {
        currState = State.STATE_GRAPPLE;
        if (isFacingRight)
        {
            grappleDirection = Vector2.right + Vector2.up;
            grappleRight = true;
        }
        else
        {
            grappleDirection = Vector2.left + Vector2.up;
            grappleRight = false;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, grappleDirection, Mathf.Infinity, groundLayer);
        RaycastHit2D hit2 = Physics2D.Raycast(transform.position, grappleDirection, Mathf.Infinity, ceilingLayer);
        //Debug.DrawRay(transform.position, grappleDirection, Color.yellow, 0.5f, false);

        if (hit.collider != null && hit2.collider != null)
        {
            dj.connectedAnchor = hit.point;
            lr.SetPosition(0, hit.point);
            lr.SetPosition(1, transform.position);
            lr.enabled = true;
            if (hit.point == hit2.point)
            {
                dj.enabled = true;
                float direction = isFacingRight ? 1f : -1f;
                rb.velocity = new Vector2(maxSpeed * direction, rb.velocity.y);
                currJumps = 1;

            }

        }
    }


    private void stopGrappling()
    {
        currState = State.STATE_JUMPING;
        cc.enabled = true;
        dj.enabled = false;
        lr.enabled = false;
    }


    public void ApplyMovement(Vector2 move)
    {

        Vector2 force = Vector2.zero;

        if (currState == State.STATE_JUMPING)
        {
            force = move * (speed / 5f);
        }
        else
        {
            if (!onSlope)
            {
                force = move * speed;
            }
            else
            {
                Vector2 slopeMovementDirection = (slopeNormalPerp * -xInput).normalized;
                force = slopeMovementDirection * speed;
            }
        }

        if (xInput != 0) // Apply force only if there's input
        {
            if (Mathf.Abs(rb.velocity.x) < maxSpeed)
            {
                rb.AddForce(force * speed * 0.0025f, ForceMode2D.Impulse); // Small initial impulse
            }
            else
            {
                rb.AddForce(force * speed, ForceMode2D.Force); // Continuous force for smoother movement
            }
        }
        else
        {
            // Slow down and stop if no input (this will also prevent sliding on slopes)
            rb.velocity = new Vector2(Mathf.Lerp(rb.velocity.x, 0, Time.deltaTime * deceleration), rb.velocity.y);
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
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(cc.size.x * 0.9f, 0.1f) * gameObject.transform.localScale, CapsuleDirection2D.Horizontal, 0, groundLayer);
    }

    private bool IsOnSand()
    {
        return Physics2D.OverlapCapsule(groundCheck.position, new Vector2(cc.size.x * 0.9f, 0.1f) * gameObject.transform.localScale, CapsuleDirection2D.Horizontal, 0, sand);
    }


    private void SlopeCheck()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, -Vector2.up, slopeCheckDistance, groundLayer);

        // Use the same small tolerance for the downward angle check
        float slopeAngleThreshold = 5f; // Adjust this value based on your game's requirements

        if (hit)
        {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            onSlope = slopeDownAngle > slopeAngleThreshold;
            slopeDownAngleOld = slopeDownAngle;

            // Debugging visualizations (optional)
            //Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            //Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
        else
        {
            onSlope = false;
            slopeDownAngle = 0;
        }
        /*
        if(onSlope && xInput == 0f)
        {
            Vector2 force = -rb.velocity * rb.mass;
            rb.AddForce(force, ForceMode2D.Impulse);
        }
        */
    }

    private bool isWallTouch()
    {
        return Physics2D.OverlapCapsule(wallCheck.position, new Vector2(0.1f, cc.size.y * 0.9f) * gameObject.transform.localScale, CapsuleDirection2D.Vertical, 0, wallLayer);
    }

    private void WallSlide()
    {
        if (isWallTouch())
        {
            currState = State.STATE_WALLSLIDING;
            gameObject.GetComponent<SpriteRenderer>().sprite = wallSlideSprite;
            cc.size = new Vector2(capsuleSize.x / 2, capsuleSize.y);
            //gameObject.transform.position += new Vector3(capsuleSize.x / 4, 0, 0) * transform.localScale.x;
            if (rb.velocity.y >= 0) rb.AddForce(wallBoost, ForceMode2D.Impulse);
            rb.velocity = new Vector2(0f, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
    }

    private void WallJump()
    {
        currState = State.STATE_JUMPING;
        wallJumpDirection = isFacingRight ? -1 : 1;
        gameObject.GetComponent<SpriteRenderer>().sprite = standingSprite;
        cc.size = new Vector2(capsuleSize.x, capsuleSize.y);

        rb.velocity = new Vector2(wallJumpDirection * wallJumpingPower.x, wallJumpingPower.y);

        if ((isFacingRight && wallJumpDirection < 0) || (!isFacingRight && wallJumpDirection > 0))
        {
            isFacingRight = !isFacingRight;
            transform.Rotate(0f, 180f, 0f);
        }
        currJumps = maxJumps;


    }

    private void Flip()
    {
        if (isFacingRight && xInput < 0f || !isFacingRight && xInput > 0f)
        {
            isFacingRight = !isFacingRight;
            transform.Rotate(0f, 180f, 0f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 hitPosition = Vector3.zero;
        if (collision != null && collision.gameObject.CompareTag("Obstacle"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector3 contactPoint = contact.point;
                Vector3Int tilePosition = tm.WorldToCell(contactPoint);
                TileBase obstacle = tm.GetTile(tilePosition);
                //Debug.Log("Tile found at position: " + tilePosition);

                if (obstacle != null)
                {
                    if(currState == State.STATE_GRAPPLE) stopGrappling();
                    tm.SetTile(tilePosition, null);
                    StartCoroutine(RestoreTile(tilePosition, obstacle));
                }
            }

        }

        if(collision != null && collision.gameObject.CompareTag("Ground") && currState == State.STATE_GRAPPLE)
        {
            stopGrappling();
        }
    }

    private IEnumerator RestoreTile(Vector3Int tilePosition, TileBase obstacle)
    {
        //Debug.Log("Restoring" + obstacle.name);
        yield return new WaitForSeconds(obstacleRespawnTime);

        //Debug.Log("Restoring tile at position: " + tilePosition);
        tm.SetTile(tilePosition, obstacle);
    }
}

