using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletDataGroup : ScriptableObject
{
    [SerializeField] private BulletsData[] BulletsProperties;

    #region Helper

    public BulletsData GetBullets (int level)
    {
        for (int i = 0; i < BulletsProperties.Length; i++)
        {
            if (BulletsProperties[i].Level == level)
                return BulletsProperties[i];
        }

        if (level > BulletsProperties[BulletsProperties.Length - 1].Level)
            return BulletsProperties[BulletsProperties.Length - 1];

        return BulletsProperties[0];
    }

    #endregion
}