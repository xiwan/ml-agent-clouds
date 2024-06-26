﻿using System;
using UnityEditor;
using UnityEngine;
using MlAgent.Clouds;
using System.IO.Compression;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace MlAgent.Editor
{
  public class MLAgentCloudsEditor : EditorWindow
  {
    private string[] tabTitles = { "General", "Single Node", "Ray on EKS" };
    private int selectedTab = 0;

    string[] osOptions = { "Linux", "Windows", "MacOS" };
    int osIndex = 0;
    string packageName = "";
    string dedicateServerLocation = "";
    string buildButton = "Build Dedicated Server";
    bool buildButtonFired = false;

    string awsAK = "";
    string awsSK = "";
    string targetS3Bucket = "";
    string[] regionOptions = { "us-east-1", "us-east-2", "us-west-1", "us-west-2" };
    int regionIndex = 0;

    string s3Button = "Send Dedicated Server to S3";
    bool s3ButtonFired = false;
    string uploadS3Location = "";

    string buildStatus = "";
    string imageName = "aws-mlagents";
    string dockerButton = "Build Docker Image and Push to ECR";
    bool dockerButtonFired = false;


    string cloudformationStatus = "";
    string cloudformationStackName = "MlagentSinglenodeStack";
    string cloudformationButton = "Create EC2/Sagemaker instance with GPU";
    string[] cloudformationTypeOptions = { "ec2", "sagemaker" };
    int cloudformationTypeIndex = 0;
    string[] cloudformationInstanceOptions = { "g4dn.xlarge", "g4dn.2xlarge" };
    int cloudformationInstanceIndex = 0;
    string[] cloudformationVolumeOptions = { "100", "200", "500", "1000" };
    int cloudformationVolumeIndex = 0;
    bool cloudformationButtonFired = false;

    //bool groupEnabled;
    //bool myBool = true;
    //float myFloat = 1.23f;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/ML-Agent-Clouds")]
    static void Init()
    {
      // Get existing open window or if none, make a new one:
      MLAgentCloudsEditor window = (MLAgentCloudsEditor)EditorWindow.GetWindow(typeof(MLAgentCloudsEditor));
      window.Show();

    }

    async void OnGUI()
    {
      selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);

      switch (selectedTab)
      {
        case 0:
          GeneralSteps();
          break;
        case 1:
          SingleNode();
          break;
        case 2:
          RayOnEKS();
          break;
      }
    }

    async void GeneralSteps()
    {
      GUILayout.Label("General Settings", EditorStyles.largeLabel);
      GUILayout.Label("Here you need to complete some basic environment setup, such as Unity's dedicated server packaging and uploading work, AWS's access key and secret key, region, and the location of the S3 bucket, etc.", EditorStyles.wordWrappedMiniLabel);

      DrawHorizontalLine();
      GUILayout.Label("1st.\t Need to specify OS for dedicated server. At least, 1 scene need check. Assets ouput to Build folder.", EditorStyles.wordWrappedMiniLabel);
      osIndex = EditorGUILayout.Popup("target platform", osIndex, osOptions);
      if (EditorBuildSettings.scenes.Length > 0)
      {
        packageName = System.IO.Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path);
      }
      packageName = EditorGUILayout.TextField("package name", packageName);
      GUILayout.Label("Local dedicated server location:", EditorStyles.wordWrappedMiniLabel);
      GUILayout.Label(dedicateServerLocation, EditorStyles.linkLabel);

      if (GUILayout.Button(buildButton))
      {
        buildButtonFired = true;
        if (buildButtonFired)
        {
          if (osIndex == 0)
          {
            packageName = string.IsNullOrEmpty(packageName) ? "default" : packageName;
            BuildScript.BuildLinuxServer(packageName);
            dedicateServerLocation = $"build/Linux/{packageName}";
            UnityEngine.Debug.Log($"dedicateServerLocation: {dedicateServerLocation}");
            Repaint();
          }
          buildButtonFired = false;
        }
      }


      DrawHorizontalLine();
      GUILayout.Label("2nd.\t Please input the AK/SK here, make sure sufficient privilege is granted.", EditorStyles.wordWrappedMiniLabel);
      awsAK = EditorGUILayout.TextField("AWS accessKey", awsAK);
      awsSK = EditorGUILayout.TextField("AWS secretKey", awsSK);
      targetS3Bucket = EditorGUILayout.TextField("s3 bucket", targetS3Bucket);
      regionIndex = EditorGUILayout.Popup("region", regionIndex, regionOptions);

      GUILayout.Label("Please use following s3 URI:", EditorStyles.wordWrappedMiniLabel);
      GUILayout.Label(uploadS3Location, EditorStyles.linkLabel);

      if (GUILayout.Button(s3Button))
      {
        s3ButtonFired = true;
        if (s3ButtonFired)
        {
          var localFolderPath = Path.Combine(Application.dataPath, "../Build/Linux", packageName);
          var localZipFilePath = Path.Combine(Application.dataPath, "../Build/archive.zip"); 
          try
          {

            ZipFile.CreateFromDirectory(localFolderPath, localZipFilePath);
            AWSTools.Instance.Init(awsAK, awsSK, regionOptions[regionIndex]);
            var uploadLocation = AWSTools.Instance.UploadToS3Bucket(targetS3Bucket, localZipFilePath, packageName);

            if (uploadLocation != null)
            {
              UnityEngine.Debug.Log($"uploadLocation: {uploadLocation}");
              uploadS3Location = uploadLocation;
              Repaint();
            }
          }
          catch (Exception ex)
          {
            UnityEngine.Debug.Log($"Error1: {ex.Message}");
          }
          finally
          {
            // 删除本地 ZIP 文件
            if (File.Exists(localZipFilePath))
            {
              File.Delete(localZipFilePath);
            }
          }

          s3ButtonFired = false;
        }
      }

      DrawHorizontalLine();
      GUILayout.Label("3rd.\t Need docker env, and build a docker image and push to AWS ECR (optional).  ", EditorStyles.wordWrappedMiniLabel);
      GUILayout.Label("\t If M-series chips, it's recommended to find an AMD64 (x86) environment.", EditorStyles.wordWrappedMiniLabel);
      GUILayout.Label("\t The manual usage is: build.sh {regionCode} {accountId} {imageName}", EditorStyles.wordWrappedMiniLabel);
      imageName = EditorGUILayout.TextField("Image Name", imageName);
      GUILayout.Label("Build outputs:", EditorStyles.wordWrappedMiniLabel);
      GUILayout.Label(buildStatus, EditorStyles.linkLabel);

      if (GUILayout.Button(dockerButton))
      {
        dockerButtonFired = true;
        if (dockerButtonFired)
        {
          buildStatus = "...";
          Repaint();
          AWSTools.Instance.Init(awsAK, awsSK, regionOptions[regionIndex]);
          var accountId = await AWSTools.Instance.GetCallerInfoAsync();
          DockerTools.Instance.Build(regionOptions[regionIndex], accountId, imageName, (object sender, DataReceivedEventArgs e) =>
          {
            if (!string.IsNullOrEmpty(e.Data))
            {
              UnityEngine.Debug.Log(e.Data); // 在Unity编辑器控制台输出
              buildStatus = e.Data;
              buildStatus = $"{accountId}.dkr.ecr.{regionOptions[regionIndex]}.amazonaws.com/{imageName}:latest";
              //Repaint();
            }
          });
          dockerButtonFired = false;
        }
      }

      DrawHorizontalLine();
    }

    async void SingleNode()
    {
      GUILayout.Label("Single Node Settings", EditorStyles.largeLabel);
      GUILayout.Label("You need to set up the AWS EC2/SageMaker environment.", EditorStyles.wordWrappedMiniLabel);

      DrawHorizontalLine();

      cloudformationStackName = EditorGUILayout.TextField("Stack Name", cloudformationStackName);
      cloudformationTypeIndex = EditorGUILayout.Popup("instance type", cloudformationTypeIndex, cloudformationTypeOptions);
      cloudformationInstanceIndex = EditorGUILayout.Popup("instance size", cloudformationInstanceIndex, cloudformationInstanceOptions);
      cloudformationVolumeIndex = EditorGUILayout.Popup("volume size", cloudformationVolumeIndex, cloudformationVolumeOptions);
      GUILayout.Label(cloudformationStatus, EditorStyles.linkLabel);

      if (GUILayout.Button(cloudformationButton))
      {
        cloudformationButtonFired = true;
        if (cloudformationButtonFired)
        {

          Dictionary<string, string> parameters = new Dictionary<string, string>
          {
              { "InstanceTypeParam", cloudformationInstanceOptions[cloudformationInstanceIndex] },
              { "RegionCodeParam", regionOptions[regionIndex]},
              { "VolumeSizeParam", cloudformationVolumeOptions[cloudformationVolumeIndex] }
          };

          cloudformationStatus = "...";
          Repaint();
          AWSTools.Instance.Init(awsAK, awsSK, regionOptions[regionIndex]);
          var accountId = await AWSTools.Instance.GetCallerInfoAsync();
          var ymlName = cloudformationTypeIndex == 0 ? "mlagent-ec2" : "mlagent-sagemaker";
          cloudformationStatus = ymlName;
          var response = await AWSTools.Instance.CallStack(cloudformationStackName, ymlName, parameters);
          cloudformationButtonFired = false;
        }
      }

    }

    async void RayOnEKS()
    {


    }

    void DrawHorizontalLine()
    {
      GUILayout.Space(5); // 添加一些垂直空间
      Rect rect = EditorGUILayout.GetControlRect(false, 1); // 获取一个1像素高的矩形区域
      rect.height = 1; // 设置矩形高度为1像素
      EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f)); // 绘制灰色的矩形
      GUILayout.Space(5); // 添加一些垂直空间
    }

    void Update()
    {
      //if (actionFired)
      //{

      //}
    }
  }

}



