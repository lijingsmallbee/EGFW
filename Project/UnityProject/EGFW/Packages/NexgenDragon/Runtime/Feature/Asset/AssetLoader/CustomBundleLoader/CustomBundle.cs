using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NexgenDragon
{
    //abstract class
    public abstract class CustomBundle
    {
        public struct CustomAssetHandle
        {
            public string assetName;
            public string assetPath;
            public byte[] assetBytes;
        }

        protected string customBundleName;
        public CustomBundle(string customBundleName)
        {
            this.customBundleName = customBundleName;
        }

        public abstract void OnWillOpen(string customFullPath, Func<string, string>  pathConverter);

        public abstract byte[] LoadFile(string fileName);

        public abstract int LoadFile(string fileName, ref byte[] outBytes);
        
        public abstract bool LoadFileAsync(string fileName, Action<CustomAssetHandle> assetHandle);

        public abstract bool FileExist(string fileName);

        public abstract void Unload();
    }


    public class DefaultCustomBundle : CustomBundle
    {
        private FileStream customBundleStream = null;
        public DefaultCustomBundle(string bundlePath) : base(bundlePath)
        {
            if (File.Exists(bundlePath))
            {
                customBundleStream = File.OpenRead(bundlePath);
            }
        }

        public override void OnWillOpen(string customFullPath, Func<string, string> pathConverter)
        {
            
        }

        public override byte[] LoadFile(string fileName)
        {
            if (customBundleStream == null) return null;

            return null;
        }

        public override int LoadFile(string fileName, ref byte[] outBytes)
        {
            var bytes = LoadFile(fileName);
            outBytes = bytes;
            return bytes.Length;
        }

        public override bool LoadFileAsync(string fileName, Action<CustomAssetHandle> assetHandle)
        {
            return false;
        }

        public override bool FileExist(string fileName)
        {
            return false;
        }

        public override void Unload()
        {
        }
    }
}
