using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : Singleton<GameData>
{
    [Header ("Currency")] public CurrencyImage CurrencyIcon;

    [Header ("Node")] public ItemNodeGroupData ItemDataGroup;
    public                   GridNodeData      GridNodeData;

    public ItemNodeImage ItemMoving;
    public ItemNodeImage ItemNodeIdleImage;

    [Header ("Equipments")] public EquipmentGroupData EquipmentData;
    public                         EquipmentIconData  EquipmentIconData;
    public                         EquipmentImage     EquipmentImage;
    public                         EquipmentData      MultiRewardData;

    [Header ("Shop")] public ItemShopDetail    ItemShopDetail;
    public                   ItemDiamondPrices ItemDiamondPrices;

    [Header ("Boost")] public BoostData BoostData;

    [Header ("Number")] public NumberIcon NumberIcon;

    [Header ("Level")] public RewardLevelup RewardLevelup;
    public                    LevelData     LevelData;

    [Header ("Bullets Data")] public BulletDataGroup BulletsData;
    [Header ("Weapons Data")] public WeaponData      WeaponData;
    [Header ("Enemy Data")]   public EnemyData       EnemyData;

    [Header ("Wave Data")] [SerializeField]
    public WaveEnemyDataGroup WaveEnemyDataGroup;

    #region Helper

    public void GetProfit (EquipmentEnums.AbilityId id, out double profit_data, out int profit_unit)
    {
        var level = PlayerData.GetEquipmentUpgrade (id);
        var data  = EquipmentData.GetData (id);

        profit_data = data.UpgradeValue * level;
        profit_unit = 0;

        Helper.FixUnit (ref profit_data, ref profit_unit);
    }

    public float GetEquipmentPercents (EquipmentEnums.AbilityId id)
    {
        var upgrade_times = PlayerData.GetEquipmentUpgrade (id);

        var data = EquipmentData.GetData (id);

        return data.UpgradeValue * upgrade_times;
    }

    #endregion
}