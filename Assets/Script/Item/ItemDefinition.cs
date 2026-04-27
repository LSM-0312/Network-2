using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scriptable Object/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic")]
    public int itemId;
    public string displayName;
    public Sprite icon;
    public GameObject heldViewPrefab;

    [Header("Start")]
    public int defaultAmmo;
}