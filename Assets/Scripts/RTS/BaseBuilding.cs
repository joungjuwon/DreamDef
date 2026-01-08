using UnityEngine;

public class BaseBuilding : Building
{
    public static event System.Action OnBaseDestroyed;

    protected override void Die()
    {
        OnBaseDestroyed?.Invoke();
        Debug.Log("Game Over");
        
        Time.timeScale = 0f; // 게임 정지
        
        base.Die();
    }
}