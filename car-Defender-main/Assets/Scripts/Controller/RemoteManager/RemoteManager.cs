using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class RemoteManager : Singleton<RemoteManager>
{
    private bool IsInit;
    
    #region Variables

    private bool is_check_update;
    private bool is_fetch_value;

    #endregion

    #region System

    protected override void Awake ()
    {
        base.Awake ();

        InitRemoteConfig ();
    }


    #endregion

    #region Controller
  
    private void InitRemoteConfig ()
    {
        RemoteSettings.Completed += RemoteSettingsOnCompleted;
    }

    private void RemoteSettingsOnCompleted (bool arg1, bool arg2, int arg3)
    {
        RefreshValueNotification ();
        RefreshUpdate ();
    }

    private void RefreshValueNotification ()
    {
        Contains.time_to_push = RemoteSettings.GetInt ("notification_time", 36000);
    }

    private void CheckUpdate ()
    {

        long version = RemoteSettings.GetLong ("update_version", 0);
        long current_version = 0;

        var last_version_update = current_version;

        long.TryParse (Contains.LastVersionUpdate, out last_version_update);

        var is_force_update = RemoteSettings.GetBool ("update_skip");

        if (long.TryParse (Version.bundleVersion, out current_version))
        {
            if (version > current_version && version > last_version_update)
            {
                if (MessageManager.InstanceAwake () != null)
                {
                    var message = string.Empty;

                    #if UNITY_ANDROID
                    message = ApplicationLanguage.Text_description_update_android;
                    #elif UNITY_IOS
                     message = ApplicationLanguage.Text_description_update_ios;
                    #endif

                    MessageManager.Instance.ShowForceMessage (message, is_force_update, () =>
                    {
                        MessageManager.Instance.DisableForceHud ();
                        ApplicationManager.Instance.OpenUrlStore ();
                    }, () => { Contains.LastVersionUpdate = version.ToString(); });
                }
            }
        }
    }

    public void RefreshUpdate ()
    {
        if (is_check_update)
            return;

        is_check_update = true;

        StartCoroutine (Enumerator_check_for_update ());
    }

    #endregion

    #region Enumerator

    private IEnumerator Enumerator_check_for_update ()
    {
        while (!ApplicationManager.IsGameReady)
        {
            yield return Timing.WaitForOneFrame;
        }

        CheckUpdate ();
    }

    #endregion
}