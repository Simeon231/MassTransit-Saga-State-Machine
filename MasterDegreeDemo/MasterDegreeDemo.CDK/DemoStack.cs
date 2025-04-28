using Amazon.CDK;
using Amazon.CDK.AWS.ApplicationAutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace MasterDegreeDemo.CDK
{
    public class DemoStack : Stack
    {
        public DemoStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "BlazorVpc", new VpcProps
            {
                MaxAzs = 1,
                SubnetConfiguration =
                [
                    new SubnetConfiguration
                    {
                        Name = "PublicSubnet",
                        SubnetType = SubnetType.PUBLIC,
                        CidrMask = 24
                    }
                ],
                VpcName = "Master",
            });

            var cluster = new Cluster(this, "MasterDegreeDemoCluster", new ClusterProps
            {
                Vpc = vpc
            });

            CreateApp(this, cluster, "EventReceiver1", "./MasterDegreeDemo.EventReceiver1/Dockerfile", 80);

            CreateApp(this, cluster, "EventReceiver2", "./MasterDegreeDemo.EventReceiver2/Dockerfile", 80);

            CreateApp(this, cluster, "EventSender", "./MasterDegreeDemo.EventSender/Dockerfile", 80);
        }

        private void CreateApp(Construct scope, Cluster cluster, string appName, string dockerPath, int containerPort)
        {

            var taskRole = new Role(this, $"{appName}FargateTaskRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                ManagedPolicies =
                [
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSQSFullAccess"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSNSFullAccess")
                ]
            });

            var taskDefinition = new FargateTaskDefinition(scope, $"{appName}Task", new FargateTaskDefinitionProps
            {
                Cpu = 256,
                MemoryLimitMiB = 512,
                TaskRole = taskRole,
            });

            taskDefinition.AddContainer($"{appName}Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset("./", new AssetImageProps
                {
                    File = dockerPath,
                }),
                PortMappings = new[]
                {
                    new PortMapping
                    {
                        ContainerPort = containerPort
                    }
                },
                HealthCheck = new HealthCheck()
                {
                    Command = ["CMD-SHELL", "curl -f http://localhost:80/health || exit 1"],
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    Retries = 3,
                    StartPeriod = Duration.Seconds(10)
                },
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps()
                {
                    LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_DAY,
                    StreamPrefix = appName,
                }),
            });

            var service = new FargateService(scope, $"{appName}Service", new FargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                AssignPublicIp = true,
                DesiredCount = 1,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC
                },
                CircuitBreaker = new DeploymentCircuitBreaker
                { 
                    Rollback = true
                },
                MinHealthyPercent = 100,
            });

            // Allow inbound traffic to the container port
            service.Connections.AllowFromAnyIpv4(Port.Tcp(containerPort));
        }
    }
}
