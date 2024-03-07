using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentData : ScriptableObject
{
    public EquipmentEnums.AbilityId AbilityId;
    public CurrencyEnums.CurrencyId CurrencyId;
    public float                    UpgradeValue;
    public float                    Price;
    public int                      PriceUnit;
    public float                    PriceCoefficient;
    public int                      UpgradeFromLevel;
    public int                      MaxLevel;
}