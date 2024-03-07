using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public struct WeaponProperty
    {
        public int   Level;
        public float FireRate;
        public int   NumberBullets;
    }

    [SerializeField] private WeaponProperty[] WeaponProperties;

    #region Helper

    public WeaponProperty GetWeapon (int level)
    {
        for (int i = 0; i < WeaponProperties.Length; i++)
        {
            if (WeaponProperties[i].Level == level)
                return WeaponProperties[i];
        }

        if (level > WeaponProperties[WeaponProperties.Length - 1].Level)
            return WeaponProperties[WeaponProperties.Length - 1];

        return WeaponProperties[0];
    }

    #endregion
}