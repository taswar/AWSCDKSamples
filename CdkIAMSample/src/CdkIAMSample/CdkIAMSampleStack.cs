using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SSM;

namespace CdkIAMSample
{
    public class CdkIAMSampleStack : Stack
    {
        internal CdkIAMSampleStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "MyVpcSample", new VpcProps
            {
                Cidr = "10.0.0.0/16",
                MaxAzs = 2,
                SubnetConfiguration = new ISubnetConfiguration[]
                {
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        SubnetType = SubnetType.PUBLIC,
                        Name = "MyPublicSubnet"
                    },
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        SubnetType = SubnetType.PRIVATE,
                        Name = "MyPrivateSubnet"
                    }
                }
            });

            // Create the database
            const int dbPort = 1433;
            
            var db = new DatabaseInstance(this, "DB", new DatabaseInstanceProps
            {
                Vpc = vpc,
                VpcSubnets = new SubnetSelection{ SubnetType = SubnetType.PRIVATE },
                Engine = DatabaseInstanceEngine.SqlServerEx(new SqlServerExInstanceEngineProps { Version = SqlServerEngineVersion.VER_14 }),
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE2, InstanceSize.MICRO),
                Port = dbPort,
                InstanceIdentifier = "MyDbInstance",
                BackupRetention = Duration.Seconds(0) //not a good idea in prod, for sample it's ok
            });

            _ = new StringParameter(this, "DbSecretsParameter", new StringParameterProps
            {
                Description = "This is the dev env db secret name",
                ParameterName = "/myvpcsampledev/dbsecretsname",
                StringValue = db.Secret.SecretName
            });


            //Create the IAM role
            var appRole = new Role(this, "InstanceRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"),
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole")
                }
            });

            //attach an inline policy
            appRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "ssm:GetParametersByPath" },
                Resources = new[]
                {
                    Arn.Format(new ArnComponents
                    {
                        Service = "ssm",
                        Resource = "parameter",
                        ResourceName = "myvpcsampledev/*"
                    }, this)
                }
            }));

            //grant the read to the app Role
            db.Secret.GrantRead(appRole);
        }
    }
}
