using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class BulletsManager : Singleton<BulletsManager>
{
    private readonly List<IBullets> iBullets = new List<IBullets> ();
    private          int            size_bullets;

    private bool IsUpdate;

    private CoroutineHandle handle_Update;

    #region System

    protected override void Awake ()
    {
        base.Awake ();

        Init ();
    }

    protected override void OnDestroy ()
    {
        IsUpdate = false;

        Timing.KillCoroutines (handle_Update);

        base.OnDestroy ();
    }

    #endregion

    #region Enumerator

    private IEnumerator<float> Enumerator_Update ()
    {
        while (IsUpdate)
        {
            for (int i = 0; i < size_bullets; i++)
            {
                var item = iBullets[i];

                item.IUpdate ();
                item.IRenderer ();
            }

            yield return Timing.WaitForSeconds (0.0167f);
        }
    }

    #endregion

    #region Action

    private void Init ()
    {
        IsUpdate = true;

        handle_Update = Timing.RunCoroutine (Enumerator_Update (), Segment.LateUpdate);
    }

    public void Register (IBullets value)
    {
        iBullets.Add (value);
        size_bullets++;
    }

    public void UnRegister (IBullets value)
    {
        iBullets.Remove (value);
        size_bullets--;
    }

    #endregion
}