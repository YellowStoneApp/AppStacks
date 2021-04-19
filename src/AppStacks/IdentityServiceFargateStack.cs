using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Route53;

namespace AppStacks
{
    public class IdentityServiceFargateStack : Stack
    {
        internal IdentityServiceFargateStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "IdentityServiceVpc", new VpcProps
            {
                Cidr = "10.0.0.0/24", // careful about how you set this. If it's not a public cidr you'll get in trouble. 
                // TBH I have :noidea: what makes it public.
                MaxAzs = 3,
            });

            var cluster = new Cluster(this, "IdentityServiceCluster", new ClusterProps
            {
                Vpc = vpc,
            });
            
            var executionRole = new Role(this, "IdentityServiceExecutionRole-", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
            });

            var containerTaskRole = new Role(this, "IdentityServiceTaskRole-", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
            });
            
            containerTaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("CloudWatchFullAccess"));
            containerTaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("SecretsManagerReadWrite"));
            
            var taskDefinition = new FargateTaskDefinition(this, "IdentityServiceTaskDefinition", new FargateTaskDefinitionProps
            {
                TaskRole = containerTaskRole,
                ExecutionRole = executionRole,
            });
            
            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = "IdentityService",
            });
            
            var containerOptions = new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset("/Users/stopidRobot/Projects/YellowStone/IdentityService"),
                Logging = logging,
            };

            var portMapping = new PortMapping
            {
                ContainerPort = 80,
                HostPort = 80,
            };
            
            taskDefinition
                .AddContainer("IdentityService", containerOptions)
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
                DomainName = "identityservice.yellowstoneapp.io",
                DomainZone = zone,
            };
            
            var service = new ApplicationLoadBalancedFargateService(this, "IdentityServiceFargateService", serviceProps);
        }
    }
}