using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirusEnemy : EnemyBehaviour
{
   [Header("ID")]
   [SerializeField] private EnemyEnums.VirusEnemyId _VirusEnemyId;
   
   [Header ("Animation")] [SerializeField]
   private Animation animation_controller;

   [SerializeField] private AnimationClip clip_animation_hit;
   
   #region Variables
   
   private DirectionEnums.DirectionId direction_up_down;
   private DirectionEnums.DirectionId direction_left_right;
   
   #endregion

   #region Action

   public override void Init (Vector3 position_start)
   {
      base.Init (position_start);
      
      RefreshDirectionUpDown ();
      RefreshDirectionLeftRight ();

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

      switch (direction_up_down)
      {
         case DirectionEnums.DirectionId.Up:
            position.y += speed_moving;
            break;
         case DirectionEnums.DirectionId.Down:
            position.y -= speed_moving;
            break;
      }

      switch (direction_left_right)
      {
         case DirectionEnums.DirectionId.Left:
            position.x -= speed_moving;

            if (GameManager.Instance.IsOutSiteLeft (position.x))
               direction_left_right = DirectionEnums.DirectionId.Right;

            break;
         case DirectionEnums.DirectionId.Right:
            position.x += speed_moving;

            if (GameManager.Instance.IsOutSiteRight (position.x))
               direction_left_right = DirectionEnums.DirectionId.Left;

            break;
      }

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
      
      PoolExtension.SetPool (EnemyEnums.GetPoolEnemy (_VirusEnemyId), transform);
      
      direction_left_right = DirectionEnums.DirectionId.None;
      direction_up_down    = DirectionEnums.DirectionId.None;
   }
   
   private void RefreshDirectionUpDown ()
   {
      switch (direction_up_down)
      {
         case DirectionEnums.DirectionId.None:
            direction_up_down = DirectionEnums.DirectionId.Down;
            break;
         case DirectionEnums.DirectionId.Down:
            direction_up_down = DirectionEnums.DirectionId.Up;
            break;
         case DirectionEnums.DirectionId.Up:
            direction_up_down = DirectionEnums.DirectionId.Down;
            break;
      }
   }

   private void RefreshDirectionLeftRight ()
   {
      switch (direction_left_right)
      {
         case DirectionEnums.DirectionId.None:

            var random_value = Random.Range (0, 3);

            switch (random_value)
            {
               case 0:
                  direction_left_right = DirectionEnums.DirectionId.None;
                  break;
               case 1:
                  direction_left_right = DirectionEnums.DirectionId.Left;
                  break;
               case 2:
                  direction_left_right = DirectionEnums.DirectionId.Right;
                  break;
               default:
                  direction_left_right = DirectionEnums.DirectionId.None;
                  break;
            }

            break;
         case DirectionEnums.DirectionId.Left:
            direction_left_right = DirectionEnums.DirectionId.Right;
            break;
         case DirectionEnums.DirectionId.Right:
            direction_left_right = DirectionEnums.DirectionId.Left;
            break;
      }
   }

   public override void OnHit (double damage, int damageUnit)
   {
      base.OnHit (damage, damageUnit);
      
      FxHit ();
   }

   protected override void OnBreak ()
   {
      base.OnBreak ();
      
      GameActionManager.Instance.InstanceFxExplodeVirus (position);

      this.PlayAudioSound (AudioEnums.SoundId.FxVirusEnemyExplode);
   }

   #endregion
   
   #region Fx

   public void FxHit ()
   {
      if (animation_controller.isPlaying)
      {
         animation_controller.Stop ();
      }

      animation_controller.Play (clip_animation_hit.name);
   }

   #endregion
  
}
