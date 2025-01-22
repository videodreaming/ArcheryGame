using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class PlayerScript : MonoBehaviour
{
    //This needs to be set up dynamically : i.e the playerIDnumber; 
    public int playerIDnumber = 1;
    public float speed;
    public float jumpForce;
    private Rigidbody2D rb;
    private bool isGrounded;
    private int maxNumberofArrows = 3;
    private int currentNumberofArrows = 2;

    // Dash Variables
    public float dashSpeed = 10f;
    public float doubleTapTime;
    private float lastTapTime;
    private KeyCode lastKeyCode;
    public float dashCooldown = 1.0f;
    private float lastDashTime;

    //Aiming Circle

    public LineRenderer circleRenderer;
    public LineRenderer directionRenderer;
    public int circleSegments = 50;
    public float circleRadius = 3.0f;
    public float reducedcircleRadius = 1.5f;
    private bool jumpRequest = false;
    private int dashDirection = 0;

    //Sub Aiming Circles
    public Transform aimingCircle1;
    public Transform aimingCircle2;
    private float holdTime = 0f;
    public float holdTimeToMaxAccuracy = 2.0f;  // Time in seconds to reach maximum accuracy
    public float maxAimingDistance = 1.5f;  // Max distance the aiming circles can be apart
    public GameObject arrowPrefab;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();     
        SetupCircle();  
    }

    void SetupCircle()
    {
        circleRenderer.positionCount = circleSegments + 1;
        circleRenderer.useWorldSpace = false;
        UpdateCircle();

        directionRenderer.positionCount = 2;
        directionRenderer.useWorldSpace = false;
    }

    void UpdateCircle()
    {
        float deltaTheta = (2f * Mathf.PI) / circleSegments;
        float theta = 0f;

        for (int i = 0; i < circleSegments + 1; i++)
        {
            float x = circleRadius * Mathf.Cos(theta);
            float y = circleRadius * Mathf.Sin(theta);

            Vector3 pos = new Vector3(x, y, 0);
            circleRenderer.SetPosition(i, pos);

            theta += deltaTheta;
        }
    }

    void UpdateDirection(Vector3 direction)
    {
        directionRenderer.SetPosition(0, Vector3.zero);
        directionRenderer.SetPosition(1, direction * circleRadius);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequest = true;
        }

        if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A)) && (Time.time >= lastDashTime + dashCooldown))
        {
            KeyCode currentKeyCode = Input.GetKeyDown(KeyCode.D) ? KeyCode.D : KeyCode.A;
            if (currentKeyCode == lastKeyCode && (Time.time - lastTapTime) < doubleTapTime)
            {
                dashDirection = (currentKeyCode == KeyCode.D ? 1 : -1);
            }
            lastTapTime = Time.time;
            lastKeyCode = currentKeyCode;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 direction = (mousePos - transform.position).normalized;

        if (Input.GetMouseButton(0))
        {
            holdTime += Time.deltaTime;
            circleRenderer.enabled = true;
            UpdateDirection(direction);
            directionRenderer.enabled = true;
            reducedcircleRadius = circleRadius - maxAimingDistance;
            float angleOffset = Mathf.Rad2Deg * (1f / reducedcircleRadius);  // angle in degrees

            // Base position directly in the direction of the mouse
            Vector3 basePosition = direction * reducedcircleRadius;

            // Rotated positions for aiming circles
            Vector3 initialPos1 = Quaternion.Euler(0, 0, angleOffset) * basePosition;
            Vector3 initialPos2 = Quaternion.Euler(0, 0, -angleOffset) * basePosition;

            // Calculate convergence towards the base position
            float convergence = Mathf.Lerp(0, 1, holdTime / holdTimeToMaxAccuracy);
            Vector3 finalPos1 = Vector3.Lerp(initialPos1, basePosition, convergence);
            Vector3 finalPos2 = Vector3.Lerp(initialPos2, basePosition, convergence);

            aimingCircle1.position = transform.position + finalPos1;
            aimingCircle2.position = transform.position + finalPos2;
        }
        else
        {
            holdTime = 0f;  // Reset hold time on mouse release
            circleRenderer.enabled = false;
            directionRenderer.enabled = false;
        }
        if(Input.GetMouseButtonUp(0) && currentNumberofArrows > 0)
        {
            currentNumberofArrows--;
            Shoot(direction,playerIDnumber);
        }
    }
    
    void Shoot(Vector3 direction, int playerIDnumber)
    {
        
        // Get positions relative to the player
        Vector3 pos1 = aimingCircle1.position - transform.position;
        Vector3 pos2 = aimingCircle2.position - transform.position;

        // Generate a random factor between 0 and 1 to pick a direction between pos1 and pos2
        float randomFactor = UnityEngine.Random.Range(0f, 1f);
        Vector3 shootDirection = Vector3.Lerp(pos1, pos2, randomFactor);
        
        // Normalize to ensure consistent force is applied
        shootDirection.Normalize();
        //Set arrowLayerName to the ArrowX ID
        int arrowLayerNumber = playerIDnumber + 5;
        
        // Create the arrow and apply force
        GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
        
        //assign the Arrow to the arrow
        arrow.layer = arrowLayerNumber;
        
        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        arrowRb.AddForce(shootDirection * 25f, ForceMode2D.Impulse); // Use a constant force magnitude
    }

    void FixedUpdate()
    {
        Move();
        if (jumpRequest)
        {
            Jump();
            jumpRequest = false;
        }
        if (dashDirection != 0)
        {
            Dash(dashDirection);
            dashDirection = 0;
        }
    }
// handling arrow up
    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.CompareTag("Arrow") && (currentNumberofArrows < maxNumberofArrows))
        {
            HandleArrowPickUp(col.gameObject);
        }
    }

    void HandleArrowPickUp(GameObject arrow)
    {
        ArrowScript arrowScript = arrow.GetComponent<ArrowScript>();
        if(arrowScript.isStuck == true)
        {
            Destroy(arrow);
            currentNumberofArrows++;
        }
        Debug.Log("Arrow Picked Up");
    }
    void Move()
    {
        float moveHorizontal = Input.GetAxis("Horizontal") * speed;
        rb.velocity = new Vector2(moveHorizontal, rb.velocity.y);
    }

    void Jump()
    {
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        isGrounded = false;
    }

    void Dash(int direction)
    {
        Vector2 dashForce = new Vector2(dashSpeed * direction, 0);
        rb.AddForce(dashForce, ForceMode2D.Impulse);
        lastDashTime = Time.time;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
        // Check if the collision normal indicates that the player is landing on a horizontal surface
            foreach (ContactPoint2D contact in col.contacts)
            {
                if (contact.normal.y > 0.5f) // Adjust this threshold if needed
                {
                    Debug.Log(contact + "normal.y > 0.5");
                    isGrounded = true;
                    return;
                }
            }
            foreach (ContactPoint2D contact in col.contacts)
            {
                Debug.DrawRay(contact.point, contact.normal, Color.red, 1f);
            }
        }
    }


    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
