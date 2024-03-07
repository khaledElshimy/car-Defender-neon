using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IShooter
{
    void Active ();
    void DeActive ();
    void Pause ();
    void Resume ();
    void Fire ();
}