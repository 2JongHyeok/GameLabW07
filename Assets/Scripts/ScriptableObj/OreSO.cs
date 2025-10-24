using UnityEngine;
public enum OreType
{
    Coal,
    Iron,
    Gold,
    Diamond,
    PlanetCore
}
[CreateAssetMenu(fileName = "New Ore", menuName = "ScriptableObjects/Ore")]
public class OreSO : ScriptableObject
{
    public OreType oreType;
    public int weight;
    public Sprite icon;
}
