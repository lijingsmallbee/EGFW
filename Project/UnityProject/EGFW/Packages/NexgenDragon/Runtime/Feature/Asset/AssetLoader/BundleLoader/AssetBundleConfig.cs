using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NexgenDragon;
public class AssetBundleConfig : Singleton<AssetBundleConfig> 
{
    private List<string> _constBundles = new List<string>();
    private string _shaderBundle;
    List<string> _otaLoadedBundles = new List<string>(128);
    public void SetConstBundles(List<string> bundles)
    {
        _constBundles.Clear();
        foreach(var bundle in bundles)
        {
            if (!_constBundles.Contains(bundle))
            {
                _constBundles.Add(bundle);
            }
        }
    }

    public void SetShaderBundle(string shaderBundle)
    {
        _shaderBundle = shaderBundle;
    }

    public bool IsConstBundle(string bundle)
    {
        foreach (var constBundle in _constBundles)
        {
            if (bundle.Contains(constBundle))
                return true;
        }
        return false;
    }

    public bool IsLuaBundle(string bundle)
    {
        return bundle.Contains("static+lua");
    }

    public static bool IsBundle(string fileName)
    {
        return fileName.StartsWith("ota+") || fileName.StartsWith("static+");
    }

    public bool IsShaderBundle(string bundle)
    {
        if(bundle.Contains("shader") || bundle.Contains("Shader"))
        {
            return true;
        }
        return bundle == _shaderBundle;
    }

    public void SetOtaLoadedBundles(List<string> bundles)
    {
        _otaLoadedBundles = bundles;
    }

    public List<string> GetOtaLoadedBundles()
    {
        return _otaLoadedBundles;
    }

    public bool TryGetBundleInfo(string info, out string md5, out string crc)
    {
        md5 = string.Empty;
        crc = string.Empty;
        
        if (string.IsNullOrEmpty(info))
        {
            return false;
        }
        var infos = info.Split('@');
        if (infos.Length == 2)
        {
            md5 = infos[0];
            crc = infos[1];
            return true;
        }

        return false;
    }

}
