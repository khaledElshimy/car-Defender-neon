using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrapEnemy : EnemyBehaviour
{
    [Header ("Config")] [SerializeField] private EnemyEnums.TrapEnemyId _TrapEnemyId;

    [Header ("Animation")] [SerializeField]
    private Animation animation_controller;

    public override void Init (Vector3 position_start)
    {
        base.Init (position_start);

        level_round_enemy = PlayerData.LevelRound;

        hp      = enemy_data.Hp * enemy_data.HpCoefficient * PlayerData.LevelRound;
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

        speed_moving = Time.deltaTime * target_speed_moving;

        position.y -= speed_moving;

        IsRenderer = true;
    }

    public override void IRenderer ()
    {
        if (!IsRenderer)
            return;

        rigidBody2D.position = position;

        if (GameManager.Instance.IsOutSiteDown (position.y))
            AlterRefresh ();

        IsRenderer = false;
    }

    public override void ReturnToPools ()
    {
        base.ReturnToPools ();

        PoolExtension.SetPool (EnemyEnums.GetPoolEnemy (_TrapEnemyId), transform);
    }

    public override void OnHit (double damage, int damageUnit)
    {
        base.OnHit (damage, damageUnit);

        animation_controller.Stop ();
        animation_controller.Play ();
    }

    protected override void OnBreak ()
    {
        base.OnBreak ();

        CameraManager.Instance.FxShakeCamera ();

        GameActionManager.Instance.InstanceFxExplodeTrap (position);

        this.PlayAudioSound (AudioEnums.SoundId.FxTrapEnemyExplode);
    }
}