using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCollider : MonoBehaviour
{
    public event System.Action<Collider2D> _OnTriggerEnter;
    public event System.Action<Collider2D> _OnTriggerExit;

    private void OnTriggerEnter2D (Collider2D other)
    {
        if (_OnTriggerEnter != null)
            _OnTriggerEnter (other);
    }

    private void OnTriggerExit2D (Collider2D other)
    {
        if (_OnTriggerExit != null)
            _OnTriggerExit (other);
    }
}
