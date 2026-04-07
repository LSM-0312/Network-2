using Fusion;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public int itemId;
    public string displayName;
    public Sprite icon;

    public ItemType itemType;
    public ItemUseMode useMode;

    public GameObject heldViewPrefab;

    public int defaultAmmo;
    public int defaultStack = 1;

    public NetworkPrefabRef projectilePrefab;
    public float throwForce = 15f;
    public float upwardForce = 1.5f;

    public float range = 50f;
    public int damage = 20;
    public LayerMask hitMask;
}