using Amazon.CDK;
using Amazon.CDK.AWS.ApplicationAutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Constructs;

namespace MasterDegreeDemo.CDK
{
    public class DemoStack : Stack
    {
        public DemoStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
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

            //CreateBlazorApp(this, cluster, "EventReceiver2", "./BlazorApp2", 80);

            //CreateBlazorApp(this, cluster, "BlazorApp3", "./BlazorApp3", 80);
        }

        private void CreateApp(Construct scope, Cluster cluster, string appName, string dockerPath, int containerPort)
        {
            var taskDefinition = new FargateTaskDefinition(scope, $"{appName}Task", new FargateTaskDefinitionProps
            {
                Cpu = 256,
                MemoryLimitMiB = 512,
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
                }
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

        private void CreateLoadBalancedApp(Construct scope, Cluster cluster, string appName, string dockerPath, int containerPort)
        {
            var service = new ApplicationLoadBalancedFargateService(scope, $"{appName}Service", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = ContainerImage.FromAsset(dockerPath), // This will build from Dockerfile
                    ContainerPort = containerPort,
                },
                DesiredCount = 1,
                PublicLoadBalancer = true,
                Cpu = 256,
                MemoryLimitMiB = 512,
            });

            // Attach AutoScaling
            var scalableTarget = service.Service.AutoScaleTaskCount(new EnableScalingProps
            {
                MinCapacity = 1,
                MaxCapacity = 1, // prevent auto-scaling
            });
        }
    }
}
