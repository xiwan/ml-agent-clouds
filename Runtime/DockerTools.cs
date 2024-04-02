using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MlAgent.Clouds
{
    public class DockerTools
    {
        private static readonly Lazy<DockerTools> _instance = new Lazy<DockerTools>(() => new DockerTools());

        private DockerTools() { }

        public static DockerTools Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        bool isDockerRunning()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "docker";
                process.StartInfo.Arguments = "info";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return true;
            }
            catch (Exception ex)
            {
                // Docker守护进程未运行
                UnityEngine.Debug.LogError(ex.Message);
            }
            return false;
        }

        private Process process;
        private StringBuilder outputBuilder;

        // 定义委托类型
        public delegate void OutputDataReceivedHandler(object sender, DataReceivedEventArgs e);

        public void Build(
            string regionCode,
            string accountId,
            string imageName,
            OutputDataReceivedHandler outputHandler)
        {
            var isRunning = isDockerRunning();
            if (!isRunning)
            {
                UnityEngine.Debug.LogError("Make sure docker daemon is ruuning!");
                return;
            }

            //string currentDirectory = Directory.GetCurrentDirectory();
            //UnityEngine.Debug.Log("Current Directory: " + currentDirectory);
            //string dockerBuildDirectory = "./Container";
            string parentDirectory = "../Library/PackageCache/com.benxiwan.ml-agent-clouds@2.0.2";

            UnityEngine.Debug.Log(parentDirectory);
            string dockerBuildDirectory = Path.Combine(Application.dataPath, parentDirectory, "CoreFramework/Container");

            // 确保目录存在
            if (!Directory.Exists(dockerBuildDirectory))
            {
                UnityEngine.Debug.LogError("Not valid Path");
                return;
            }

            try
            {
                // Directory.SetCurrentDirectory(dockerBuildDirectory);

                // 在这里替换为你要执行的shell脚本的路径和文件名
                string scriptPath = $"{dockerBuildDirectory}/build.sh {regionCode} {accountId} {imageName}";
                UnityEngine.Debug.Log(scriptPath);
                // 启动shell脚本进程
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash", // 使用bash作为shell
                    Arguments = $"-c \"{scriptPath}\"", // 传递shell脚本路径作为参数
                    RedirectStandardOutput = true, // 重定向标准输出
                    UseShellExecute = false, // 不使用操作系统shell
                    CreateNoWindow = true // 不创建新窗口
                };

                process = new Process { StartInfo = processStartInfo };
                process.OutputDataReceived += (sender, args) => outputHandler(sender, args); ;
                process.Start();
                process.BeginOutputReadLine();

                outputBuilder = new StringBuilder();

                process.WaitForExit();
                UnityEngine.Debug.Log("======DONE=======");
            }
            catch (Exception ex)
            {
                // Docker守护进程未运行
                UnityEngine.Debug.LogError(ex.Message);
            }
        }

        public string GetOutput()
        {
            return outputBuilder.ToString();
        }
    }

}


