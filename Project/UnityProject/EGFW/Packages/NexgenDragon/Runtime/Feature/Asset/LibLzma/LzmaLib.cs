using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class LzmaLib
{
#if UNITY_EDITOR
	[DllImport("lzmalib")]
	// level(1-9) = 5, dictSize = 20 means 1<<20
	public static extern int encodeLzmaFile(string inFilePath, string outFilePath, int level, int dictSize);
	
	[DllImport("lzmalib")]
	public static extern int decodeLzmaFile(string inFilePath, string outFilePath, int taskId);
	
	[DllImport("lzmalib")]
	public static extern int decodeLzmaBytesToFile(IntPtr inBytes,int byteLen, string outFilePath, int taskId);

	[DllImport("lzmalib")]
	public static extern float getCurrentDecodeProgress(int taskId);
#elif UNITY_IOS
	[DllImport("__Internal")]	
	public static extern int encodeLzmaFile(string inFilePath, string outFilePath, int level, int dictSize);
	
	[DllImport("__Internal")]
	public static extern int decodeLzmaFile(string inFilePath, string outFilePath, int taskId);

	[DllImport("__Internal")]
	public static extern int decodeLzmaBytesToFile(IntPtr inBytes,int byteLen, string outFilePath, int taskId);
	
	[DllImport("__Internal")]
	public static extern float getCurrentDecodeProgress(int taskId);
	
#elif UNITY_ANDROID
	[DllImport("lzmalib")]
	public static extern int encodeLzmaFile(string inFilePath, string outFilePath, int level, int dictSize);
	
	[DllImport("lzmalib")]
	public static extern int decodeLzmaFile(string inFilePath, string outFilePath, int taskId);

	[DllImport("lzmalib")]
	public static extern int decodeLzmaBytesToFile(IntPtr inBytes,int byteLen, string outFilePath, int taskId);
	
	[DllImport("lzmalib")]
	public static extern float getCurrentDecodeProgress(int taskId);
#else
	[DllImport("lzmalib")]
	public static extern int encodeLzmaFile(string inFilePath, string outFilePath, int level, int dictSize);
	
	[DllImport("lzmalib")]
	public static extern int decodeLzmaFile(string inFilePath, string outFilePath, int taskId);

	[DllImport("lzmalib")]
	public static extern int decodeLzmaBytesToFile(IntPtr inBytes,int byteLen, string outFilePath, int taskId);
	
	[DllImport("lzmalib")]
	public static extern float getCurrentDecodeProgress(int taskId);
#endif

}