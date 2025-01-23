using UnityEngine;
using System.Collections;

public class Slime : Enemy
{
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Transform[] patrolPoints;
    
    private int currentPatrolIndex = 0;
    private bool isChasing = false;
    private float originalScaleX;
    private Transform playerTransform;
    private Animator animator;

    // Get the leftmost and rightmost patrol points for boundary checking
    private float leftBoundary;
    private float rightBoundary;

    protected override void Start()
    {
        base.Start();
        originalScaleX = Mathf.Abs(transform.localScale.x);
        playerTransform = PlayerController.Instance.transform;
        animator = GetComponent<Animator>();

        if (patrolPoints.Length < 2)
        {
            Debug.LogWarning("Slime needs at least 2 patrol points to function properly!");
        }

        // Set patrol boundaries
        SetPatrolBoundaries();
    }

    private void SetPatrolBoundaries()
    {
        if (patrolPoints == null || patrolPoints.Length < 2) return;

        leftBoundary = patrolPoints[0].position.x;
        rightBoundary = patrolPoints[0].position.x;

        // Find the leftmost and rightmost points
        foreach (Transform point in patrolPoints)
        {
            if (point.position.x < leftBoundary) leftBoundary = point.position.x;
            if (point.position.x > rightBoundary) rightBoundary = point.position.x;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isRecoiling) return;

        // Check if player is in range
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            isChasing = distanceToPlayer < detectionRange;
            
            // Update animation state
            animator.SetBool("isChasing", isChasing);
        }

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void ChasePlayer()
    {
        if (playerTransform == null) return;

        // Calculate direction to player
        float directionToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
        
        // Calculate target X position
        float targetX = transform.position.x + (directionToPlayer * chaseSpeed * Time.deltaTime);

        // Check if target position is within boundaries
        if (targetX >= leftBoundary && targetX <= rightBoundary)
        {
            rb.velocity = new Vector2(directionToPlayer * chaseSpeed, rb.velocity.y);
            transform.localScale = new Vector2(directionToPlayer * originalScaleX, transform.localScale.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length < 2) return;

        Vector2 targetPosition = patrolPoints[currentPatrolIndex].position;
        float distanceToTarget = Vector2.Distance(new Vector2(transform.position.x, 0), 
                                                new Vector2(targetPosition.x, 0));

        // Calculate direction to target
        float directionToTarget = Mathf.Sign(targetPosition.x - transform.position.x);
        
        // Move towards target
        rb.velocity = new Vector2(directionToTarget * patrolSpeed, rb.velocity.y);
        
        // Update facing direction while maintaining original scale
        transform.localScale = new Vector2(directionToTarget * originalScaleX, transform.localScale.y);

        // Check if we've reached the current patrol point
        if (distanceToTarget < 0.1f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        // Update animation
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    public override void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        base.EnemyHit(_damageDone, _hitDirection, _hitForce);
        
        if (health <= 0)
        {
            // Trigger death animation
            animator.SetTrigger("Death");
            
            // Wait for animation to finish before handling death
            float deathAnimationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            StartCoroutine(HandleDeathAfterAnimation(deathAnimationLength));
        }
    }

    private IEnumerator HandleDeathAfterAnimation(float delay)
    {
        // Wait for death animation to finish
        yield return new WaitForSeconds(delay);
        
        // Then start the death handling process
        StartCoroutine(HandleDeath());
    }

    private IEnumerator HandleDeath()
    {
        // Disable movement and collisions but keep sprite visible
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;  // Prevent physics from affecting the body
        GetComponent<Collider2D>().enabled = false;
        
        // Disable the script's update logic but keep the GameObject
        this.enabled = false;
        
        // Optional: Disable any other components that might affect the slime
        if (rb != null) rb.simulated = false;
        
        // Wait for a few seconds before destroying
        yield return new WaitForSeconds(2f); // Adjust this time as needed
        
        // Destroy the game object after delay
        Destroy(gameObject);
    }

    // Optional: Visualize detection range and patrol path in editor
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw patrol path
        if (patrolPoints != null && patrolPoints.Length >= 2)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
            // Connect last point to first point
            if (patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
            }
        }
    }
}