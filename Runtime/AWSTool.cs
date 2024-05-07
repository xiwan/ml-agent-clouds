using System;
using UnityEngine;

using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.SecurityToken.Model;

using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif


namespace MlAgent.Clouds
{
  public class AWSTools
  {
    private static readonly Lazy<AWSTools> _instance = new Lazy<AWSTools>(() => new AWSTools());

    private AWSTools() { }

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
      Instance.credentials = new BasicAWSCredentials(AK, SK);
      Instance.regionEndpoint = RegionEndpoint.GetBySystemName(regionCode);
    }

    public async Task<string> GetCallerInfoAsync()
    {
      var client = new AmazonSecurityTokenServiceClient(Instance.credentials, Instance.regionEndpoint);
      var response = await client.GetCallerIdentityAsync(new GetCallerIdentityRequest());
      string accountId = response.Account;
      return accountId;
    }

    public async Task<string> CallStack(string stackName, string ymlName, Dictionary<string, string> parameters)
    {
      var client = new AmazonCloudFormationClient(Instance.credentials, Instance.regionEndpoint);

      string cloudformationDirectory = Utils.checkDir("Cloudformation");
      string templatePath = $"{cloudformationDirectory}/{ymlName}.yml";
      Debug.Log(templatePath);
      try
      {
        var describeStacksRequest = new DescribeStacksRequest
        {
          StackName = stackName
        };
        var describeStacksResponse = await client.DescribeStacksAsync(describeStacksRequest);
        if (describeStacksResponse.Stacks.Count > 0)
        {
          var updateStackRequest = new UpdateStackRequest
          {
            StackName = stackName,
            TemplateBody = File.ReadAllText(templatePath),
            Capabilities = new List<string> { "CAPABILITY_IAM" },
            Parameters = parameters.Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList()
          };
          UnityEngine.Debug.Log(parameters.Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList());
          var updateStackResponse = await client.UpdateStackAsync(updateStackRequest);
          Debug.Log($"updateStackResponse: {updateStackResponse.HttpStatusCode}");
        }
        else
        {
          var createStackRequest = new CreateStackRequest
          {
            StackName = stackName,
            TemplateBody = File.ReadAllText(templatePath),
            Capabilities = new List<string> { "CAPABILITY_IAM" },
            Parameters = parameters.Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList()
          };
          UnityEngine.Debug.Log(parameters.Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList());
          var createStackResponse = await client.CreateStackAsync(createStackRequest);
          Debug.Log($"createStackResponse: {createStackResponse.HttpStatusCode}");
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error: {ex.Message}"); 

        //var createStackRequest = new CreateStackRequest
        //{
        //  StackName = stackName,
        //  TemplateBody = File.ReadAllText(templatePath),
        //  Capabilities = new List<string> { "CAPABILITY_IAM" },
        //  Parameters = parameters.Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList()
        //};
        //UnityEngine.Debug.Log(parameters.Select(p => new Parameter { ParameterKey = p.Key, ParameterValue = p.Value }).ToList());
        //var createStackResponse = await client.CreateStackAsync(createStackRequest);
        //Debug.Log($"createStackResponse: {createStackResponse.HttpStatusCode}");
      }
      finally
      {
        Debug.Log($"MonitorStackProgress: {stackName}");
        bool result = await MonitorStackProgress(client, stackName);
        Debug.Log($"MonitorStackProgress: {result}");
      }
      return "";
    }

    public async Task<bool> MonitorStackProgress(AmazonCloudFormationClient client, string stackName)
    {

      var describeStackEventsRequest = new DescribeStackEventsRequest
      {
        StackName = stackName
      };

      var stackEvents = new List<StackEvent>();
      string lastEventId = null;

      while (true)
      {
        var describeStackEventsResponse = await client.DescribeStackEventsAsync(describeStackEventsRequest);
        stackEvents.AddRange(describeStackEventsResponse.StackEvents);

        if (describeStackEventsResponse.NextToken == null)
        {
          break;
        }

        lastEventId = describeStackEventsResponse.StackEvents[0].EventId;
        describeStackEventsRequest.NextToken = describeStackEventsResponse.NextToken;
      }

      stackEvents.Reverse();

      foreach (var stackEvent in stackEvents)
      {
        if (lastEventId == null || stackEvent.EventId != lastEventId)
        {
          Debug.Log($"{stackEvent.Timestamp} {stackEvent.LogicalResourceId} {stackEvent.ResourceType} {stackEvent.ResourceStatus} {stackEvent.ResourceStatusReason}");
        }
      }

      var lastEvent = stackEvents[0];
      var isStackCompleted = lastEvent.ResourceType == "AWS::CloudFormation::Stack" &&
                          (lastEvent.ResourceStatus.ToString().EndsWith("COMPLETE") || lastEvent.ResourceStatus.ToString().EndsWith("FAILED"));

      return isStackCompleted;
    }

    public string UploadToS3Bucket(string bucketName, string filePath, string packageName)
    {
      var s3Client = new AmazonS3Client(Instance.credentials, Instance.regionEndpoint);
      string uploadLocation = "";
      string s3Location = "";
      try
      {
        s3Location = SetupBucket(s3Client, bucketName);
        uploadLocation = $"{packageName}-{DateTime.Now.ToString("yyyy-MM-dd")}.zip";
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
        else
        {
          throw new AmazonS3Exception("s3Location is Empty");
        }

      }
      catch (AmazonS3Exception ex)
      {
        Debug.Log($"UploadToS3Bucket: {ex.Message}");
        return null;
      }
      return $"{s3Location}/{uploadLocation}";
    }

    string SetupBucket(AmazonS3Client s3Client, string bucketName)
    {
      try
      {
        Debug.Log($"bucketLocation: {bucketName}");
        var bucketLocation = s3Client.GetBucketLocation(bucketName);
        // 如果成功获取存储桶位置, 则表示存储桶存在
        Debug.Log($"bucketLocation: {bucketLocation}");
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
          Debug.LogError($"SetupBucket: {ee.Message}");
          return null;
        }

      }

      return $"s3://{bucketName}";
    }

    // Method to get SSO credentials from the information in the shared config file.
    static AWSCredentials LoadSsoCredentials(string profile)
    {
      var chain = new CredentialProfileStoreChain();
      if (!chain.TryGetAWSCredentials(profile, out var credentials))
        throw new Exception($"Failed to find the {profile} profile");
      return credentials;
    }

  }


}


