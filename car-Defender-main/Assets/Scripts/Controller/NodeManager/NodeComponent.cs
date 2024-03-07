using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class NodeComponent : MonoBehaviour
{
    #region Variables

    #endregion

    #region Variables

    protected ItemNodeData _ItemNodeData;

    protected int _IndexId;
    protected int _XColumn;
    protected int _YRow;

    private bool is_unbox;

    protected int level_upgrade;

    #endregion

    #region Action

    public virtual NodeComponent Init (ItemNodeData item_node_data)
    {
        _ItemNodeData = item_node_data;
        _IndexId      = GetInstanceID ();
        
        return this;
    }

    public virtual NodeComponent TouchBusy ()
    {
        return this;
    }

    public virtual NodeComponent TouchHit ()
    {
        return this;
    }

    #endregion

    public Vector3 GetPosition ()
    {
        return transform.position;
    }

    public virtual NodeComponent SetEnable ()
    {
        return this;
    }

    public virtual NodeComponent SetPosition (Vector3 position)
    {
        transform.position = position;

        return this;
    }

    public virtual NodeComponent SetIndex (int xColumn, int yRow)
    {
        _XColumn = xColumn;
        _YRow    = yRow;
        return this;
    }

    public virtual NodeComponent SetDisable ()
    {
        return this;
    }

    public virtual NodeComponent SetBusy (bool isBusy)
    {
        return this;
    }

    public virtual NodeComponent SetUnbox (bool isUnbox)
    {
        is_unbox = isUnbox;

        return this;
    }

    public virtual void SetStatePause (bool state, bool force_resume_state = false) { }

    public virtual void RefreshLevel ()
    {
        level_upgrade = PlayerData.GetNumberUpgradeItemProfitCoefficient (_ItemNodeData.Level);
    }

    public virtual void ReturnToPool ()
    {
        PoolExtension.SetPool (_ItemNodeData.ItemPoolId, transform);
    }

    #region Helper

    public int GetIndexX ()
    {
        return _XColumn;
    }

    public int GetIndexY ()
    {
        return _YRow;
    }

    public int GetLevel ()
    {
        return _ItemNodeData.Level;
    }

    public int GetExp ()
    {
        return _ItemNodeData.Exp;
    }

    public int GetId ()
    {
        return _IndexId;
    }

    public virtual bool IsBusy ()
    {
        return true;
    }

    public virtual PoolEnums.PoolId GetPoolId ()
    {
        return _ItemNodeData.ItemPoolId;
    }

    public virtual string GetKey ()
    {
        return string.Empty;
    }

    public virtual bool IsUnbox ()
    {
        return is_unbox;
    }

    #endregion
}