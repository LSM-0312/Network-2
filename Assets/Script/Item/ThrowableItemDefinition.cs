using Fusion;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scriptable Object/Items/Throwable Item")]
public class ThrowableItemDefinition : ItemDefinition
{
    [Header("Throwable")]
    public NetworkPrefabRef projectilePrefab;
    public float throwForce = 15f;
    public float upwardForce = 1.5f;
    public int ammoPerUse = 1;
}