using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scriptable Object/Items/Melee Item")]
public class MeleeItemDefinition : ItemDefinition
{
    [Header("Melee")]
    public float attackRange = 1.8f;
    public float attackRadius = 0.6f;
    public int damage = 20;
    public float cooldown = 0.5f;
    public LayerMask targetMask;
}