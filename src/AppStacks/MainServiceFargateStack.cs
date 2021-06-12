using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Route53;


namespace AppStacks
{
    public class MainServiceFargateStack : Stack
    {
        public ApplicationLoadBalancedFargateService FargateService { get; }
        public Cluster Cluster { get; }

        internal MainServiceFargateStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "MainServiceVpc", new VpcProps
            {
                MaxAzs = 3
            });

            Cluster = new Cluster(this, "MainServiceCluster", new ClusterProps
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
                Cluster = Cluster,
                DesiredCount = 1,
                TaskDefinition = taskDefinition,
                MemoryLimitMiB = 2048,
                PublicLoadBalancer = true,
                Certificate = Certificate.FromCertificateArn(this, "Certificate", "arn:aws:acm:us-west-2:217720823904:certificate/9333851f-05f5-4bdc-99f4-bd7f88f9ae32"),
                DomainName = "mainservice.yellowstoneapp.io",
                DomainZone = zone,
            };
            
            // create the load balanced Fargate service and make it public. 
            FargateService = new ApplicationLoadBalancedFargateService(this, "MainServiceFargateService", serviceProps);
            
        }
    }
}