using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    SODatabase.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.SO
{
    public abstract class SODatabase<TBase> : ScriptableObject where TBase : BaseSO
    {
        [Header("Settings")]
        [SerializeField] private bool _useRawSO = true;
        
        [Header("Database")]
        [SerializeField] protected List<TBase> _database;

        private void AddItem(TBase so)
        {
            if (_useRawSO) _database.Add(so);
            else _database.Add(ScriptableObject.Instantiate(so));
        }

        [ContextMenu(itemName: "Init Database")]
        public void InitDatabase()
        {
            ClearDatabase();

            List<TBase> foundTBases = Resources.LoadAll<TBase>("").OrderBy(e => e.id).Where(e => e.includeInDatabase).ToList();

            List<TBase> hasIDInRange = foundTBases.Where(e => e.id != -1 && e.id < foundTBases.Count).OrderBy(e => e.id).ToList();
            List<TBase> hasIDNotInRange = foundTBases.Where(e => e.id != -1 && e.id >= foundTBases.Count).OrderBy(e => e.id).ToList();
            List<TBase> noID = foundTBases.Where(e => e.id <= -1).ToList();

            int index = 0;
            for (int i = 0; i < foundTBases.Count; i++)
            {
                TBase newTBase;
                newTBase = hasIDInRange.Find(e => e.id == i);
                if (newTBase != null) AddItem(newTBase);
                else if (index < noID.Count)
                {
                    noID[index].id = i;
                    newTBase = noID[index];
                    index++;
                    AddItem(newTBase);
                }
            }

            foreach (TBase item in hasIDNotInRange)
            {
                item.id = _database.Count;
                AddItem(item);
            }
        }

        [ContextMenu(itemName: "Clear Database")]
        public void ClearDatabase()
        {
            _database = new();

            List<TBase> foundTBases = Resources.LoadAll<TBase>("").ToList();
            foreach (TBase item in foundTBases)
            {
                item.id = -1;
            }
        }

        public List<TBase> GetAllEntries()
        {
            return new List<TBase>(_database);
        }

        public List<TBase> Where(Func<TBase, bool> pred)
        {
            return _database.Where(pred).ToList();
        }

        public TBase GetEntry(int id)
        {
            return _database.Find(e => e.id == id);
        }

        public TBase GetEntry<T>(int id) where T : TBase
        {
            TBase card = _database.Find(e => e.id == id);

            if (card != null && card.GetType().IsAssignableFrom(typeof(T))) return card;
            else return null;
        }
    }
}
