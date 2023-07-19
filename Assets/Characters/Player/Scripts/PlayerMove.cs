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
    [Tooltip("Objeto que irá checar o ponto de colisão com o chão (Ele deve estar posicionado no ponto que o personagem irá se chocar com o chão)")]
    [SerializeField] GameObject groundPoint;
    [Tooltip("Raio de checagem da colisão com o chão")]
    [SerializeField] float groundRay = .2f;

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

        public void Stop()
        {
            horizontalMove = 0;
        }
    }

    class ComponentDash
    {
        public bool IsDashing { get { return isDashing; } }

        bool isDashing;
        readonly PlayerAnimation playerAnimation;

        public ComponentDash(PlayerAnimation playerAnimation)
        {
            this.playerAnimation = playerAnimation;
        }

        public void Execute(float horizontalMove)
        {
            if (Input.GetButtonDown("Fire2") && horizontalMove != 0)
            {
                isDashing = true;
                playerAnimation.TriggerDash();
            }
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
        var rb2D = GetComponent<Rigidbody2D>();
        playerAttack = GetComponent<PlayerAttack>();
        playerAnimation = GetComponent<PlayerAnimation>();

        componentRun = new ComponentRun(rb2D, playerAttack, playerAnimation);
        componentDash = new ComponentDash(playerAnimation);
        componentJump  = new ComponentJump(rb2D, playerAnimation);
        componentFlip  = new ComponentFlip();
        
        health = GetComponent<Health>();
    }

    void Update()
    {
        ProcessGroundCollision();

        if (PlayerHasToStop())
        {
            StopRun();
            return;
        }

        if (componentDash.IsDashing)
            return;

        componentRun.PopulateHorizontalMove();
        StartCoroutine(DashRoutine());
        componentJump.TriggerJump();
        componentFlip.Execute(transform, componentRun.HorizontalMove);
    }

    bool PlayerHasToStop()
    {
        return playerAttack.IsAttacking() || health.IsHurting() || health.IsDead();
    }

    void ProcessGroundCollision()
    {
        var hit = Physics2D.Raycast(groundPoint.transform.position, Vector2.down, groundRay);
        if (hit.collider != null && hit.collider.gameObject.CompareTag("Ground"))
            componentJump.Stop();
    }

    IEnumerator DashRoutine()
    {
        componentDash.Execute(componentRun.HorizontalMove);

        yield return new WaitForSeconds(delayDash);

        componentDash.Stop();
    }

    void FixedUpdate()
    {
        var moveSpeed = componentDash.IsDashing ? dashSpeed : speed;
        componentRun.Execute(moveSpeed);
        componentJump.Execute(jumpHeight);
    }

    public void StopRun()
    {
        componentRun.Stop();
    }
}
