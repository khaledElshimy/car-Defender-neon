using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletsBehaviour : MonoBehaviour, IBullets
{
    [Header ("Config")] [SerializeField] private SpriteRenderer sprite_renderer;

    [SerializeField] [Tooltip ("The size of texture where we need detect collision with enemy")]
    private Vector2 box_cast_size_renderer;

    [SerializeField] protected BulletsData BulletsProperty;

    #region Variables

    private bool IsRenderer;
    private bool IsUpdate;
    private bool IsRegister;

    private new Transform  transform;
    private     Vector3    position;
    private     Quaternion rotation;

    private   RaycastHit2D rayCastHit2D;
    protected Transform    transform_enemy;

    private Vector3 vector_forward;

    private float speed_moving;

    protected string enemy_tag;

    protected int level_weapon;

    protected double damage;
    protected int    damage_unit;

    protected bool IsCritDamage;

    #endregion

    #region System

    private void OnBecameInvisible ()
    {
        ReturnToPools ();
    }

    #endregion

    public void Init (Vector3 position_start, Vector3 angle_start, string tag_enemy, int level_weapon_upgrade)
    {
        transform = gameObject.transform;

        position = position_start;
        rotation = Quaternion.Euler (angle_start);

        transform.position    = position;
        transform.eulerAngles = angle_start;

        vector_forward = rotation * Vector.Vector3Up;

        speed_moving = (BulletsProperty.SpeedMoving + Contains.SpeedUpTimes * BulletsProperty.SpeedMoving) * 0.02f;
        enemy_tag    = tag_enemy;

        level_weapon = level_weapon_upgrade;

        IsUpdate = true;
    }

    public void Register ()
    {
        if (IsRegister)
            return;

        transform = gameObject.transform;

        BulletsManager.Instance.Register (this);

        IsRegister = true;
    }

    public void UnRegister ()
    {
        if (!IsRegister)
            return;

        BulletsManager.Instance.UnRegister (this);

        IsRegister = false;
    }

    public void IUpdate ()
    {
        if (!IsUpdate)
            return;

        rayCastHit2D    = Physics2D.BoxCast (position, box_cast_size_renderer, 90, Vector2.up, speed_moving);
        transform_enemy = rayCastHit2D.transform;

        position.y += vector_forward.y * speed_moving;
        position.x += vector_forward.x * speed_moving;
        position.z += vector_forward.z * speed_moving;

        if (!ReferenceEquals (transform_enemy, null) && transform_enemy.CompareTag (enemy_tag))
        {
            UnRegister ();
            ReturnToPools ();

            OnHit (position);
        }
    }

    public virtual void OnHit (Vector3 _position) { }

    public void IRenderer ()
    {
        if (IsRenderer)
            return;

        transform.position = position;
        transform.rotation = rotation;

        if (position.y > 11)
        {
            UnRegister ();
            ReturnToPools ();
        }

        IsRenderer = false;
    }

    public void ReturnToPools ()
    {
        PoolExtension.SetPool (BulletsProperty.BulletId, transform);
    }
}