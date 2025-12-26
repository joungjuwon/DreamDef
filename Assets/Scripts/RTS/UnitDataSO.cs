using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "RTS/Unit Data")]
public class UnitDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string unitName;
    public float maxHealth = 100f;
    public float defense = 5f;
    public float moveSpeed = 5f;

    [Header("Attack Stats")]
    public float attackDamage = 10f;
    public float attackRange = 5f;
    public float attackSpeed = 1f;
    
    [Header("Combat Configuration")]
    public AttackType attackType;
    public TargetPriority targetPriority;
    public float searchRange = 10f;
    public float areaRadius = 3f;

    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject zonePrefab;
}