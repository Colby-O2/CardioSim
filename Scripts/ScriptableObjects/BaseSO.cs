using UnityEngine;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    BaseSO.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.SO
{
    public abstract class BaseSO : ScriptableObject
    {
        [Header("Global SO Settings")]
        public int id = -1;
        public bool includeInDatabase = true;
    }
}
