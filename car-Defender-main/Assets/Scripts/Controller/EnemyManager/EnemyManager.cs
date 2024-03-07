using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : Singleton<EnemyManager>
{
    [Header ("Config")] [SerializeField] private Transform transform_start_instance_enemy;
    [SerializeField]                     private Transform transform_middle_position_enemy;

    [SerializeField] private float max_range_x_instance_enemy_position;
    [SerializeField] private float max_range_y_instance_enemy_position;

    [SerializeField] private float range_x_position_change_direction;


    #region Variables

    private readonly List<IEnemy> IEnemy = new List<IEnemy> ();

    private int size;

    private int MaxSizeEnemy;
    private int MaxEnemyOnScreen;

    // =============================== The number of enemies will be created ================================ //
    private int current_size_enemy_instanced_during_waves;

    // =============================== The number of enemies will be created in the small wave in wave ================================ //
    private int current_number_enemy_instance_in_a_wave;

    private int total_enemy_in_the_wave;

    private float current_time_instance_enemy;
    private float time_instance_enemy;

    private Vector3 position_instance_enemy;
    private Vector3 default_position_instance_enemy;

    private int current_number_enemy_destroyed;

    private WaveEnemyData current_wave_data;

    private EnemyEnums.EnemyId current_enemy_id;

    private int current_wave_index;
    private int current_wave_level;

    private bool IsReady;

    #endregion

    #region System

    protected override void Awake ()
    {
        base.Awake ();

        InitConfig ();
        RefreshData ();
        RefreshLevelRound ();
    }

    private void Update ()
    {
        if (!IsReady)
            return;

        for (int i = 0; i < size; i++)
        {
            IEnemy[i].IUpdate ();
        }

        if (size < MaxEnemyOnScreen)
        {
            current_time_instance_enemy += Time.deltaTime;

            if (current_time_instance_enemy > time_instance_enemy &&
                current_number_enemy_instance_in_a_wave < MaxSizeEnemy &&
                current_size_enemy_instanced_during_waves < total_enemy_in_the_wave)
            {
                current_time_instance_enemy = 0;

                InstanceEnemy (current_enemy_id);
            }
        }
    }

    private void LateUpdate ()
    {
        for (int i = 0; i < size; i++)
        {
            IEnemy[i].IRenderer ();
        }
    }

    #endregion

    #region Action

    public void RemoveLevel ()
    {
        for (int i = 0; i < size; i++)
        {
            IEnemy[i].Remove ();
        }

        size = 0;
        IEnemy.Clear ();
    }

    public void ReloadLevel (int level_wave)
    {
        if (level_wave > PlayerData.LevelRound)
            return;

        PlayerData.LevelRound = level_wave;
        PlayerData.SaveLevelRound ();
        PlayerData.SaveHighLevelRound ();

        current_enemy_id = EnemyEnums.EnemyId.None;

        current_wave_index = 1;
        current_wave_level = 1;

        RefreshData ();
        RefreshLevelRound ();
    }

    private void InitConfig ()
    {
        time_instance_enemy             = 0.5f;
        default_position_instance_enemy = transform_start_instance_enemy.position;

        current_wave_index = 1;
        current_wave_level = 1;

        RefreshSateGame (PlayerData.IsTutorialCompleted (TutorialEnums.TutorialId.HowToPlayGame));
    }

    public void RefreshSateGame (bool is_ready)
    {
        IsReady = is_ready;
    }

    public void RefreshData ()
    {
        current_size_enemy_instanced_during_waves = 0;

        current_wave_data       = GameData.Instance.WaveEnemyDataGroup.GetData (current_wave_level);
        MaxEnemyOnScreen        = Mathf.Clamp (PlayerData.LevelRound * current_wave_data.MaxEnemyOnScreen, current_wave_data.MaxEnemyOnScreen, GameConfig.MaxEnemyOnScreen);
        total_enemy_in_the_wave = current_wave_data.GetTotalEnemy ();

        current_number_enemy_destroyed = 0;
    }

    public void RefreshLevelRound ()
    {
        WaveProperty value = current_wave_data.GetWave (current_wave_index);

        if (current_enemy_id != value.EnemyId)
        {
            current_number_enemy_instance_in_a_wave = 0;

            if (value.IsIncreaseEachLevelRound)

                MaxSizeEnemy = Mathf.Clamp (PlayerData.LevelRound * value.EnemyAmount, value.EnemyAmount, GameConfig.MaxEnemyIncreased);

            else
            {
                MaxSizeEnemy = value.EnemyAmount;
            }
        }
        else
        {
            if (value.IsIncreaseEachLevelRound)

                MaxSizeEnemy += Mathf.Clamp (PlayerData.LevelRound * value.EnemyAmount, value.EnemyAmount, GameConfig.MaxEnemyIncreased);
            else
            {
                MaxSizeEnemy = value.EnemyAmount;
            }
        }

        current_enemy_id = value.EnemyId;
    }

    public void InstanceEnemy (EnemyEnums.EnemyId enemyId)
    {
        switch (enemyId)
        {
            case EnemyEnums.EnemyId.Virus:
                InstanceVirusEnemy ();
                break;
            case EnemyEnums.EnemyId.Rock:
                InstanceRockEnemy ();
                break;
            case EnemyEnums.EnemyId.Trap:
                InstanceTrapEnemy ();
                break;
            case EnemyEnums.EnemyId.Boss:
                InstanceBossEnemy ();
                break;
            default:
                InstanceVirusEnemy ();
                break;
        }

        current_number_enemy_instance_in_a_wave++;

        if (current_number_enemy_instance_in_a_wave >= MaxSizeEnemy)
        {
            current_wave_index++;

            if (!current_wave_data.IsLastWave (current_wave_index))
            {
                RefreshLevelRound ();
            }
        }
    }

    public void InstanceBossEnemy ()
    {
        var enemy = EnemyEnums.GetRandomBossEnemy ();

        var pool_enemy = PoolExtension.GetPool (EnemyEnums.GetPoolEnemy (enemy), false);

        current_size_enemy_instanced_during_waves++;

        if (ReferenceEquals (pool_enemy, null))
        {
            current_number_enemy_destroyed++;
            return;
        }

        var script = pool_enemy.GetComponent<EnemyBehaviour> ();

        script.Init (GetVisiblePosition ());

        Register (script);

        pool_enemy.gameObject.SetActive (true);
    }

    public void InstanceRockEnemy ()
    {
        var enemy = EnemyEnums.GetRandomRockEnemy ();

        var pool_enemy = PoolExtension.GetPool (EnemyEnums.GetPoolEnemy (enemy), false);

        current_size_enemy_instanced_during_waves++;

        if (ReferenceEquals (pool_enemy, null))
        {
            current_number_enemy_destroyed++;
            return;
        }

        var script = pool_enemy.GetComponent<EnemyBehaviour> ();

        script.Init (GetRandomPosition ());

        Register (script);

        pool_enemy.gameObject.SetActive (true);
    }

    public void InstanceTrapEnemy ()
    {
        var enemy = EnemyEnums.GetRandomTrapEnemy ();

        var pool_enemy = PoolExtension.GetPool (EnemyEnums.GetPoolEnemy (enemy), false);

        current_size_enemy_instanced_during_waves++;

        if (ReferenceEquals (pool_enemy, null))
        {
            current_number_enemy_destroyed++;
            return;
        }

        var script = pool_enemy.GetComponent<EnemyBehaviour> ();

        script.Init (GetRandomPosition ());

        Register (script);

        pool_enemy.gameObject.SetActive (true);
    }

    public void InstanceVirusEnemy ()
    {
        var enemy = EnemyEnums.GetRandomVirusEnemy ();

        var pool_enemy = PoolExtension.GetPool (EnemyEnums.GetPoolEnemy (enemy), false);

        current_size_enemy_instanced_during_waves++;

        if (ReferenceEquals (pool_enemy, null))
        {
            current_number_enemy_destroyed++;
            return;
        }

        var script = pool_enemy.GetComponent<EnemyBehaviour> ();

        script.Init (GetRandomPosition ());

        Register (script);

        pool_enemy.gameObject.SetActive (true);
    }

    public void Register (IEnemy value)
    {
        IEnemy.Add (value);

        size++;
    }

    public void UnRegister (IEnemy value)
    {
        IEnemy.Remove (value);

        size--;
    }

    public void AlterDestroyEnemy (IEnemy value)
    {
        UnRegister (value);

        if (value.GetEnemyId () != current_enemy_id)
            return;

        current_number_enemy_destroyed++;

        if (current_number_enemy_destroyed >= total_enemy_in_the_wave)
        {
            current_wave_level++;
            current_wave_index = 1;

            if (GameData.Instance.WaveEnemyDataGroup.IsLastedLevel (current_wave_level))
            {
                current_wave_level = 1;

                PlayerData.LevelRound++;
                PlayerData.SaveLevelRound ();
                PlayerData.SaveHighLevelRound ();

                GameManager.Instance.EnableNextGame ();
            }

            RefreshData ();
            RefreshLevelRound ();
        }
    }

    public void AlterRefreshEnemy (IEnemy value)
    {
        UnRegister (value);

        current_number_enemy_instance_in_a_wave--;
        current_size_enemy_instanced_during_waves--;
    }

    public void AlterRemoveEnemy (IEnemy value)
    {
        UnRegister (value);
    }

    #endregion

    #region Helper

    public Vector3 GetRandomPosition ()
    {
        position_instance_enemy.x = default_position_instance_enemy.x + Random.Range (-max_range_x_instance_enemy_position / 2, max_range_x_instance_enemy_position / 2);
        position_instance_enemy.y = default_position_instance_enemy.y + Random.Range (-max_range_y_instance_enemy_position / 2, max_range_y_instance_enemy_position / 2);
        position_instance_enemy.z = 0;

        return position_instance_enemy;
    }

    public Vector3 GetVisiblePosition ()
    {
        position_instance_enemy.x = default_position_instance_enemy.x +
                                    Random.Range (-(max_range_x_instance_enemy_position - range_x_position_change_direction) / 2,
                                                  (max_range_x_instance_enemy_position - range_x_position_change_direction) / 2);

        position_instance_enemy.y = default_position_instance_enemy.y + Random.Range (-max_range_y_instance_enemy_position / 2, max_range_y_instance_enemy_position / 2);
        position_instance_enemy.z = 0;

        return position_instance_enemy;
    }

    public float GetLimitXLeftInstanceEnemy ()
    {
        return default_position_instance_enemy.x - max_range_x_instance_enemy_position / 2f;
    }

    public float GetLimitYRightInstanceEnemy ()
    {
        return default_position_instance_enemy.x + max_range_x_instance_enemy_position / 2f;
    }

    public float GetRangeChangeXPosition ()
    {
        return range_x_position_change_direction;
    }

    public Vector3 GetMiddlePositionScreenEnemy ()
    {
        return transform_middle_position_enemy.position;
    }

    #endregion
}