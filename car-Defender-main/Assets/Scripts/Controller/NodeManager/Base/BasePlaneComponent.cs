using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BasePlaneComponent : NodeComponent, IIdle
{
    [Header ("Data")] [SerializeField] private SpriteRenderer sprite_renderer;

    [SerializeField] private Transform transform_renderer;

    [Header ("Shooter")] [SerializeField] private WeaponBehaviour weapon_behaviour;

    private new Transform transform;

    private BasePlaneMoving _BasePlaneMoving;

    private bool IsMinerGold;
    private bool IsIdleGold;

    private bool IsIdlePause;
    private bool IsIdleMinerGold;


    public override NodeComponent Init (ItemNodeData _item_node_data)
    {
        if (ReferenceEquals (transform, null))
        {
            transform = gameObject.transform;
        }

        _ItemNodeData = _item_node_data;
        _IndexId      = GetInstanceID ();

        sprite_renderer.enabled = true;

        weapon_behaviour.Init (GameData.Instance.WeaponData.GetWeapon (_item_node_data.Level), TagEnums.GetKey (TagEnums.TagId.Enemy));

        SetStatePause (true);

        weapon_behaviour.OnShooter = WeaponBehaviourOnOnShooter;

        RefreshLevel ();

        return this;
    }

    private void WeaponBehaviourOnOnShooter ()
    {
        GameManager.Instance.FxTapNode (transform, null);
    }

    public override NodeComponent SetIndex (int xColumn, int yRow)
    {
        IsIdleMinerGold = !GameManager.Instance.IsBaseActiveWeapon (yRow);

        RefreshIndexActiveWeapon (yRow);

        return base.SetIndex (xColumn, yRow);
    }

    public override NodeComponent SetBusy (bool IsBusy)
    {
        IsMinerGold = IsBusy;

        return base.SetBusy (IsBusy);
    }

    public override NodeComponent SetEnable ()
    {
        IdleRegister ();

        weapon_behaviour.Active ();

        sprite_renderer.enabled = true;

        return base.SetEnable ();
    }

    public override NodeComponent SetDisable ()
    {
        IdleUnRegister ();

        weapon_behaviour.DeActive ();

        sprite_renderer.enabled = false;

        return base.SetDisable ();
    }

    public void SetPlaneMoving (BasePlaneMoving basePlaneMoving)
    {
        _BasePlaneMoving = basePlaneMoving;
    }

    public void RefreshIndexActiveWeapon (int row)
    {
        if (GameManager.Instance.IsBaseActiveWeapon (row))
        {
            weapon_behaviour.Resume ();
        }
        else
        {
            weapon_behaviour.Pause ();
        }
    }

    public override void RefreshLevel ()
    {
        base.RefreshLevel ();

        weapon_behaviour.RefreshLevelUpdated (level_upgrade);
    }

    #region Action

    public override NodeComponent TouchBusy ()
    {
        if (_BasePlaneMoving != null)
        {
            _BasePlaneMoving.Stop ();

            GameManager.Instance.SetItemBackToNode (GetIndexX (), GetIndexY (), _BasePlaneMoving, this);
        }

        _BasePlaneMoving = null;

        return this;
    }

    public override NodeComponent TouchHit ()
    {
        double profit      = _ItemNodeData.ProfitPerSec * _ItemNodeData.PerCircleTime * GameConfig.PercentCoinEarnFromHitItem;
        int    profit_unit = _ItemNodeData.ProfitPerSecUnit;

        Helper.FixNumber (ref profit, ref profit_unit);

        profit = profit * Mathf.Pow (_ItemNodeData.ProfitPerUpgradeCoefficient, level_upgrade);

        Helper.FixUnit (ref profit, ref profit_unit);

        if (profit < 1 && profit_unit == 0)
        {
            profit      = GameConfig.DefaultCoinEarn;
            profit_unit = 0;
        }

        GameManager.Instance.FxEarnCoin (profit, profit_unit, GetPosition ());
        GameManager.Instance.FxTapNode (transform, null);

        GameActionManager.Instance.InstanceFxTapCoins (GetPosition ());

        this.PlayAudioSound (AudioEnums.SoundId.TapOnItem);

        this.PostMissionEvent (MissionEnums.MissionId.TapOnItem);

        if (Random.Range (0.00f, 1.00f) < 0.25f)
        {
            this.PlayAudioSound (AudioEnums.SoundId.ItemTouchTalk);
        }

        return this;
    }

    public void EarnCoins ()
    {
        if (!IsIdleMinerGold)
            return;

        double profit      = _ItemNodeData.ProfitPerSec * _ItemNodeData.PerCircleTime;
        int    profit_unit = _ItemNodeData.ProfitPerSecUnit;

        Helper.FixNumber (ref profit, ref profit_unit);

        profit = profit * Mathf.Pow (_ItemNodeData.ProfitPerUpgradeCoefficient, level_upgrade);

        EarningManager.Instance.GetRealEarning (ref profit, ref profit_unit);

        if (profit < 1 && profit_unit == 0)
        {
            profit      = GameConfig.DefaultCoinEarn;
            profit_unit = 0;
        }

        GameManager.Instance.FxDisplayEarnCoin (profit, profit_unit, GetPosition ());
        GameManager.Instance.FxTapNode (transform, null);
    }

    public void IdleRegister ()
    {
        if (IsIdleGold == true)
            return;

        IsIdleGold = true;

        GameIdleAction.Instance.RegisterIdle (this, _ItemNodeData.PerCircleTime);
        EarningManager.Instance.RegisterData (_ItemNodeData);
        UIGameManager.Instance.UpdateTextProfitPerSec ();
    }

    public void IdleUnRegister ()
    {
        if (IsIdleGold == false)
            return;

        IsIdleGold = false;

        GameIdleAction.Instance.UnRegisterIdle (this);
        EarningManager.Instance.UnRegisterData (_ItemNodeData);
        UIGameManager.Instance.UpdateTextProfitPerSec ();
    }

    public override void SetStatePause (bool state, bool force_resume_state = false)
    {
        if (force_resume_state)
        {
            weapon_behaviour.Resume ();

            IsIdlePause = false;

            return;
        }

        if (state != IsIdlePause)
        {
            IsIdlePause = state;

            if (IsIdlePause)
            {
                RefreshIndexActiveWeapon (_YRow);
            }
            else
            {
                weapon_behaviour.Pause ();
            }
        }
    }

    #endregion

    #region Helper

    public override bool IsBusy ()
    {
        return IsMinerGold;
    }

    public bool IsStop ()
    {
        return !IsIdleGold;
    }

    #endregion
}