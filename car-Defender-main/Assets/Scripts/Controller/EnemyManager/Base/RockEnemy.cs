using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockEnemy : EnemyBehaviour
{
    [Header ("ID")] [SerializeField] private EnemyEnums.RockEnemyId _RockEnemyId;

    #region Variables

    private DirectionEnums.DirectionId DirectionId;

    #endregion

    #region Action

    public override void Init (Vector3 position_start)
    {
        base.Init (position_start);

        RefreshDirection ();
        
        level_round_enemy = Random.Range (PlayerData.LevelRound / 2, PlayerData.LevelRound);
        
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

        speed_moving = Time.deltaTime * target_speed_moving;

        switch (DirectionId)
        {
            case DirectionEnums.DirectionId.Left:
                position.x -= speed_moving;
                break;
            case DirectionEnums.DirectionId.Right:
                position.x += speed_moving;
                break;
        }

        position.y -= speed_moving;

        IsRenderer = true;
    }

    public override void IRenderer ()
    {
        if (!IsRenderer)
            return;

        rigidBody2D.position = position;
        
        if (GameManager.Instance.IsOutSiteDown (position.y))
            AlterDestroy ();

        IsRenderer = false;
    }

    public void RefreshDirection ()
    {
        DirectionId = DirectionEnums.DirectionId.None;

        var limit_x_left  = EnemyManager.Instance.GetLimitXLeftInstanceEnemy ();
        var limit_x_right = EnemyManager.Instance.GetLimitYRightInstanceEnemy ();

        if (position.x < limit_x_left)
        {
            DirectionId = DirectionEnums.DirectionId.Right;
        }

        if (position.x > limit_x_right)
        {
            DirectionId = DirectionEnums.DirectionId.Left;
        }
    }

    public override void ReturnToPools ()
    {
        base.ReturnToPools ();
        
        PoolExtension.SetPool (EnemyEnums.GetPoolEnemy (_RockEnemyId), transform);
    }
    
    protected override void OnBreak ()
    {
        base.OnBreak ();
      
        GameActionManager.Instance.InstanceFxExplodeTrap (position);

        this.PlayAudioSound (AudioEnums.SoundId.FxRockEnemyExplode);
    }

    #endregion
}