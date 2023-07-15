using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("Quantidade de vida inicial")]
    [SerializeField] int health = 8;

    public void TakeDamage(int damage)
    {
        if (health > 0)
            health -= damage;
    }

    public bool IsDead()
    {
        return health <= 0;
    }
}
