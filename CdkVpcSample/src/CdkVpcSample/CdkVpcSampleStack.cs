using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

namespace CdkVpcSample
{
    public class CdkVpcSampleStack : Stack
    {
        internal CdkVpcSampleStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
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
        }
    }
}
