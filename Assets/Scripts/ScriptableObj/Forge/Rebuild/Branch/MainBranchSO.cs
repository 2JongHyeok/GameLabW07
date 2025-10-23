using UnityEngine;
public enum MainBranchType
{
    SpaceShip,
    Attacks,
    Planet,
    Planet2,
}
[CreateAssetMenu(fileName = "MainBranchSO", menuName = "ScriptableObjects/Forge/Branch/MainBranchSO", order = 1)]
public class MainBranchSO : BranchSO
{
    public MainBranchType branchType;
    public SubBranchSO[] subBranches;
}
