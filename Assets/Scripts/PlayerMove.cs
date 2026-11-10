
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float maxSpeed;
    public float jumpPower;
    float knockbackTimer = 0f;
    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;
    Animator anim;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        //Jump
        if (Input.GetButtonDown("Jump") && !anim.GetBool("IsJumping"))
        {
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            anim.SetBool("IsJumping", true);
        }



        if (knockbackTimer <= 0f && Input.GetButtonUp("Horizontal"))
        {
            //Stop Speed
            rigid.linearVelocity = new Vector2(
                rigid.linearVelocity.normalized.x * 0.5f,
                rigid.linearVelocity.y
            );
        }

        //Direction Sprite Flip
        if (Input.GetButton("Horizontal"))
            spriteRenderer.flipX = Input.GetAxisRaw("Horizontal") == -1;

        //Animation
        if (Mathf.Abs(rigid.linearVelocity.x) < 0.3)
            anim.SetBool("IsWalking", false);
        else 
            anim.SetBool("IsWalking", true);
    }



    void FixedUpdate()
    {
        // 피격 직후에는 잠시 이동 입력 제한
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // Move By Key Control
            float h = Input.GetAxisRaw("Horizontal");

            rigid.AddForce(Vector2.right * h * 3f, ForceMode2D.Impulse);

            // Max Speed
            if (rigid.linearVelocity.x > maxSpeed)
                rigid.linearVelocity = new Vector2(maxSpeed, rigid.linearVelocity.y);
            else if (rigid.linearVelocity.x < -maxSpeed)
                rigid.linearVelocity = new Vector2(-maxSpeed, rigid.linearVelocity.y);
        }

        // Landing Platform
        if (rigid.linearVelocity.y < 0)
        {
            Debug.DrawRay(rigid.position, Vector3.down, new Color(0, 1, 0));

            RaycastHit2D rayHit = Physics2D.Raycast(
                rigid.position,
                Vector3.down,
                1,
                LayerMask.GetMask("Platform")
            );

            if (rayHit.collider != null)
            {
                if (rayHit.distance < 0.5f)
                    anim.SetBool("IsJumping", false);
            }
        }
    }




    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyMove enemyMove =
                collision.gameObject.GetComponentInParent<EnemyMove>();

            // 실제 적을 위에서 밟았을 때만 공격
            if (enemyMove != null &&
                rigid.linearVelocity.y < 0 &&
                transform.position.y > collision.transform.position.y)
            {
                OnAttack(enemyMove.transform);
            }
            else // 가시나 적의 옆면에 닿았을 때
            {
                OnDamaged(collision.transform.position);
            }
        }
    }


    void OnDamaged(Vector2 targetPos)
    {
        gameObject.layer = 11;

        // 피격 직후 0.2초만 이동 제한
        knockbackTimer = 0.2f;

        spriteRenderer.color = new Color(1, 1, 1, 0.4f);

        int dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
        rigid.AddForce(new Vector2(dirc, 1) * 7, ForceMode2D.Impulse);

        anim.SetTrigger("doDamaged");
        Invoke("OffDamaged", 3);
    }


    void OnAttack(Transform enemy)
    {
        // Reaction Force
        rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);

        // EnemyMove 컴포넌트 가져오기
        EnemyMove enemyMove = enemy.GetComponentInParent<EnemyMove>();

        if (enemyMove != null)
        {
            enemyMove.OnDamaged();
        }
        else
        {
            Debug.LogError(enemy.name + "에서 EnemyMove를 찾을 수 없습니다!");
        }
    }

    void OffDamaged()
    {
        gameObject.layer = 10;
        spriteRenderer.color = new Color(1, 1, 1, 1);

    }
}
