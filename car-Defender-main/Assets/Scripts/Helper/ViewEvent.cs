using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewEvent : MonoBehaviour
{
    public event System.Action OnVisible;
    public event System.Action OnInVisible;

    private void OnBecameInvisible ()
    {
        if (OnInVisible != null)
            OnInVisible ();
    }

    private void OnBecameVisible ()
    {
        if (OnVisible != null)
            OnVisible ();
    }
}
