using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 2f;
    public float chaseRange = 5f;
    public float attackRange = 1f;
    public float attackDamage = 20;
    public float attackDelay = 1.5f;

    public float roamRadius = 3f;
    public float roamDelay = 3f;
    public float outOfRangeTime = 2f; // Time before enemy starts roaming again if the player is out of range

    private Animator animator;
    private GameObject player;
    private Vector2 roamPosition;

    private float attackTimer = 0f;
    private float roamTimer = 0f;
    private float playerOutOfRangeTimer = 0f;
    private bool isRoaming = true;
    private bool isFacingRight = false;

    private PlayerHP playerHP;
    private Vector2 startPosition;

    private bool isStunned = false; // Tracks if the enemy is stunned
    private float stunDuration = 1f; // Duration of the stun effect

    private bool isIdle = false;

    private KnockbackManager knockbackManager;

    public float jumpForce = 5f; // Force of the jump
    public float raycastDistance = 1f; // Distance to check for obstacles in front of the enemy

    private Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHP = player.GetComponent<PlayerHP>();
            knockbackManager = player.GetComponent<KnockbackManager>();
        }
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>(); // Cache Rigidbody2D reference
        SetNewRoamPosition();
    }

    void Update()
    {
        if (isStunned)
        {
            return; // If stunned, do not process movement or attacking
        }

        if (player == null || playerHP == null || playerHP.IsDead()) // Check if player is dead
        {
            StopMovement();
            return; // Stop all behavior if the player is dead
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= attackRange)
        {
            Attack();
            playerOutOfRangeTimer = 0f; // Reset the timer when in range
        }
        else if (distanceToPlayer <= chaseRange)
        {
            ChasePlayer();
            playerOutOfRangeTimer = 0f; // Reset the timer when in range
        }
        else
        {
            playerOutOfRangeTimer += Time.deltaTime;
            if (playerOutOfRangeTimer >= outOfRangeTime)
            {
                Roam(); // Start roaming if the player has been out of range for the set time
            }
        }

        attackTimer += Time.deltaTime;

        // Check for obstacles and jump over them if necessary
        CheckForObstacles();
    }

    private void ChasePlayer()
    {
        isRoaming = false;
        isIdle = false;
        Vector2 direction = (player.transform.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);

        FlipSprite(direction);

        animator.SetFloat("xVelocity", Mathf.Abs(direction.x));
        animator.SetBool("isRunning", true);
        animator.SetBool("isAttacking", false);
    }

    private void Roam()
    {
        // If currently idle, just stay idle
        if (isIdle)
        {
            animator.SetBool("isRunning", false);
            return; // Exit if currently idle
        }

        if (isRoaming)
        {
            if (Vector2.Distance(transform.position, roamPosition) > 0.1f)
            {
                Vector2 direction = (roamPosition - (Vector2)transform.position).normalized;
                transform.position = Vector2.MoveTowards(transform.position, roamPosition, speed * Time.deltaTime);

                FlipSprite(direction);

                animator.SetFloat("xVelocity", Mathf.Abs(direction.x));
                animator.SetBool("isRunning", true);
            }
            else
            {
                // When reaching the roam position, start idle coroutine
                StartCoroutine(IdleCoroutine());
            }
        }

        // Manage roam timer to determine when to set a new roam position
        roamTimer += Time.deltaTime;

        // Random chance to go idle based on roamTimer
        if (roamTimer >= Random.Range(3f, 5f) && !isIdle) // Random range for idle
        {
            StartCoroutine(IdleCoroutine());
        }
    }

    private IEnumerator IdleCoroutine()
    {
        isIdle = true; // Set to idle
        animator.SetBool("isIdle", true); // Trigger idle animation

        yield return new WaitForSeconds(Random.Range(1f, 3f)); // Random idle duration

        isIdle = false; // End idle state
        animator.SetBool("isIdle", false); // Reset idle animation

        SetNewRoamPosition(); // Decide the next roam position
    }

    private void SetNewRoamPosition()
    {
        roamPosition = startPosition + new Vector2(
            Random.Range(-roamRadius, roamRadius),
            Random.Range(-roamRadius, roamRadius)
        );

        roamTimer = 0f; // Reset roam timer for next roam decision
    }

    private void Attack()
    {
        if (attackTimer >= attackDelay)
        {
            if (animator.GetBool("isReady"))
            {
                animator.SetBool("isReady", true);
                StartCoroutine(PrepareAttackCoroutine());
            }
            else
            {
                PerformAttack();
            }

            attackTimer = 0f;
        }

        animator.SetBool("isRunning", false);
        animator.SetBool("isAttacking", true);
    }

    private IEnumerator PrepareAttackCoroutine()
    {
        yield return new WaitForSeconds(0.5f); // Time for preparation animation
        PerformAttack();
    }

    private void PerformAttack()
    {
        animator.SetTrigger("Attack");
        playerHP.TakeDMG(Mathf.RoundToInt(attackDamage), this.gameObject);

        if (knockbackManager != null)
        {
            knockbackManager.PlayFeedback(gameObject);
        }
    }

    private void StopMovement()
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isAttacking", false);
    }

    private void FlipSprite(Vector2 direction)
    {
        if (direction.x > 0 && !isFacingRight || direction.x < 0 && isFacingRight)
        {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }

    private void CheckForObstacles()
    {
        // Cast a ray to detect obstacles in front of the enemy
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, raycastDistance);
        if (hit.collider != null && hit.collider.CompareTag("Obstacle"))
        {
            JumpOverObstacle();
        }
    }

    private void JumpOverObstacle()
    {
        // Handle the jump logic without animation
        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0); // Reset vertical velocity first to avoid compound jumps
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse); // Apply jump force to Rigidbody2D
        }
    }

    private IEnumerator StunCoroutine()
    {
        isStunned = true;
        animator.SetTrigger("Hurt");
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    public void TakeDamage(int damage)
    {
        StartCoroutine(StunCoroutine());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // Attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);  // Chase range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, roamRadius);  // Roam radius
    }
}