using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemNodeData : ScriptableObject
{
    public PoolEnums.PoolId         ItemPoolId;
    public int                      Level;
    public float                    PerCircleTime;
    public float                    ProfitPerSec;
    public int                      ProfitPerSecUnit;
    public int                      Exp;
    public float                    Prices;
    public int                      PricesUnit;
    public float                    PricesUpgrade;
    public int                      PricesUnitUpgrade;
    public float                    PricesCoefficient;
    public float                    PriceUpgradeCoefficient;
    public float                    ProfitPerUpgradeCoefficient;
    public int                      BuyFromLevel;
}