#if MO_LOG || UNITY_DEBUG || MO_DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace NexgenDragon
{
    public class AssetCollector
    {
        private static bool _isOpen = false;

        private static readonly HashSet<string> _assets = new HashSet<string>();
        private static readonly HashSet<string> _soundEvent = new HashSet<string>();
        private static readonly HashSet<string> _soundBank = new HashSet<string>();

        public static void OpenAndStart()
        {
            _assets.Clear();
            _soundEvent.Clear();
            _soundBank.Clear();
            _isOpen = true;
        }
        
        public static void CloseAndSave()
        {
            _isOpen = false;
            try
            {
                var ht = new Hashtable();
                ht["assets"] = _assets;
                ht["soundEvents"] = _soundEvent;
                ht["soundBanks"] = _soundBank;

                var result = JsonConvert.SerializeObject(ht, Formatting.Indented);
                var filepath = Path.Combine(AssetManager.Instance.IOtool.GetPersistentDataPath(),$"AssetCollector_{DateTime.Now:yy-MM-dd.HHmmss}.log.json");
               File.WriteAllText(filepath, result, Encoding.UTF8);
            }
            catch (Exception e)
            {
                NLogger.Error( e.ToString());
            }
        }
        
        public static void Collect(string cacheIndex, string originPath)
        {
            if (!_isOpen) return;

            _assets.Add(cacheIndex);
        }
        
        public static void CollectSoundEvent(string eventName)
        {
            if (!_isOpen) return;

            _soundEvent.Add(eventName);
        }
        
        public static void CollectSoundBank(string bankName)
        {
            if (!_isOpen) return;

            _soundBank.Add(bankName);
        }
        
        // public static void CollectSoundWem(string wemName)
        // {
        //     if (!_isOpen) return;
        //
        // }
    }
}

#endif
