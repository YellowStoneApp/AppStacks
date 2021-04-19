using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Route53;


namespace AppStacks
{
    public class MainServiceFargateStack : Stack
    {
        internal MainServiceFargateStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "MainServiceVpc", new VpcProps
            {
                MaxAzs = 3
            });

            var cluster = new Cluster(this, "MainServiceCluster", new ClusterProps
            {
                Vpc = vpc
            });

            var executionRole = new Role(this, "MainServiceExecutionRole-", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
            });

            var containerTaskRole = new Role(this, "MainServiceTaskRole-", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
            });
            
            // all permissions to s3.
            containerTaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonS3FullAccess"));
            containerTaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchFullAccess"));

            var taskDefinition = new FargateTaskDefinition(this, "MainServiceTaskDefinition", new FargateTaskDefinitionProps
            {
                TaskRole = containerTaskRole,
                ExecutionRole = executionRole,
            });

            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = "MainService",
            });

            var containerOptions = new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset("/Users/stopidRobot/RiderProjects/Tamarak/MainService"),
                Logging = logging,
            };
            

            var portMapping = new PortMapping
            {
                ContainerPort = 80,
                HostPort = 80,
            };
            
            taskDefinition
                .AddContainer("MainService", containerOptions)
                .AddPortMappings(portMapping);

            var zone = HostedZone.FromLookup(this, "YellowStone", new HostedZoneProviderProps
            {
                DomainName = "yellowstoneapp.io"
            });
            
            
            var serviceProps = new ApplicationLoadBalancedFargateServiceProps()
            {
                Cluster = cluster,
                DesiredCount = 2,
                TaskDefinition = taskDefinition,
                MemoryLimitMiB = 2048,
                PublicLoadBalancer = true,
                Certificate = Certificate.FromCertificateArn(this, "Certificate", "arn:aws:acm:us-west-2:217720823904:certificate/9333851f-05f5-4bdc-99f4-bd7f88f9ae32"),
                DomainName = "mainservice.yellowstoneapp.io",
                DomainZone = zone,
            };
            
            // create the load balanced Fargate service and make it public. 
            var service = new ApplicationLoadBalancedFargateService(this, "MainServiceFargateService", serviceProps);
            
            // Code Build 
            // var repo = new Repository(this, "MainServiceRepo");
            //
            // var githubSource = Source.GitHub(new GitHubSourceProps
            // {
            //     Owner = "rpbarnes",
            //     Repo = "YellowStoneApp/MainService",
            //     Webhook = true,
            //     WebhookFilters = new []
            //     {
            //         // this will trigger deploy when push to deploy
            //         FilterGroup.InEventOf(EventAction.PUSH).AndBranchIs("deploy")
            //     },
            // });
            //
            // var project = new Project(this, "MainServiceDeploymentProject", new ProjectProps
            // {
            //     ProjectName = StackName,
            //     Source = githubSource,
            //     Environment = new BuildEnvironment
            //     {
            //         BuildImage = LinuxBuildImage.AMAZON_LINUX_2_2,
            //         Privileged = true,
            //     },
            //     BuildSpec = BuildSpec.FromObject(new Dictionary<string, object>()
            //     {
            //         
            //     }) 
            //     
            // })

        }
    }
}