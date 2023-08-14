using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerAnimation), typeof(PlayerAttack), typeof(Health))]
public class PlayerMove : MonoBehaviour
{
    [Tooltip("Velocidade do movimento")]
    [SerializeField] float speed = 2f;
    [Tooltip("Altura máxima alcançada com o pulo")]
    [SerializeField] float jumpHeight = 5f;
    [Tooltip("Velocidade da esquiva")]
    [SerializeField] float dashSpeed = 8f;
    [Tooltip("Tempo da duração da esquiva")]
    [SerializeField] float delayDash = .5f;

    public bool IsMoving { get { return componentRun.HorizontalMove != 0; } }
    public bool IsDashing { get { return componentDash.IsDashing; } }
    public bool IsJumping { get { return componentJump.IsJumping; } }
    public bool IsFlipped { get { return componentFlip.IsFlipped; } }
    
    PlayerAnimation playerAnimation;
    PlayerAttack playerAttack;
    Health health;
    ComponentRun componentRun;
    ComponentDash componentDash;
    ComponentJump componentJump;
    ComponentFlip componentFlip;
    bool isFalling;
    bool isGrabbing;
    float gravityScale;
    Rigidbody2D rb2D;

    #region Classes Components Of PlayerMove

    class ComponentRun
    {
        public float HorizontalMove { get { return horizontalMove; } }

        float horizontalMove;
        readonly PlayerAttack playerAttack;
        readonly PlayerAnimation playerAnimation;
        Rigidbody2D rb2D;

        public ComponentRun(Rigidbody2D rb2D, PlayerAttack playerAttack, PlayerAnimation playerAnimation)
        {
            this.rb2D = rb2D;
            this.playerAttack = playerAttack;
            this.playerAnimation = playerAnimation;
        }

        public void PopulateHorizontalMove()
        {
            if (playerAttack.IsAttacking())
                horizontalMove = 0;
            else
                horizontalMove = ReturnIntHorizontalAxis();
        }

        int ReturnIntHorizontalAxis()
        {
            var getAxisRaw = Input.GetAxisRaw("Horizontal");
            if (getAxisRaw > 0)
                return 1;
            else if (getAxisRaw < 0)
                return -1;
            else
                return 0;
        }

        public void Execute(float speed)
        {
            playerAnimation.SetRun(horizontalMove != 0);
            
            var newPosition = new Vector2(horizontalMove * speed, rb2D.velocity.y);
            rb2D.velocity = newPosition;
        }

        public void ExecuteDash(float speed)
        {
            var newPosition = new Vector2(speed, rb2D.velocity.y);
            rb2D.velocity = newPosition;
        }

        public void Stop()
        {
            horizontalMove = 0;
        }
    }

    class ComponentDash
    {
        public bool IsDashing { get { return isDashing; } }
        public float HorizontalMove { get { return horizontalMove; } }

        bool isDashing;
        float horizontalMove;
        readonly PlayerAnimation playerAnimation;

        public ComponentDash(PlayerAnimation playerAnimation)
        {
            this.playerAnimation = playerAnimation;
        }

        public void Execute(float horizontalMove)
        {
            this.horizontalMove = horizontalMove;
            isDashing = true;
            playerAnimation.TriggerDash();
        }

        public void Stop()
        {
            isDashing = false;
        }
    }

    class ComponentJump
    {
        public bool IsJumping { get { return isJumping; } }

        bool jumpTriggered;
        bool isJumping;
        Rigidbody2D rb2D;
        readonly PlayerAnimation playerAnimation;

        public ComponentJump(Rigidbody2D rb2D, PlayerAnimation playerAnimation)
        {
            this.playerAnimation = playerAnimation;
            this.rb2D = rb2D;
        }

        public void TriggerJump()
        {
            if (Input.GetButtonDown("Jump") && !isJumping)
                jumpTriggered = true;
        }

        public void Execute(float jumpHeight)
        {
            if (!jumpTriggered || isJumping)
                return;

            playerAnimation.SetJump(true);
            isJumping = true;
            jumpTriggered = false;
            
            rb2D.velocity = Vector2.up * jumpHeight;
        }

        public void Stop()
        {
            isJumping = false;
            playerAnimation.SetJump(false);
        }
    }

    class ComponentFlip
    {
        public bool IsFlipped { get { return isFlipped; } }

        bool isFlipped;

        public void Execute(Transform transform, float horizontalMove)
        {
            if (!(horizontalMove < 0 && !isFlipped || horizontalMove > 0 && isFlipped))
                return;
            
            isFlipped = !isFlipped;
            var localScale = transform.localScale;
            var xPosition = transform.position.x;
            localScale.x *= -1;
            xPosition += localScale.x;
            transform.position = new Vector2(xPosition, transform.position.y);
            transform.localScale = localScale;
        }
    }
   
    #endregion Classes Components Of PlayerMove

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        playerAttack = GetComponent<PlayerAttack>();
        playerAnimation = GetComponent<PlayerAnimation>();

        componentRun = new ComponentRun(rb2D, playerAttack, playerAnimation);
        componentDash = new ComponentDash(playerAnimation);
        componentJump  = new ComponentJump(rb2D, playerAnimation);
        componentFlip  = new ComponentFlip();

        gravityScale = rb2D.gravityScale;
        
        health = GetComponent<Health>();

        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        while (true)
        {
            if (Input.GetButton("Fire2") && IsMoving && !IsDashing)
            {
                componentDash.Execute(componentRun.HorizontalMove);

                yield return new WaitForSeconds(delayDash);

                componentDash.Stop();
            }

            yield return new WaitForEndOfFrame();
        }
    }

    void Update()
    {
        if (PlayerHasToStop())
        {
            StopRun();
            return;
        }

        if (componentDash.IsDashing)
            return;

        if (!isGrabbing)
            componentRun.PopulateHorizontalMove();

        if (!isFalling || isGrabbing)
            componentJump.TriggerJump();

        componentFlip.Execute(transform, componentRun.HorizontalMove);
    }

    bool PlayerHasToStop()
    {
        return playerAttack.IsAttacking() || health.IsHurting || health.IsDead();
    }

    void FixedUpdate()
    {
        if (componentDash.IsDashing)
            componentRun.ExecuteDash(dashSpeed * componentDash.HorizontalMove);
        else
            componentRun.Execute(speed);
            
        componentJump.Execute(jumpHeight);

        if (componentJump.IsJumping)
            SetGrab(false);
    }

    public void StopRun()
    {
        componentRun.Stop();
    }

    public void StopJump()
    {
        componentJump.Stop();
    }

    public void SetFall(bool active)
    {
        isFalling = active;
        playerAnimation.SetFall(active);
    }

    public void SetGrab(bool active)
    {
        isGrabbing = active;
        playerAnimation.SetGrab(active);
        rb2D.gravityScale = active ? 0 : gravityScale;
        if (active)
        rb2D.velocity = new Vector2(0, 0);
    }
}
