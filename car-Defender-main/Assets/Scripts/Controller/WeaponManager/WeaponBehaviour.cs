using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour, IShooter
{
    [Header ("Handle")] [SerializeField] private Transform[] transform_holder;

    [Header ("Config")] [SerializeField] private float max_angle_each_bullet = 15;
    [SerializeField]                     private float min_angle_each_bullet = 5;

    public System.Action OnShooter;
    
    #region Variables

    private bool IsActiveShooter;
    private bool IsPauseShooter;

    private string enemy_Tag;

    private WeaponData.WeaponProperty WeaponProperty;

    private CoroutineHandle handle_shooter;

    private Vector3 position_shooter;

    private int weapon_level_updated;

    #endregion

    #region Action

    public void Init (WeaponData.WeaponProperty data, string tag_enemy)
    {
        WeaponProperty = data;
        enemy_Tag      = tag_enemy;
    }

    public void Active ()
    {
        if (IsActiveShooter)
            return;

        IsActiveShooter = true;

        handle_shooter = Timing.RunCoroutine (Enumerator_Fire ());
    }

    public void DeActive ()
    {
        if (!IsActiveShooter)
            return;

        IsActiveShooter = false;

        Timing.KillCoroutines (handle_shooter);
    }

    public void RefreshLevelUpdated (int level_upgraded)
    {
        weapon_level_updated = level_upgraded;
    }

    public void Pause ()
    {
        IsPauseShooter = true;
    }

    public void Resume ()
    {
        IsPauseShooter = false;
    }

    public void Fire ()
    {
        for (int i = 0; i < transform_holder.Length; i++)
        {
            position_shooter = transform_holder[i].position;

            var angle       = Random.Range (min_angle_each_bullet, max_angle_each_bullet);
            var angle_start = transform_holder[i].localEulerAngles.z + 0 - (WeaponProperty.NumberBullets - 1) / 2f * angle;

            for (int j = 0; j < WeaponProperty.NumberBullets; j++)
            {
                GameActionManager.Instance.InstanceBullets (WeaponProperty.Level, weapon_level_updated, position_shooter, enemy_Tag, new Vector3 (0, 0, angle_start + angle * j));

            }
                 
            GameActionManager.Instance.InstanceMuzzle (position_shooter, transform_holder[i].localEulerAngles);
        }

        if (OnShooter != null)
        {
            OnShooter ();
        }
        
        this.PlayAudioSound (AudioEnums.SoundId.Shoot);
    }

    #endregion

    #region Enumerator

    private IEnumerator<float> Enumerator_Fire ()
    {
        while (IsActiveShooter)
        {
            yield return Timing.WaitForSeconds (WeaponProperty.FireRate - Contains.SpeedUpTimes * WeaponProperty.FireRate);

            if (!IsPauseShooter)
            {
                Fire ();
            }
        }
    }

    #endregion
}