using System;
using UnityEngine;

using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon;
using UnityEditor.PackageManager;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.GameLift.Model;

namespace MlAgent.Clouds
{
    public class AWSTools
    {
        private static readonly Lazy<AWSTools> _instance = new Lazy<AWSTools>(() => new AWSTools());

        private AWSTools(){}

        public static AWSTools Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        BasicAWSCredentials credentials;
        RegionEndpoint regionEndpoint;

        public void Init(string AK, string SK, string regionCode)
        {
            Instance.credentials = LoadBasicCredentials(AK, SK);
            Instance.regionEndpoint = RegionEndpoint.GetBySystemName(regionCode);
        }

        public string UploadToS3Bucket(string bucketName, string filePath)
        {
            var s3Client = new AmazonS3Client(Instance.credentials, Instance.regionEndpoint);
            string uploadLocation = "";
            string s3Location = "";
            try
            {          
                s3Location = SetupBucket(s3Client, bucketName);
                uploadLocation = $"archive-{DateTime.Now.ToString("yyyy-MM-dd")}.zip";
                if (s3Location != null)
                {

                    var transferUtility = new TransferUtility(s3Client);
                    // 上传 ZIP 文件到 S3
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        FilePath = filePath,
                        Key = uploadLocation
                    };
                    // 上传空对象
                    transferUtility.Upload(uploadRequest);
                }

            }
            catch (AmazonS3Exception ex)
            {
                Debug.Log($"Error2: {ex.Message}");
                return null;
            }
            return $"{s3Location}/{uploadLocation}";
        }

        String SetupBucket(AmazonS3Client s3Client, string bucketName)
        {
            try
            {
                var bucketLocation = s3Client.GetBucketLocation(bucketName);
                // 如果成功获取存储桶位置, 则表示存储桶存在
            }
            catch (AmazonS3Exception)
            {
                // 如果发生 404 错误, 则表示存储桶不存在
                try
                {
                    var response = s3Client.PutBucket(bucketName);
                }
                catch (AmazonS3Exception ee)
                {
                    Debug.Log($"Error: {ee.Message}");
                    return null;
                }

            }

            return $"s3://{bucketName}";
        }

        public string ListBuckets()
        {
            //var ssoCreds = LoadSsoCredentials("default");
            //var credentials = LoadBasicCredentials(ssoCreds.GetCredentials().AccessKey, ssoCreds.GetCredentials().SecretKey);
            // var credentials = LoadBasicCredentials(AK, SK);
            var s3Client = new AmazonS3Client(Instance.credentials, Instance.regionEndpoint);

            var response = s3Client.ListBuckets();
            foreach (var bucket in response.Buckets)
            {
                Debug.Log(bucket.BucketName);
            }
            //Debug.Log($"\nSSO Profile:\n {ssoProfileClient.GetSessionToken()}");
            return "ok";
        }

        // Method to get SSO credentials from the information in the shared config file.
        static AWSCredentials LoadSsoCredentials(string profile)
        {
            var chain = new CredentialProfileStoreChain();
            if (!chain.TryGetAWSCredentials(profile, out var credentials))
                throw new Exception($"Failed to find the {profile} profile");
            return credentials;
        }

        static BasicAWSCredentials LoadBasicCredentials(string AK, string SK)
        {
            return new BasicAWSCredentials(AK, SK);
        }
    }
}


