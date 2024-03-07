using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class EnemyBehaviour : MonoBehaviour, IEnemy, IHit
{
    [Header ("Enemy")] [SerializeField] private TextMeshPro EnemyValue;

    [Header ("Physics")] [SerializeField] protected Rigidbody2D rigidBody2D;

    [Header ("Data")] [SerializeField] protected EnemyData enemy_data;

    #region Variables

    protected Vector3 position;

    protected bool IsUpdate,
                   IsRenderer;

    protected double hp;
    protected int    hp_unit;


    protected float speed_moving;
    protected float target_speed_moving;

    protected bool IsActiveHit;

    protected int level_round_enemy;

    #endregion

    #region Action

    public virtual void Init (Vector3 position_start)
    {
        position             = position_start;
        rigidBody2D.position = position_start;
        transform.position   = position_start;

        IsUpdate = true;

        target_speed_moving = UnityEngine.Random.Range (enemy_data.SpeedMoving / 3f, enemy_data.SpeedMoving);
    }

    public virtual void IUpdate () { }

    public virtual void IRenderer () { }

    public virtual void Remove ()
    {
        ReturnToPools ();
    }

    public void RefreshText ()
    {
        EnemyValue.text = ApplicationManager.Instance.AppendFromUnit (hp, hp_unit);
    }

    public void AlterRefresh ()
    {
        ReturnToPools ();
        EnemyManager.Instance.AlterRefreshEnemy (this);
    }

    public void AlterDestroy ()
    {
        ReturnToPools ();
        EnemyManager.Instance.AlterDestroyEnemy (this);
    }

    public virtual void OnHit (double damage, int damageUnit)
    {
        if (!IsActiveHit)
            return;

        Helper.MinusValue (hp, hp_unit, damage, damageUnit, out hp, out hp_unit);

        if (hp < 1 && hp_unit == 0)
        {
            OnBreak ();
        }

        RefreshText ();
    }

    protected virtual void OnBreak ()
    {
        InstanceDropGold ();
        AlterDestroy ();
    }

    private void InstanceDropGold ()
    {
        double gold_drop = enemy_data.GoldDrop * enemy_data.GoldCoefficient * level_round_enemy;
        var    gold_unit = enemy_data.GoldDropUnit;
        
        EarningManager.Instance.GetRealEarning (ref gold_drop , ref gold_unit);

        GameManager.Instance.FxDisplayEarnCoin (gold_drop, gold_unit, position);
    }

    public virtual void ReturnToPools ()
    {
        IsUpdate   = false;
        IsRenderer = false;
    }

    public EnemyEnums.EnemyId GetEnemyId ()
    {
        return enemy_data.EnemyId;
    }

    #endregion
}