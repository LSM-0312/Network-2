using UnityEngine;

[CreateAssetMenu(menuName = "Game/Scriptable Object/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic")]
    public int itemId;
    public string displayName;
    public Sprite icon;
    public GameObject heldViewPrefab;

    [Header("Held View")]
    public Vector3 heldLocalPosition;
    public Vector3 heldLocalEulerAngles;
    public Vector3 heldLocalScale = Vector3.one;
}