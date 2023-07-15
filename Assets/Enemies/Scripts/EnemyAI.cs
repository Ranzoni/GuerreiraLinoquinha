using UnityEngine;

[RequireComponent(typeof(EnemyMove), typeof(EnemyAttack), typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] GameObject target;
    [SerializeField] float rangeFollow = 10f;
    [SerializeField] float minimumDistance = 2f;

    EnemyMove move;
    EnemyAttack attack;
    EnemyHealth health;

    void Start()
    {
        move = GetComponent<EnemyMove>();
        attack = GetComponent<EnemyAttack>();
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        move.StopMove();
        if (health.IsDead())
            return;

        var targetDistance = Vector2.Distance(transform.position, target.transform.position);

        if (targetDistance <= rangeFollow)
        {
            if (targetDistance > minimumDistance)
                move.Move(target.transform.position);
            else
            {
                move.Flip(target.transform.position);
                attack.Attack();
            }
        }
    }
}
