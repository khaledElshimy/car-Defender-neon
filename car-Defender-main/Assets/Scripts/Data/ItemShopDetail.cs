using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemShopDetail : ScriptableObject
{
    [System.Serializable]
    public struct ItemShopProperty
    {
        public int    Level;
        public float  Speed;
        public float  Earning;
        public float  Damage;
    }

    [SerializeField] private ItemShopProperty[] _ItemShopProperties;

    public ItemShopProperty GetItem (int item_level)
    {
        for (int i = 0; i < _ItemShopProperties.Length; i++)
        {
            if (_ItemShopProperties[i].Level == item_level)
                return _ItemShopProperties[i];
        }

        if (item_level > _ItemShopProperties[_ItemShopProperties.Length - 1].Level)
            return _ItemShopProperties[_ItemShopProperties.Length - 1];

        return _ItemShopProperties[0];
    }

    public float GetSpeed (int item_level)
    {
        for (int i = 0; i < _ItemShopProperties.Length; i++)
        {
            if (_ItemShopProperties[i].Level == item_level)
                return _ItemShopProperties[i].Speed;
        }

        return 0;
    }

    public float GetEarning (int item_level)
    {
        for (int i = 0; i < _ItemShopProperties.Length; i++)
        {
            if (_ItemShopProperties[i].Level == item_level)
                return _ItemShopProperties[i].Earning;
        }

        return 0;
    }

    public float GetDamage (int item_level)
    {
        for (int i = 0; i < _ItemShopProperties.Length; i++)
        {
            if (_ItemShopProperties[i].Level == item_level)
                return _ItemShopProperties[i].Damage;
        }

        return 0;
    }
}