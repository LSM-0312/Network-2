using Fusion;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public int itemId;
    public string displayName;
    public Sprite icon;

    [Header("분류")]
    public ItemType itemType;
    public ItemUseMode useMode;

    [Header("손에 보일 프리팹")]
    public GameObject heldViewPrefab;

    [Header("탄약/개수")]
    public bool usesAmmo;
    public int defaultAmmo = 0;
    public int maxAmmo = 0;
    public int defaultStack = 1;

    [Header("발사/투척")]
    public NetworkPrefabRef projectilePrefab;
    public float throwForce = 15f;
    public float upwardForce = 1.5f;

    [Header("히트스캔")]
    public float range = 50f;
    public int damage = 20;
    public LayerMask hitMask;
}