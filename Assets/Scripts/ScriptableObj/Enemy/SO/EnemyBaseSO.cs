using System;
using UnityEngine;
public enum EnemyType
{
    Ranger,
    RangerTank,
    Kamikaze,
    KamikazeTank,
    Parasite,
    Boss,
}

public abstract class EnemyBaseSO : ScriptableObject
{
    [Header("Base Stats")]
    public EnemyType enemyType;
    public int enemyHP;
    public float enemySpeed;
    public GameObject enemyPrefab;  
    public abstract void PerformAttack(Enemy enemy);
}
