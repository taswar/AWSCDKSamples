![Logo](http://taswar.zeytinsoft.com/wp-content/uploads/2021/08/aws-cdk-csharp-930x351.png "CDK with Csharp DotNet")

* Original Post at (http://taswar.zeytinsoft.com/using-aws-cdk-to-create-iam-roles-with-c/)

In this section we will talk about Identity and Access Management - IAM in short. I will show you an example of using AWS CDK to create IAM roles with C# and extending our current solution so that an IAM Role is ready with the correct permission to use. We will mainly focus on IAM Role and Policy. But first....

### What is IAM?

AWS Identity and Access Management (IAM) enables you to manage access to AWS services and resources securely. Using IAM, you can create and manage AWS users and groups, and use permissions to allow and deny their access to AWS resources. IAM can help securely control individual and group access to your AWS resources. You can create and manage user identities, or IAM users, roles, policies and grant permissions for them to access the resources you wish to give permission to.

### Additional things IAM Allows


*   **Manage IAM users and their access** – You can create users in IAM, assign them individual security credentials (in other words, access keys, passwords, and multi-factor authentication devices), or request temporary security credentials to provide users access to AWS services and resources. You can manage permissions in order to control which operations a user can perform.
*   **Manage IAM roles and their permissions** – You can create roles in IAM and manage permissions to control which operations can be performed by the entity, or AWS service, that assumes the role. You can also define which entity is allowed to assume the role. In addition, you can use service-linked roles to delegate permissions to AWS services that create and manage AWS resources on your behalf.
*   **Manage federated users and their permissions** – You can enable identity federation to allow existing identities (users, groups, and roles) in your enterprise to access the AWS Management Console, call AWS APIs, and access resources, without the need to create an IAM user for each identity. Use any identity management solution that supports SAML 2.0, or use one of our federation samples (AWS Console SSO or API federation).
(Additional information at: [https://aws.amazon.com/iam/](https://aws.amazon.com/iam/))

### IAM Roles

In our case we wish to grant our applications that will run on an Amazon EC2 instances access to AWS resources. This is where IAM roles comes into play and allow you to delegate access to users or services, IAM Roles are intended to be assumable by anyone who needs it including IAM users, AWS services including machines and applications in our case. One can assume a role to obtain temporary security credentials that can be used to make AWS API calls. Note that IAM roles are not associated with a specific user or group, instead a trusted entities assume the roles, such as IAM users, applications, or AWS services. The best part of IAM Role is that you don't have to share long-term credentials or define permissions for each entity that requires access to a resource. Isn't that cool?

### IAM Policy

Now that we understand an IAM Role, but how do we write rules etc for that role? This is where IAM Policy comes in, a policy is an object with identity or resource, defining their permissions one can also associate with 1-N Roles of a policy. Best practices is to use multiple policy since they are free. AWS IAM evaluates the policies when an IAM principal (a user, role, or group) makes a request. Permissions in the policies attached to the entity determine whether the request is allowed or denied. Policies are writting in json format, and one tip is when you are reading a policy always use the **EPRAC** format to evaluate it. 

*   **E** is for Effect of the policy
*   **P** is for Principle of the policy (a user, role, or group)
*   **R** is for Resources of the policy (a service etc)
*   **A** is for Action of the policy (Allow or deny)
*   **C** is for Conditions of the policy, matching some condition ip address or require MFA etc

(For more info read: [https://docs.aws.amazon.com/IAM/latest/UserGuide/access\_policies.html](https://docs.aws.amazon.com/IAM/latest/UserGuide/access_policies.html))

### Two types of policies

There are two types of policies one is managed policy and also an inline policy.

*   **Managed Policy** - has a name and can be attach to multiple users, group or roles. Think shareable policy, AWS has predefined policy in the system already that one can use.
*   **Inline Policy** - a policy that is embedded in an IAM identity (a user, role, or group) only, you cannot share it.


### Show me some code now please

Sorry to bore you with all the details but those roles and policy are important and when things don't work they could be culprits of them. But in any case we will be adding IAM to our solution now, check out the diagram below. You will notice that IAM is outside our VPC, since IAM is a global service it will be out side and we will attach the role inside to the EC2 machine in our later post. 
![AWSCDKIAMRole](http://taswar.zeytinsoft.com/wp-content/uploads/2021/10/CDKIAMRole2.png, "AWS CDK IAM Role") 
Now our stack we will look like this now

``` csharp
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
                Engine = DatabaseInstanceEngine.SqlServerEx(new SqlServerExInstanceEngineProps { Version = SqlServerEngineVersion.VER\_14 }),
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
```
### Build and Deploy

We will again use our good old trusty cdk to deploy the solution.


``` bash
$dotnet build src
$cdk deploy
```

If we check our console we can go to IAM and look at Roles and search for Instance in the search box. You will see something like this depending on your stack name etc. 

![CdkInstanceRole](http://taswar.zeytinsoft.com/wp-content/uploads/2021/10/CdkInstanceRole.png, "Cdk Instance Role Generated")

If we go into the role you look at the permission we will see an inline policy also attached to the role. 
![CdkInstanceRolePolicy](http://taswar.zeytinsoft.com/wp-content/uploads/2021/10/CdkInstanceRolePolicy.png, "Cdk Instance Role Policy Genearted")

### Challenges

*   How do we add/create a managed policy using the CDK?
*   How do we add a principle to our policy using the CDK?
*   How do we add a condition to our policy using the CDK?

### Summary

I know its a bit boring and dry for IAM but trust me its an important building block of AWS, we went through knowing more about Roles and Policy and how to use AWS CDK to create IAM roles with C#. In our next section I will cover how to create auto scaling groups for our EC2 machines.

