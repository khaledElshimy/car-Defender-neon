using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEnemy : EnemyBehaviour
{
    [Header ("ID")] [SerializeField] private EnemyEnums.BossEnemyId _BossEnemyId;

    #region Variables

    private Vector3 middle_position;
    private Vector3 target_position;

    #endregion

    #region Action

    public override void Init (Vector3 position_start)
    {
        base.Init (position_start);

        middle_position = EnemyManager.Instance.GetMiddlePositionScreenEnemy ();
        target_position = middle_position;

        level_round_enemy = PlayerData.LevelRound;

        hp      = enemy_data.Hp * enemy_data.HpCoefficient * level_round_enemy;
        hp_unit = enemy_data.HpUnit;

        Helper.FixUnit (ref hp, ref hp_unit);

        RefreshText ();
    }

    public override void IUpdate ()
    {
        if (!IsUpdate)
            return;

        if (!IsActiveHit)
        {
            IsActiveHit = GameManager.Instance.IsActiveGetHit (position.y);
        }

        if (Vector3.Distance (position, target_position) < 0.1f)
        {
            target_position = GetRandomMiddlePosition ();
        }

        speed_moving = target_speed_moving * Time.deltaTime;
        position     = Vector3.MoveTowards (position, target_position, speed_moving);

        IsRenderer = true;
    }

    public override void IRenderer ()
    {
        if (!IsRenderer)
            return;

        rigidBody2D.position = position;

        IsRenderer = false;
    }

    public override void ReturnToPools ()
    {
        PoolExtension.SetPool (EnemyEnums.GetPoolEnemy (_BossEnemyId), transform);
    }

    protected override void OnBreak ()
    {
        base.OnBreak ();

        CameraManager.Instance.FxShakeCamera ();
        GameActionManager.Instance.InstanceFxExplodeTrap (position);

        this.PlayAudioSound (AudioEnums.SoundId.FxBossEnemyExplode);
    }

    #endregion

    #region Helper

    private Vector3 GetRandomMiddlePosition ()
    {
        var position_target = middle_position;
        var left_position   = EnemyManager.Instance.GetLimitXLeftInstanceEnemy ();
        var right_position  = EnemyManager.Instance.GetLimitYRightInstanceEnemy ();

        position_target.x =  Random.Range (left_position, right_position);
        position_target.y += Random.Range (-3f, 3f);

        return position_target;
    }

    #endregion
}