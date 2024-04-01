using System;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using MlAgent.Clouds;
using System.IO.Compression;
using System.IO;

namespace MlAgent.Editor
{
    public class MLAgentCloudsEditor : EditorWindow
    {
        string[] osOptions = { "Linux", "Windows", "MacOS" };
        int osIndex = 0;

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


        bool groupEnabled;
        bool myBool = true;
        float myFloat = 1.23f;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/ML-Agent-Clouds")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            MLAgentCloudsEditor window = (MLAgentCloudsEditor)EditorWindow.GetWindow(typeof(MLAgentCloudsEditor));
            window.Show();

        }

        void OnGUI()
        {
            GUILayout.Label("ML-Agent-Clouds Settings", EditorStyles.largeLabel);

            DrawHorizontalLine();
            GUILayout.Label("1st. Need to specify OS for dedicated server. Assets ouput to Build folder.", EditorStyles.wordWrappedMiniLabel);
            osIndex = EditorGUILayout.Popup("target platform", osIndex, osOptions);

            if (GUILayout.Button(buildButton))
            {
                buildButtonFired = true;
                if (buildButtonFired)
                {
                    if (osIndex == 0)
                    {
                        BuildScript.BuildLinuxServer();
                    }
                    buildButtonFired = false;
                }
            }

            DrawHorizontalLine();
            GUILayout.Label("2nd. Please input the AK/SK here, make sure sufficient privilege is granted.", EditorStyles.wordWrappedMiniLabel);
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
                    var localFolderPath = Path.Combine(Application.dataPath, "../Build/Linux");
                    var localZipFilePath = Path.Combine(Application.dataPath, "../Build/archive.zip");
                    try
                    {
                        
                        ZipFile.CreateFromDirectory(localFolderPath, localZipFilePath);
                        AWSTools.Instance.Init(awsAK, awsSK, regionOptions[regionIndex]);
                        var uploadLocation = AWSTools.Instance.UploadToS3Bucket(targetS3Bucket, localZipFilePath);

                        if (uploadLocation != null)
                        {
                            Debug.Log($"uploadLocation: {uploadLocation}");
                            uploadS3Location = uploadLocation;
                            Repaint();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Error1: {ex.Message}");
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
            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();
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



