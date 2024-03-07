using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class FxTextDamage : MonoBehaviour
{
    [SerializeField] private TextMeshPro      text_damage;
    [SerializeField] private PoolEnums.PoolId PoolId;

    public void Init (Vector3 position, string value)
    {
        text_damage.text     = value;
        transform.position   = position;
        transform.localScale = Vector.Vector3Half;

        transform.DOComplete (true);
        transform.DOScale (1f, Durations.DurationMovingUpFx).SetEase (Ease.OutBack);
        transform.DOMoveY (position.y + 1, Durations.DurationMovingUpFx).OnComplete (() => { PoolExtension.SetPool (PoolId, transform); });
    }
}