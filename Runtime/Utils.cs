#define DEV_MODE

using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
#endif

namespace MlAgent.Clouds
{
  public static class Utils
  {
    public static string GetInstalledPackagePath(string packageName)
    {
#if UNITY_EDITOR
      ListRequest listRequest = Client.List(true, true);

      while (listRequest.Status == StatusCode.InProgress)
      {
        // 等待列表请求完成
      }

      if (listRequest.Status == StatusCode.Success)
      {
        foreach (var package in listRequest.Result)
        {
          if (package.name == packageName)
          {
            return package.resolvedPath;
          }
        }
      }
#endif
      return null;
    }

    public static string checkDir(string subfolderName) {

      string parentDirectory = "";
      string childDirectory = "";

#if DEV_MODE
      childDirectory = Path.Combine(Application.dataPath, parentDirectory, $"CoreFramework/Runtime/{subfolderName}");
#else
      parentDirectory = Utils.GetInstalledPackagePath("com.benxiwan.ml-agent-clouds");
      childDirectory = Path.Combine(Application.dataPath, parentDirectory, "Runtime/Cloudformation");
#endif

      // 确保目录存在
      if (!Directory.Exists(childDirectory))
      {
        Debug.LogError("Not valid Path");
        throw new Exception($"Not valid {subfolderName} Path"); 
      }

      return childDirectory;
    }

    

  }

}

