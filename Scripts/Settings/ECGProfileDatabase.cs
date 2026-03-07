using UnityEngine;

using ColbyO.CardioSim.SO;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ProfileDatabase.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Settings
{
    [CreateAssetMenu(fileName = "DefaultECGDatabase", menuName = "ECG/Database")]
    internal sealed class ECGProfileDatabase : SODatabase<ECGProfile>
    {
        public ECGProfile GetEntry(string profileName)
        {
            return _database.Find(e => e.profileName.ToLower().CompareTo(profileName.ToLower()) == 0);
        }
    }
}
