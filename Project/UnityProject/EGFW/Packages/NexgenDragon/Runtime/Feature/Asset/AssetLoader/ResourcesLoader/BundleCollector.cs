using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NexgenDragon
{
    public static class BundleCollector
    {
#if !UNITY_EDITOR
        public static void Collect(string cacheIndex, string originPath)
        {

        }
#else
        public static void Collect(string cacheIndex, string originPath)
        {

            if (_resCollectOpen && Application.isPlaying) 
            {
                if (!_assetSet.Contains(originPath))
                {
                    // Log($"Collect: {cacheIndex}, {originPath}");
                    _assetSet.Add(originPath);
                    _allAssets.Add(new AssetData()
                    {
                        cacheIndex = cacheIndex,
                        originPath = originPath,
                        time = Time.unscaledTime,
                    });
                }
                
                // 
                foreach (var pair in _customCollector)
                {
                    pair.Value.Collect(cacheIndex, originPath);
                }
            }
        }

        private static Dictionary<string, AssetCollector> _customCollector = new Dictionary<string, AssetCollector>();

        public class AssetData
        {
            public string cacheIndex;
            public string originPath;
            public double time;
        }
        
        private static List<AssetData> _allAssets = new List<AssetData>();
        private static HashSet<string> _assetSet = new HashSet<string>();

        private static bool _resCollectOpen = false;
        
        private const string ResCollectOpenKey = "res_collect_open";

        public static void Init()
        {
            _resCollectOpen = LoadResCollectState();
            _allAssets.Clear();
            _assetSet.Clear();
            _customCollector.Clear();
            SoundCollector.Init();
        }

        public static void SaveResCollectState(bool value)
        {
            EditorPrefs.SetBool(ResCollectOpenKey, value);
        }

        public static bool LoadResCollectState()
        {
            var value = EditorPrefs.GetBool(ResCollectOpenKey, false);
            return value;
        }

        public static void OnResCollectStateChanged(bool state)
        {
            if (Application.isPlaying)
            {
                _resCollectOpen = state;
                Log("OnResCollectStateChanged: " + state);
            }
            SaveResCollectState(state);
        }

        public static string SaveDir = "__ArtData/Editor/BundleInfo/GuideResCollect";
        public static string SavePath = $"{SaveDir}/GuideResCollect.json";

        public static string GetGuideFolderPath()
        {
            return Path.Combine(Application.dataPath, SaveDir);
        }
        
        public static string GetGuideFolderAssetPath()
        {
            return $"Assets/{SaveDir}";
        }

        public static void SaveResCollectJson()
        {
            var fullPath = Application.dataPath + "/" + SavePath;
            var guideJsonPath = fullPath.Insert(fullPath.Length - 5, $"_{DateTime.Now:MMddHHmmss}");
            var dir = Path.GetDirectoryName(guideJsonPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonConvert.SerializeObject(_allAssets, Formatting.Indented);
            File.WriteAllText(guideJsonPath, json, Encoding.UTF8);
            Log($"收集的资源列表存储完毕, count: {_allAssets.Count}, path: {guideJsonPath}");
            AssetDatabase.ImportAsset(guideJsonPath.Substring(Application.dataPath.Length - 6));
            EditorUtility.RevealInFinder(guideJsonPath);
        }
        
        public static void ClearResCollectJson()
        {
            var dataPath = Application.dataPath;
            AssetDatabase.StartAssetEditing();
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(dataPath + "/" + SavePath)))
            {
                File.Delete(file);
                Log($"清理资源: {file}");
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        public static HashSet<string> GetGuideAssets()
        {
            Debug.Log($"[GetGuideAssets]enter");
            var guideAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = Directory.GetFiles(GetGuideFolderPath(), "*.json");
            foreach (var file in files)
            {
                Debug.Log($"[GetGuideAssets]file:{file}");
                var datas = JsonConvert.DeserializeObject<List<AssetData>>(File.ReadAllText(file, Encoding.UTF8));
                Debug.Log($"[GetGuideAssets]file:{file}, datas:{datas.Count}");
                foreach (var assetData in datas)
                {
                    guideAssets.Add(assetData.cacheIndex);
                }
                Debug.Log($"[GetGuideAssets]file:{file}, guideAssets:{guideAssets.Count}");
            }
            Debug.Log($"[GetGuideAssets][guideAssets]count:{guideAssets.Count}");
            if (!Application.isBatchMode)
            {
                Debug.Log($"[GetGuideAssets][guideAssets]{JsonConvert.SerializeObject(guideAssets, Formatting.Indented)}");
            }
            return guideAssets;
        }

        public static Dictionary<string, AssetData> GetAllAssets()
        {
            var allDatas = new Dictionary<string, AssetData>();
            var fullPath = Application.dataPath + "/" + SaveDir;
            var files = Directory.GetFiles(fullPath, "*.json");
            foreach (var file in files)
            {
                var timeIndex = file.LastIndexOf('_');
                var timeStr = file.Substring(timeIndex + 1, file.Length - timeIndex - 6);
                var baseTime = long.Parse(timeStr);
                var datas = JsonConvert.DeserializeObject<List<AssetData>>(File.ReadAllText(file, Encoding.UTF8));
                foreach (var assetData in datas)
                {
                    var key = assetData.originPath;
                    if (!allDatas.ContainsKey(key))
                    {
                        allDatas[key] = assetData;
                        assetData.time = baseTime - 3 * 3600 + assetData.time;
                    }
                }
            }
            return allDatas;
        }

        public static void Log(string log)
        {
            Debug.LogFormat(LogType.Log, LogOption.None, null, "[res_collect] {0}", log);
        }


#region WorldBase
        public static string SaveWorldBaseDir = "__ArtData/Editor/BundleInfo/WorldBaseResCollect";

        public static string GetWorldBaseFolderPath()
        {
            return Path.Combine(Application.dataPath, SaveWorldBaseDir);
        }
        
        public static string GetWorldBaseFolderAssetPath()
        {
            return $"Assets/{SaveWorldBaseDir}";
        }
        
        public static HashSet<string> GetWorldBaseAssets()
        {
            var guideAssets = new HashSet<string>();
            var files = Directory.GetFiles(GetWorldBaseFolderPath(), "*.json");
            foreach (var file in files)
            {
                var datas = JsonConvert.DeserializeObject<List<AssetData>>(File.ReadAllText(file, Encoding.UTF8));
                foreach (var assetData in datas)
                {
                    guideAssets.Add(assetData.cacheIndex);
                }
            }
            return guideAssets;
        }

        public static Dictionary<string, AssetData> GetAllWorldBaseAssets()
        {
            var allDatas = new Dictionary<string, AssetData>();
            var fullPath = Application.dataPath + "/" + SaveWorldBaseDir;
            var files = Directory.GetFiles(fullPath, "*.json");
            foreach (var file in files)
            {
                var timeIndex = file.LastIndexOf('_');
                var timeStr = file.Substring(timeIndex + 1, file.Length - timeIndex - 6);
                var baseTime = long.Parse(timeStr);
                var datas = JsonConvert.DeserializeObject<List<AssetData>>(File.ReadAllText(file, Encoding.UTF8));
                foreach (var assetData in datas)
                {
                    var key = assetData.originPath;
                    if (!allDatas.ContainsKey(key))
                    {
                        allDatas[key] = assetData;
                        assetData.time = baseTime - 3 * 3600 + assetData.time;
                    }
                }
            }
            return allDatas;
        }
        
        public static void ClearWorldBaseResCollectJson()
        {
            var dataPath = Application.dataPath;
            AssetDatabase.StartAssetEditing();
            foreach (var file in Directory.GetFiles(SaveWorldBaseDir))
            {
                File.Delete(file);
                Log($"清理资源: {file}");
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
#endregion

        public static void StartCustomRecrod(string collectTag, string folder)
        {
            if (!_customCollector.TryGetValue(collectTag, out var collector))
            {
                _customCollector.Add(collectTag, new AssetCollector(collectTag, folder));
            }
        }
        
        public static void StopCustomRecrod(string collectTag)
        {
            if (_customCollector.TryGetValue(collectTag, out var collector))
            {
                collector.Stop();
            }
        }


        private class AssetCollector
        {
            private string tag;
            private string savePath;

            public AssetCollector(string tag, string savePath)
            {
                this.tag = tag;
                this.savePath = savePath;
            }
            
            private static List<AssetData> _allAssets = new List<AssetData>();
            private static HashSet<string> _assetSet = new HashSet<string>();

            public void Collect(string cacheIndex, string originPath)
            {
                if (!_assetSet.Contains(originPath))
                {
                    // Log($"Collect: {cacheIndex}, {originPath}");
                    _assetSet.Add(originPath);
                    _allAssets.Add(new AssetData()
                    {
                        cacheIndex = cacheIndex,
                        originPath = originPath,
                        time = Time.unscaledTime,
                    });
                }
            }

            public void Stop()
            {
                var fullPath = Path.Combine(savePath, $"collect_{tag}_{DateTime.Now:yyMMddHHmmss}.json");
                var dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var json = JsonConvert.SerializeObject(_allAssets, Formatting.Indented);
                File.WriteAllText(fullPath, json, Encoding.UTF8);
                Log($"{tag}收集的资源列表存储完毕, count: {_allAssets.Count}, path: {fullPath}");
                EditorUtility.RevealInFinder(fullPath);
            }
        }

#region sound

        /// <summary>
        /// 大世界基础包音频
        /// </summary>
        public static Dictionary<string, HashSet<string>> GetWorldBaseSoundAssets()
        {
            return SoundCollector.GetWorldBaseAssets();
        }

        /// <summary>
        /// 新手关
        /// </summary>
        public static Dictionary<string, HashSet<string>> GetSoundAssets()
        {
            return SoundCollector.GetAssets();
        }

        public static void SaveSoundResCollect()
        {
            SoundCollector.SaveToJsonFile();
        }
            
        public static void CleareSoundResCollectJson()
        {
            SoundCollector.CleareCollectJson();
        }
        
        public static void CollectSoundWem(string wemName)
        {
            if (_resCollectOpen && Application.isPlaying)
            {
                SoundCollector.CollectWem(wemName);
            }
        }
        
        public static void CollectSoundBank(string bankName)
        {
            if (_resCollectOpen && Application.isPlaying)
            {
                SoundCollector.CollectBank(bankName);
            }
        }

        public static void CollectSoundEvent(string eventName)
        {
            // NLogger.LogChannel("CollectSoundEvent", eventName);
            if (_resCollectOpen && Application.isPlaying)
            {
                SoundCollector.CollectEvent(eventName);
            }
        }
        
        /// <summary>
        /// 音效资源收集
        /// </summary>
        public static class SoundCollector
        {
            public const string BANK_KEY = "bank";
            public const string EVENT_KEY = "event";
            public const string WEM_KEY = "wem";
            
            private static string _saveFolderForWorldBase = "__ArtData/Editor/BundleInfo/WorldBaseSoundResCollect";
            private static string _saveFolder = "__ArtData/Editor/BundleInfo/SoundResCollect";
            private static string _savePath = $"{_saveFolder}/SoundResCollect.json";
            private static HashSet<string> _wemNames = new HashSet<string>();
            private static HashSet<string> _bankNames = new HashSet<string>();
            private static HashSet<string> _eventNames = new HashSet<string>();

            public static void CollectWem(string wemName)
            {
                _wemNames.Add(wemName);
            }
            
            public static void CollectBank(string bankName)
            {
                _bankNames.Add(bankName);
            }

            public static void CollectEvent(string eventName)
            {
                _eventNames.Add(eventName);
            }

            public static void Init()
            {
                _bankNames.Clear();
                _eventNames.Clear();
            }

            /// <summary>
            /// 强制进包的手动加进去
            /// </summary>
            private static void ProcessForceInclude(HashSet<string> banks, HashSet<string> eventNames)
            {
                banks.Add("Init");
            }
            
            public static void SaveToJsonFile()
            {
                // 有些强制进包的塞进去
                ProcessForceInclude(_bankNames,_eventNames);
                //
                var fullPath = Application.dataPath + "/" + _savePath;
                var JsonPath = fullPath.Insert(fullPath.Length - 5, $"_{DateTime.Now:MMddHHmmss}");
                var dir = Path.GetDirectoryName(JsonPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var data = new Dictionary<string, List<string>>()
                {
                    {BANK_KEY, _bankNames.ToSortedList()},
                    {EVENT_KEY, _eventNames.ToSortedList()},
                    {WEM_KEY, _wemNames.ToSortedList()}
                };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(JsonPath, json, Encoding.UTF8);
                Log($"收集的资源列表存储完毕, _eventNames: {_eventNames.Count}, _bankNames: {_bankNames.Count}, _wemNames:{_wemNames.Count}, path: {JsonPath}");
                AssetDatabase.ImportAsset(JsonPath.Substring(Application.dataPath.Length - 6));
                EditorUtility.RevealInFinder(JsonPath);
            }

            public static void CleareCollectJson()
            {
                AssetDatabase.StartAssetEditing();
                foreach (var file in Directory.GetFiles(_saveFolder))
                {
                    File.Delete(file);
                    Log($"清理资源: {file}");
                }
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            public static Dictionary<string, HashSet<string>> GetAssets()
            {
                // 将所有json里的数据合并到一起
                var bankNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var eventNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var fullPath = Path.Combine(Application.dataPath, _saveFolder);
                var files = Directory.GetFiles(fullPath, "*.json");
                foreach (var file in files)
                {
                    var datas = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText(file, Encoding.UTF8));
                    var banks = datas[BANK_KEY];
                    if (banks != null)
                    {
                        foreach (var o in banks)
                        {
                            bankNames.Add(o);
                        }
                    }
                    var events = datas[EVENT_KEY];
                    if (events != null)
                    {
                        foreach (var o in events)
                        {
                            eventNames.Add(o);
                        }
                    }
                }
                
                //
                ProcessForceInclude(bankNames,eventNames);
                //
                var result = new Dictionary<string, HashSet<string>>()
                {
                    {BANK_KEY, bankNames},
                    {EVENT_KEY, eventNames}
                };
                return result;
            }
            
            public static Dictionary<string, HashSet<string>> GetWorldBaseAssets()
            {
                // 将所有json里的数据合并到一起
                var bankNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var eventNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var fullPath = Path.Combine(Application.dataPath, _saveFolderForWorldBase);
                var files = Directory.GetFiles(fullPath, "*.json");
                foreach (var file in files)
                {
                    var datas = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText(file, Encoding.UTF8));
                    var banks = datas[BANK_KEY];
                    if (banks != null)
                    {
                        foreach (var o in banks)
                        {
                            bankNames.Add(o);
                        }
                    }
                    var events = datas[EVENT_KEY];
                    if (events != null)
                    {
                        foreach (var o in events)
                        {
                            eventNames.Add(o);
                        }
                    }
                }

                //
                var result = new Dictionary<string, HashSet<string>>()
                {
                    {BANK_KEY, bankNames},
                    {EVENT_KEY, eventNames}
                };
                return result;
            }
        }

        public static List<T> ToSortedList<T>(this HashSet<T> hashSet)
        {
            if (hashSet == null) return null;
            var list = hashSet.ToList();
            list.Sort();
            return list;
        }
		
        public static T[] ToSortedArray<T>(this HashSet<T> hashSet)
        {
            if (hashSet == null) return null;
            var list = hashSet.ToList();
            list.Sort();
            return list.ToArray();
        }
#endregion

#endif
    }
}

