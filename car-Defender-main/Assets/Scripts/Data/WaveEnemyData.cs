using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WaveProperty
{
    public int                WaveIndex;
    public EnemyEnums.EnemyId EnemyId;
    public int                EnemyAmount;
    public bool               IsIncreaseEachLevelRound;
}

public class WaveEnemyData : ScriptableObject
{
    public int            WaveLevel;
    public WaveProperty[] wave_properties;
    public int            MaxEnemyOnScreen;

    #region Helper

    public WaveProperty GetWave (int index)
    {
        for (int i = 0; i < wave_properties.Length; i++)
        {
            if (wave_properties[i].WaveIndex == index)
                return wave_properties[i];
        }

        if (wave_properties[wave_properties.Length - 1].WaveIndex < index)
            return wave_properties[wave_properties.Length - 1];

        return wave_properties[0];
    }

    public bool IsLastWave (int index)
    {
        return wave_properties[wave_properties.Length - 1].WaveIndex < index;
    }

    public int GetTotalEnemy ()
    {
        var total = 0;

        for (int i = 0; i < wave_properties.Length; i++)
        {
            if (wave_properties[i].IsIncreaseEachLevelRound)

                total += Mathf.Clamp (PlayerData.LevelRound * wave_properties[i].EnemyAmount, wave_properties[i].EnemyAmount, GameConfig.MaxEnemyIncreased);

            else
            {
                total += wave_properties[i].EnemyAmount;
            }
        }

        return total;
    }

    #endregion
}