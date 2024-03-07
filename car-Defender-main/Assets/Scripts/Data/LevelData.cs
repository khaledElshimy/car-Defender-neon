using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : ScriptableObject
{
    [System.Serializable]
    public struct LevelProperty
    {
        [Tooltip ("The level player can reach.")]
        public int Level;

        [Tooltip ("The exp will reach next level.")]
        public int LevelUpExp;

        [Tooltip ("The maximum item in grid.")]
        public int MaxItem;
    }

    [Header ("Config")] [SerializeField] private LevelProperty[] _LevelProperties;

    public LevelProperty GetLevelData (int level)
    {
        for (int i = 0; i < _LevelProperties.Length; i++)
        {
            if (_LevelProperties[i].Level == level)
                return _LevelProperties[i];
        }

        if (level > _LevelProperties[_LevelProperties.Length - 1].Level)
            return _LevelProperties[_LevelProperties.Length - 1];

        return _LevelProperties[0];
    }

    public int GetMaxLevel ()
    {
        int max = 0;

        for (int i = 0; i < _LevelProperties.Length; i++)
        {
            if (_LevelProperties[i].Level > max)
            {
                max = _LevelProperties[i].Level;
            }
        }

        return max;
    }

    public int GetExpWithLevel (int level)
    {
        for (int i = 0; i < _LevelProperties.Length; i++)
        {
            if (_LevelProperties[i].Level == level)
                return _LevelProperties[i].LevelUpExp;
        }

        return -1;
    }

    public int GetMaxItemWithLevel (int level)
    {
        for (int i = 0; i < _LevelProperties.Length; i++)
        {
            if (_LevelProperties[i].Level == level)
                return _LevelProperties[i].MaxItem;
        }

        return _LevelProperties[_LevelProperties.Length - 1].MaxItem;
    }

    public int GetSize ()
    {
        return _LevelProperties.Length;
    }
}