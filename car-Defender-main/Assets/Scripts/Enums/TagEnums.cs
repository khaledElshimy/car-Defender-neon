using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TagEnums
{
    public enum TagId
    {
        Enemy,
        DeathBox
    }

    public readonly static string[] TagKey =
    {
        "Enemy",
        "DeathBox",
    };

    #region Helper

    public static string GetKey (TagId id)
    {
        return TagKey[(int) id];
    }

    #endregion
}
