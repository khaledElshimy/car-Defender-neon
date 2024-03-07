using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBullets
{
    void Register ();
    void UnRegister ();
    void IUpdate ();
    void IRenderer ();
}