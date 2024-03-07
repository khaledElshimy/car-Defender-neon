using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnemyEnums
{
    public enum EnemyId
    {
        Rock,
        Boss,
        Virus,
        Trap,
        None,
    }

    public enum TrapEnemyId
    {
        Trap_1,
        Trap_2,
        Trap_3,
        Trap_4,
        Trap_5,
        Trap_6,
        Trap_7,
        Trap_8,
        Trap_9,
        Trap_10,
        Trap_11,
        Trap_12,
        Trap_13,
        Trap_14,
    }

    public enum RockEnemyId
    {
        Rock_1,
        Rock_2,
        Rock_3,
    }

    public enum BossEnemyId
    {
        Boss_1,
        Boss_2,
        Boss_3,
    }

    public enum VirusEnemyId
    {
        Enemy_1,
        Enemy_2,
        Enemy_3,
        Enemy_4,
        Enemy_5,
        Enemy_6,
        Enemy_7,
        Enemy_8,
    }

    private const int VirusEnemySize = 8;
    private const int RockEnemySize  = 3;
    private const int BossEnemySize  = 3;
    private const int TrapEnemySize  = 14;

    public static PoolEnums.PoolId GetPoolEnemy (VirusEnemyId id)
    {
        switch (id)
        {
            case VirusEnemyId.Enemy_1:
                return PoolEnums.PoolId.Enemy_1;
            case VirusEnemyId.Enemy_2:
                return PoolEnums.PoolId.Enemy_2;
            case VirusEnemyId.Enemy_3:
                return PoolEnums.PoolId.Enemy_3;
            case VirusEnemyId.Enemy_4:
                return PoolEnums.PoolId.Enemy_4;
            case VirusEnemyId.Enemy_5:
                return PoolEnums.PoolId.Enemy_5;
            case VirusEnemyId.Enemy_6:
                return PoolEnums.PoolId.Enemy_6;
            case VirusEnemyId.Enemy_7:
                return PoolEnums.PoolId.Enemy_7;
            case VirusEnemyId.Enemy_8:
                return PoolEnums.PoolId.Enemy_8;
            default:
                return PoolEnums.PoolId.Enemy;
        }
    }

    public static PoolEnums.PoolId GetPoolEnemy (RockEnemyId id)
    {
        switch (id)
        {
            case RockEnemyId.Rock_1:
                return PoolEnums.PoolId.Rock_Enemy_1;
            case RockEnemyId.Rock_2:
                return PoolEnums.PoolId.Rock_Enemy_2;
            case RockEnemyId.Rock_3:
                return PoolEnums.PoolId.Rock_Enemy_3;
            default:
                return PoolEnums.PoolId.Enemy;
        }
    }

    public static PoolEnums.PoolId GetPoolEnemy (BossEnemyId id)
    {
        switch (id)
        {
            case BossEnemyId.Boss_1:
                return PoolEnums.PoolId.Boss_1;
            case BossEnemyId.Boss_2:
                return PoolEnums.PoolId.Boss_2;
            case BossEnemyId.Boss_3:
                return PoolEnums.PoolId.Boss_1;
            default:
                return PoolEnums.PoolId.Boss_1;
        }
    }

    public static PoolEnums.PoolId GetPoolEnemy (TrapEnemyId id)
    {
        switch (id)
        {
            case TrapEnemyId.Trap_1:
                return PoolEnums.PoolId.TrapEnemy_1;
            case TrapEnemyId.Trap_2:
                return PoolEnums.PoolId.TrapEnemy_2;
            case TrapEnemyId.Trap_3:
                return PoolEnums.PoolId.TrapEnemy_3;
            case TrapEnemyId.Trap_4:
                return PoolEnums.PoolId.TrapEnemy_4;
            case TrapEnemyId.Trap_5:
                return PoolEnums.PoolId.TrapEnemy_5;
            case TrapEnemyId.Trap_6:
                return PoolEnums.PoolId.TrapEnemy_6;
            case TrapEnemyId.Trap_7:
                return PoolEnums.PoolId.TrapEnemy_7;
            case TrapEnemyId.Trap_8:
                return PoolEnums.PoolId.TrapEnemy_8;
            case TrapEnemyId.Trap_9:
                return PoolEnums.PoolId.TrapEnemy_9;
            case TrapEnemyId.Trap_10:
                return PoolEnums.PoolId.TrapEnemy_10;
            case TrapEnemyId.Trap_11:
                return PoolEnums.PoolId.TrapEnemy_11;
            case TrapEnemyId.Trap_12:
                return PoolEnums.PoolId.TrapEnemy_12;
            case TrapEnemyId.Trap_13:
                return PoolEnums.PoolId.TrapEnemy_13;
            case TrapEnemyId.Trap_14:
                return PoolEnums.PoolId.TrapEnemy_14;
            default:
                return PoolEnums.PoolId.TrapEnemy_1;
        }
    }

    public static VirusEnemyId GetRandomVirusEnemy ()
    {
        return (VirusEnemyId) Random.Range (0, VirusEnemySize);
    }

    public static RockEnemyId GetRandomRockEnemy ()
    {
        return (RockEnemyId) Random.Range (0, RockEnemySize);
    }

    public static BossEnemyId GetRandomBossEnemy ()
    {
        return (BossEnemyId) Random.Range (0, BossEnemySize);
    }

    public static TrapEnemyId GetRandomTrapEnemy ()
    {
        return (TrapEnemyId) Random.Range (0, TrapEnemySize);
    }
}