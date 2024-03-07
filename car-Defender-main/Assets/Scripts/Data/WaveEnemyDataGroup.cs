using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveEnemyDataGroup : ScriptableObject
{
    [SerializeField] private WaveEnemyData[] WaveEnemyData;

    #region Helper

    public WaveEnemyData GetData (int index_level)
    {
        for (int i = 0; i < WaveEnemyData.Length; i++)
        {
            if (WaveEnemyData[i].WaveLevel == index_level)
                return WaveEnemyData[i];
        }

        if (WaveEnemyData[WaveEnemyData.Length - 1].WaveLevel < index_level)
            return WaveEnemyData[WaveEnemyData.Length - 1];

        return WaveEnemyData[0];
    }

    public bool IsLastedLevel (int index_level)
    {
        return WaveEnemyData[WaveEnemyData.Length - 1].WaveLevel < index_level;
    }

    #endregion
}