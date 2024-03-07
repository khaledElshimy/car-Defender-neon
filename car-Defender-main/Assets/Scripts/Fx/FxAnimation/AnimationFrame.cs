using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationFrame : MonoBehaviour
{
    [Header ("Renderer")] [SerializeField] private SpriteRenderer sprite_renderer;
    [Header ("Frame")] [SerializeField]    private Sprite[]       sprite_frames;
    [SerializeField]                       private int            fps_speed;

    private float fps_time;
    private float current_fps;
    private int   index_frame;
    private int   count;

    private void Start ()
    {
        if (fps_speed < 1)
            fps_speed = 1;

        current_fps = 0;
        fps_time    = 1f / (sprite_frames.Length * fps_speed);
        count       = sprite_frames.Length;
    }

    // Update is called once per frame
    void LateUpdate ()
    {
        current_fps += Time.deltaTime;

        if (current_fps < fps_time)
            return;

        index_frame++;

        sprite_renderer.sprite = sprite_frames[index_frame];

        if (index_frame == count - 1)
            index_frame = 0;

        current_fps = 0;
    }
}