using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletsData : ScriptableObject
{
    public PoolEnums.PoolId BulletId;
    public int              Level;
    public float            Damage;
    public int              DamageUnit;
    public float            SpeedMoving;
    public float            CritChange;
    public float            CritAmount;
    
    [Range(0, 1f)]
    public float            DamageMissRange;
    public float            DamageCoefficient;
}