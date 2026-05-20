using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public void TakeDamage(float damage)
    {
        Debug.Log("Damage: " + damage);
    }

    public void ApplySlowDebuff(float slowAmount, float duration)
    {
        Debug.Log("Trash Debuff: " + slowAmount + " / " + duration);
    }
}