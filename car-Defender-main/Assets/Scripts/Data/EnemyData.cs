using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyData : ScriptableObject
{
    public EnemyEnums.EnemyId EnemyId;
    public float GoldDrop;
    public int   GoldDropUnit;
    public float Hp;
    public int   HpUnit;
    public float SpeedMoving;
    public float HpCoefficient;
    public float GoldCoefficient;
}