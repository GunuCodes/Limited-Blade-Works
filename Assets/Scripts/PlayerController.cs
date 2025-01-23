using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Main player controller that handles movement, combat, and player states.
public class PlayerController : MonoBehaviour
{
    // Movement speed when walking.
    [SerializeField] private float walkSpeed = 1;

    // Settings for dodge roll ability.
    [SerializeField] private float dodgeRollSpeed;
    [SerializeField] private float dodgeRollTime;
    [SerializeField] private float dodgeRollCooldown;
    [SerializeField] GameObject DodgerollFX;
    [Space(5)]

    // Vertical jump settings and coyote time for delayed jumps.
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float jumpReleaseMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    // Health system and auto-healing settings.
    public int health;
    public int maxHealth;
    [SerializeField] private float healTimer = 3f;  // Time needed to heal 1 heart
    [SerializeField] private float currentHealTimer = 0f;
    private bool isHealing = false;

    // Add this to track which heart is currently regenerating
    public int regeneratingHeartIndex { get; private set; } = -1;
    public float healProgress { get; private set; } = 0f;

    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;
    [Space(5)]

    // Combat settings for player attacks.
    [SerializeField] private Transform AttackTransform; //the middle of the side attack area
    [SerializeField] private Vector2 AttackArea; //how large the area of side attack is

    [SerializeField] private float damage; //the damage the player does to an enemy
    [SerializeField] private float timeBetweenAttack;

    [SerializeField] private LayerMask attackableLayer; //the layer the player can attack and recoil off of
    private float timeSinceAttack;

    // Settings for attack movement and air control.
    [SerializeField] private float attackMovementSlowdown = 0.5f; // How much to slow down during attacks
    [SerializeField] private float airAttackStallDuration = 0.4f; // How long to stall in air
    [SerializeField] private float airAttackGravityMultiplier = 0.3f; // Reduced gravity during air attack

    // Recoil settings when hitting enemies.
    [SerializeField] private float recoilXSpeed = 100f;
    [SerializeField] private float recoilYSpeed = 100f;
    private int stepsXRecoiled = 0;
    private int stepsYRecoiled = 0;
    [SerializeField] private int recoilXSteps = 5;
    [SerializeField] private int recoilYSteps = 5;

    [SerializeField] private float recoilDelay = 0.1f; // Delay before recoil starts (in seconds)
    private bool isRecoilDelayed = false;

    //References
    [HideInInspector] public PlayerStateList pState;

    Rigidbody2D rb;

    private float xAxis;

    private float gravity;

    private Animator anim;

    private bool canDodgeRoll = true;

    private bool DodgeRolled;

    private bool attack = false;

    bool restoreTime;

    float restoreTimeSpeed;

    // Ground detection settings.
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    public static PlayerController Instance;

    private bool isAttacking = false; // New flag to track if the player is attacking
    private bool isAirAttacking = false;  // New flag specifically for air attacks

    // Hit stop settings for impact freeze frames.
    [SerializeField] private bool useHitStop = true;
    [SerializeField] private float hitStopTimeScale = 0f;
    [SerializeField] private float hitStopDuration = 0.5f;
    [SerializeField] private int hitStopRestoreSpeed = 5;

    private HitEffect hitEffect;

    // Audio clips for player actions.
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip dodgeRollSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hurtSound;

    private float landSoundCooldown = 0.5f; // Adjust this value to change the minimum time between land sounds
    private float lastLandSoundTime = -1f;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // Set initial health and ensure player is alive
        pState = GetComponent<PlayerStateList>();
        pState.alive = true;  // Explicitly set alive state
        Health = maxHealth;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hitEffect = GetComponent<HitEffect>();
        gravity = rb.gravityScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(AttackTransform.position, AttackArea);

    }

    void Update()
    {
        if (GameManager.Instance.gameIsPaused) return;

        if (pState.cutscene) return;

        if (pState.alive)
        {
            GetInputs();
        }

        if (pState.recoilingX || pState.recoilingY)
        {
            Recoil();
            return;
        }

        if (pState.alive)
        {
            Move();
            Jump();
            Flip();
            Attack();
            StartDodgeRoll();
            RestoreTimeScale();
        }

        // Handle heart regeneration
        if (!pState.invincible && health < maxHealth && !isHealing)
        {
            currentHealTimer += Time.deltaTime;
            if (currentHealTimer >= healTimer)
            {
                StartHealing();
            }
        }
        
        // Update healing progress
        if (isHealing)
        {
            healProgress += Time.deltaTime / healTimer;
            if (healProgress >= 1f)
            {
                Health++;
                healProgress = 0f;
                regeneratingHeartIndex = -1;
                
                if (Health >= maxHealth)
                {
                    StopHealing();
                }
                else
                {
                    // Start healing next heart
                    regeneratingHeartIndex = Health;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (pState.cutscene) return;

        if (pState.DodgeRolling) return;
        {
            anim.SetFloat("xVelocity", Math.Abs(rb.velocity.x));
        }
    }

    void GetInputs()
    {
        // Only get inputs if dialogue is not active
        if (!DialogueManager.Instance.isDialogueActive)
        {
            xAxis = Input.GetAxisRaw("Horizontal");
            attack = Input.GetMouseButtonDown(0);
        }
        else
        {
            // Reset inputs when dialogue is active
            xAxis = 0;
            attack = false;
            // Stop any movement
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void Flip()
    {
        // Don't flip if attacking while in air or on ground
        if (isAttacking) return;
        
        // Don't flip during dodge roll
        if (pState.DodgeRolling) return;

        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
            pState.lookingRight = false; 
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
            pState.lookingRight = true; 
        }
    }

    public void TakeDamage(float _damage)
    {
        if (!pState.alive || pState.invincible) return;  // Don't take damage if dead or invincible
        
        // Play hurt sound
        if (audioSource != null && hurtSound != null)
        {
            PlaySound(hurtSound);
        }
        
        Health -= Mathf.RoundToInt(_damage);
        hitEffect.OnHit();
        
        if (Health <= 0)
        {
            Health = 0;
            StartCoroutine(Death());
        }
        else
        {
            if (useHitStop)
            {
                HitStopTime(hitStopTimeScale, hitStopRestoreSpeed, hitStopDuration);
            }
            StartCoroutine(StopTakingDamage());
            
            // Reset heal timer when taking damage
            currentHealTimer = 0f;
            isHealing = false;
        }
    }

    public int Health
    {
        get { return health; }
        set
        {
            if (health != value)    
            {
                health = Mathf.Clamp(value, 0, maxHealth);

                if (onHealthChangedCallback != null)
                {
                    onHealthChangedCallback.Invoke();
                }
            }
        }
    }

    IEnumerator StopTakingDamage()
    {
        pState.invincible = true;
        anim.SetTrigger("TakeDamage");
        hitEffect.StartInvincibilityFlash();
        yield return new WaitForSeconds(1f);
        pState.invincible = false;
    }   

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
    {
        restoreTimeSpeed = _restoreSpeed;

        if (_delay > 0)
        {
            StopCoroutine(StartTimeAgain(_delay));
            StartCoroutine(StartTimeAgain(_delay));
        }
        else
        {
            restoreTime = true;
        }
        Time.timeScale = _newTimeScale;
    }
    
    IEnumerator StartTimeAgain(float _delay)
    {
        yield return new WaitForSecondsRealtime(_delay);
        restoreTime = true;
    }

    void RestoreTimeScale()
    {
        if (restoreTime)
        {
            if (Time.timeScale < 1)
            {
                Time.timeScale += Time.unscaledDeltaTime * restoreTimeSpeed;
            }
            else
            {
                Time.timeScale = 1;
                restoreTime = false;
            }
        }
    }

    // Handles basic movement and applies attack slowdown.
    void Move()
    {
        float currentSpeed = walkSpeed;
        float currentXAxis = xAxis;
        
        // Apply movement slowdown during attacks
        if (isAttacking)
        {
            currentSpeed *= attackMovementSlowdown;
            
            // Prevent horizontal movement during air attacks
            if (isAirAttacking)
            {
                currentXAxis = 0;
            }
        }

        rb.velocity = new Vector2(currentSpeed * currentXAxis, rb.velocity.y);
    }

    public bool CheckGrounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        return false;
    }

    // Initiates dodge roll movement and invincibility.
    void StartDodgeRoll()
    {
        if (Input.GetButtonDown("DodgeRoll") && canDodgeRoll && !DodgeRolled)
        {
            StartCoroutine(DodgeRoll());
            DodgeRolled = true;
        }

        if (CheckGrounded())
        {
            DodgeRolled = false;
        }
    }

    IEnumerator DodgeRoll()
    {
        canDodgeRoll = false;
        pState.DodgeRolling = true;
        
        // Play dodge roll sound
        if (audioSource != null && dodgeRollSound != null)
        {
            PlaySound(dodgeRollSound);
        }
        
        // Reset other animation bools to ensure clean transition
        anim.SetBool("isJumping", false);
        anim.SetBool("isFalling", false);
        
        // Trigger the dodge roll animation
        anim.SetTrigger("Dodgeroll");
        if (CheckGrounded()) Instantiate(DodgerollFX, transform);
        
        float elapsedTime = 0f;
        float initialSpeed = dodgeRollSpeed;
        
        // Get the direction from input rather than transform scale when attacking
        float rollDirection = isAttacking ? Mathf.Sign(xAxis) : Mathf.Sign(transform.localScale.x);
        // If no direction is held during attack, use the facing direction
        if (isAttacking && xAxis == 0) rollDirection = Mathf.Sign(transform.localScale.x);
        
        // Update scale to match roll direction
        transform.localScale = new Vector2(rollDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y);
        
        // Phase 1: Initial burst with no gravity (first 40% of the dodge roll)
        float initialBurstDuration = dodgeRollTime * 0.3f;
        rb.gravityScale = 0;
        
        while (elapsedTime < initialBurstDuration)
        {
            elapsedTime += Time.deltaTime;
            float speedMultiplier = Mathf.Lerp(1f, 0.6f, elapsedTime / initialBurstDuration);
            rb.velocity = new Vector2(rollDirection * initialSpeed * speedMultiplier, 0);
            yield return null;
        }
        
        // Phase 2: Decay with gravity (remaining 60% of the dodge roll)
        rb.gravityScale = gravity;
        
        while (elapsedTime < dodgeRollTime)
        {
            elapsedTime += Time.deltaTime;
            float speedMultiplier = Mathf.Lerp(0.6f, 0.1f, (elapsedTime - initialBurstDuration) / (dodgeRollTime - initialBurstDuration));
            speedMultiplier = Mathf.Pow(speedMultiplier, 2f);
            
            // Keep horizontal velocity decaying but allow downward gravity
            float currentYVelocity = rb.velocity.y;
            rb.velocity = new Vector2(rollDirection * initialSpeed * speedMultiplier, 
                                    Mathf.Min(currentYVelocity, 0)); // Only allow zero or negative Y velocity
            
            yield return null;
        }
        
        // Restore normal movement
        rb.gravityScale = gravity;
        pState.DodgeRolling = false;
        pState.jumping = false; // Reset jumping state
        
        // Reset animation states based on current condition
        if (!CheckGrounded())
        {
            if (rb.velocity.y > 0)
            {
                anim.SetBool("isJumping", true);
            }
            else
            {
                anim.SetBool("isFalling", true);
            }
        }
        
        yield return new WaitForSeconds(dodgeRollCooldown);
        canDodgeRoll = true;
    }

    // Handles attack execution and hit detection.
    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if(attack && timeSinceAttack >= timeBetweenAttack)
        {
            // Play attack sound
            if (audioSource != null && attackSound != null)
            {
                PlaySound(attackSound);
            }

            isAttacking = true;
            isAirAttacking = !CheckGrounded();
            
            // Store the direction the player is facing when attack starts
            float attackDirection = transform.localScale.x;
            transform.localScale = new Vector2(attackDirection, transform.localScale.y);
            
            Hit(AttackTransform, AttackArea);
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");

            if (isAirAttacking)
            {
                StartCoroutine(AirAttackStall());
            }

            StartCoroutine(EndAttack());
        }
    }

    IEnumerator AirAttackStall()
    {
        float originalGravity = rb.gravityScale;
        rb.gravityScale = gravity * airAttackGravityMultiplier;
        
        // Initial vertical velocity reduction
        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        
        yield return new WaitForSeconds(airAttackStallDuration);
        
        rb.gravityScale = originalGravity;
    }

    IEnumerator EndAttack()
    {
        float attackDuration = anim.GetCurrentAnimatorStateInfo(0).length;
        // Allow dodge roll sooner (e.g., after 50% of the attack animation)
        yield return new WaitForSeconds(attackDuration * 0.5f);
        
        isAttacking = false;
        isAirAttacking = false;
    }

    void Hit(Transform _attackTransform, Vector2 _attackArea)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);
        List<Enemy> hitEnemies = new List<Enemy>();

        if(objectsToHit.Length > 0)
        {
            StartCoroutine(DelayedRecoil());
        } 

        for(int i = 0; i < objectsToHit.Length; i++)
        {
            Enemy e = objectsToHit[i].GetComponent<Enemy>();
            if(e && !hitEnemies.Contains(e))
            {
                e.EnemyHit(damage, (transform.position - objectsToHit[i].transform.position).normalized, 20);
                hitEnemies.Add(e);
            }
        }
    }

    IEnumerator DelayedRecoil()
    {
        isRecoilDelayed = true;
        yield return new WaitForSeconds(recoilDelay);
        pState.recoilingX = true;
        isRecoilDelayed = false;
    }

    void Recoil()
    {
        // Don't apply recoil forces during the delay
        if (isRecoilDelayed) return;

        if(pState.recoilingX)
        {
            if(pState.lookingRight)
            {
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if(pState.recoilingY)
        {
            rb.gravityScale = 0;
            // Always recoil upward when hit
            rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
        }
        else
        {
            rb.gravityScale = gravity;
        }

        //stop recoil
        if(pState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }
        if (pState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
        }
        else
        {
            StopRecoilY();
        }

        if(CheckGrounded())
        {
            StopRecoilY();
        } 
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }
    
    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    } 

    // Handles jump execution, coyote time, and variable jump height.
    void Jump()
    {
        // Handle coyote time
        if (CheckGrounded())
        {
            coyoteTimeCounter = coyoteTime;
            // Reset jumping state when grounded to fix the early landing bug
            pState.jumping = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Handle jump release (variable jump height)
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpReleaseMultiplier);
            pState.jumping = false;
        }

        // Handle jump execution with coyote time
        if (!pState.jumping)
        {
            if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
                pState.jumping = true;
                coyoteTimeCounter = 0; // Reset coyote time when jump is used
                
                // Play jump sound
                if (audioSource != null && jumpSound != null)
                {
                    PlaySound(jumpSound);
                }
            }
        }

        // Animation states
        if (!CheckGrounded())
        {
            anim.SetFloat("yVelocity", Mathf.Clamp(rb.velocity.y, -1f, 1f));
            
            if (rb.velocity.y > 0)
            {
                anim.SetBool("isJumping", true);
                anim.SetBool("isFalling", false);
            }
            else
            {
                anim.SetBool("isJumping", false);
                anim.SetBool("isFalling", true);
            }
        }
        else
        {
            anim.SetBool("isJumping", false);
            anim.SetBool("isFalling", false);
            anim.SetFloat("yVelocity", 0f);

            if (rb.velocity.y < -0.1f && 
                audioSource != null && 
                landSound != null && 
                Time.time - lastLandSoundTime >= landSoundCooldown)
            {
                PlaySound(landSound);
                lastLandSoundTime = Time.time;
            }
        }
    }

    private void StopHealing()
    {
        isHealing = false;
        currentHealTimer = 0f;
        regeneratingHeartIndex = -1;
        healProgress = 0f;
    }

    // Initiates automatic health regeneration.
    private void StartHealing()
    {
        isHealing = true;
        healProgress = 0f;
        regeneratingHeartIndex = Health;
        currentHealTimer = 0f;
    }

    IEnumerator Death()
    {
        if (!pState.alive) yield break;  // Prevent multiple death coroutines
        
        pState.alive = false;
        Time.timeScale = 1f;
        anim.SetTrigger("Death");
        
        // Disable player movement/input by zeroing velocity
        rb.velocity = Vector2.zero;
        
        yield return new WaitForSeconds(1f);
        StartCoroutine(UIManager.Instance.ActivateDeathScreen());
    }

    // Handles player movement during scene transitions.
    public IEnumerator WalkIntoNewScene(Vector2 _exitDir, float _delay)
    {
        if (pState == null || rb == null)
        {
            Debug.LogError("PlayerController not properly initialized!");
            yield break;
        }

        // Set player state to prevent unwanted interactions during transition
        pState.cutscene = true;
        pState.invincible = true;

        //If exit direction is upwards
        if(_exitDir.y > 0)
        {
            rb.velocity = jumpForce * _exitDir;
        }

        //If exit direction requires horizontal movement
        if(_exitDir.x != 0)
        {
            xAxis = _exitDir.x > 0 ? 1 : -1;
            Move();
        }

        Flip();
        yield return new WaitForSeconds(_delay);
        
        if (pState != null)
        {
            pState.cutscene = false;
            pState.invincible = false;  // Make sure to disable invincibility after transition
        }
    }

    public void TakeHazardDamage(float damage)
    {
        if (!pState.alive || pState.invincible) return;

        Health -= Mathf.RoundToInt(damage);
        hitEffect.OnHit();
        PlaySound(hurtSound);
        
        // Reset heal timer and healing state
        currentHealTimer = 0f;
        isHealing = false;
        regeneratingHeartIndex = -1;
        healProgress = 0f;

        if (Health <= 0)
        {
            Health = 0;
            StartCoroutine(Death());
        }
        else
        {
            StartCoroutine(HazardHit());
        }
    }

    public void InstantDeath()
    {
        if (!pState.alive || pState.invincible) return;
        
        Health = 0;
        StartCoroutine(Death());
    }

    private IEnumerator HazardHit()
    {
        pState.invincible = true;
        rb.velocity = Vector2.zero; // Stop all movement
        
        // Get current scene name
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Check if there's a checkpoint to respawn at
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint(currentScene))
        {
            // Fade out
            if (UIManager.Instance?.sceneFader != null)
            {
                yield return StartCoroutine(UIManager.Instance.sceneFader.Fade(SceneFader.FadeDirection.In));
            }
            
            // Move player to checkpoint
            transform.position = CheckpointManager.Instance.GetCheckpoint(currentScene);
            
            // Reset player state
            rb.velocity = Vector2.zero;
            
            // Fade back in
            if (UIManager.Instance?.sceneFader != null)
            {
                yield return StartCoroutine(UIManager.Instance.sceneFader.Fade(SceneFader.FadeDirection.Out));
            }
        }
        
        // Give brief invincibility after respawn
        yield return new WaitForSeconds(1f);
        pState.invincible = false;
    }

    // Optional: Add this method to play sounds with slight pitch variation
    private void PlaySound(AudioClip clip, float baseVolume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            float randomPitch = UnityEngine.Random.Range(0.95f, 1.05f);
            audioSource.pitch = randomPitch;
            audioSource.PlayOneShot(clip, baseVolume);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh components and references
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hitEffect = GetComponent<HitEffect>();
        pState = GetComponent<PlayerStateList>();

        // Reset states
        isAttacking = false;
        isAirAttacking = false;
        pState.invincible = false;  // Make sure invincibility is reset
        
        // Ensure proper layer and tag are set
        gameObject.layer = LayerMask.NameToLayer("Player");
        gameObject.tag = "Player";

        // Reset any ongoing coroutines that might interfere with state
        StopAllCoroutines();
    }
}