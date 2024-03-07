using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [Header ("Animation")] [SerializeField]
    private Animation animation_controller;

    [SerializeField] private AnimationClip animation_clip_shake_camera;
    
    #region Fx

    public void FxShakeCamera ()
    {
        animation_controller.Stop();
        animation_controller.Play (animation_clip_shake_camera.name);
    }
    
    #endregion
}
