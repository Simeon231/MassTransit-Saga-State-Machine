using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
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
                Vpc = vpc,
            });

            CreateApp(this, cluster, "Inventory", "./MasterDegreeDemo.EventReceiver1/Dockerfile", 80);

            CreateApp(this, cluster, "Payment", "./MasterDegreeDemo.EventReceiver2/Dockerfile", 80);

            CreateApp(this, cluster, "Orders", "./MasterDegreeDemo.EventSender/Dockerfile", 80);
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

            var logGroup = new LogGroup(scope, $"{appName}LogGroup", new LogGroupProps
            {
                RemovalPolicy = RemovalPolicy.DESTROY,
                Retention = RetentionDays.ONE_DAY,
                LogGroupName = $"{appName}_LogGroup",
            });

            taskDefinition.AddContainer($"{appName}Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset("./", new AssetImageProps
                {
                    File = dockerPath,
                }),
                PortMappings =
                [
                    new PortMapping
                    {
                        ContainerPort = containerPort,
                    },
                ],
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps()
                {
                    StreamPrefix = appName,
                    LogGroup = logGroup,
                }),
                Environment = new Dictionary<string, string>
                {
                    ["ASPNETCORE_HTTP_PORTS"] = containerPort.ToString(),
                },
            });

            AddAlarm(logGroup, appName);

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

        private void AddAlarm(LogGroup logGroup, string appName)
        {
            var metricFilter = new MetricFilter(this, $"{appName}CriticalLogFilter", new MetricFilterProps
            {
                LogGroup = logGroup,
                FilterPattern = FilterPattern.AllTerms("crit"),
                MetricName = $"{appName}_CriticalLogsMetric",
                MetricNamespace = "BlazorMonitoring",
                DefaultValue = 0,
            });

            var alarm = new Alarm(this, $"{appName}CriticalLogAlarm", new AlarmProps
            {
                Metric = metricFilter.Metric(new MetricOptions
                {
                    Statistic = "Sum",
                    Period = Duration.Seconds(30),
                }),
                EvaluationPeriods = 1,
                Threshold = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                AlarmDescription = $"{appName} app logged a Critical error",
            });

            var alertTopic = new Topic(this, $"{appName}CriticalLogAlertsTopic", new TopicProps
            {
                DisplayName = $"{appName}_CriticalLogAlerts"
            });

            alertTopic.AddSubscription(new EmailSubscription("firuss213@gmail.com"));

            alarm.AddAlarmAction(new SnsAction(alertTopic));
        }
    }
}
