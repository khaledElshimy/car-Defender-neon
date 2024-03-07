using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy
{
   void IUpdate ();
   void IRenderer ();
   EnemyEnums.EnemyId GetEnemyId ();
   void Remove ();
}
