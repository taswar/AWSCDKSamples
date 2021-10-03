![Logo](http://taswar.zeytinsoft.com/wp-content/uploads/2021/08/aws-cdk-csharp-930x351.png "CDK with Csharp DotNet")

* Original Post at (http://taswar.zeytinsoft.com/using-aws-cdk-parameter-store-with-c/)
* 
In this section we will talk about Parameter Store what it is and why we would want to use it? I will show you an example of using AWS CDK Parameter Store with C# and extending our current solution.

### What is Parameter Store

AWS Parameter Store is actually a capability of [AWS Systems Manager](https://aws.amazon.com/systems-manager/). The AWS Parameter store provides secure, hierarchical storage for configuration data management and secrets management. One can store data such as database connection strings, passwords, Amazon Machine Image (AMI) IDs, or even license codes as parameter values. There is also one additional advantage in parameter store where you can store values as plain text or encrypted data since it is also integrated with Secret Manager. For more info on Parameter Store you can view the AWS Documentation ([https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html))

### Why would I want to use it?

From our previous deployment using CDK we saw that the db secret was stored in secret manager but it was a random string that was created something along the line of **DBSecretB45K907**. When we have multiple environment let's say staging, testing and prod it will be hard to tell which one is which, since the dbsecrets will all have some random characters in their name, this is where Parameter comes and help. We can store the values in a path like format. e.g **/myvpcsampledev/dbsecretsname**. Now whenever we need to get the value of the dbsecret in our dev environment we can just refer to parameter store which will in turn use the Secret Manager. Make sense? There are also additional advantages of Parameter Store

*   Store configuration data and encrypted strings in hierarchies and track versions.
*   Use a secure, scalable, hosted secrets management service with no servers to manage.
*   Improve your security posture by separating your data from your code.
*   Control and audit access at granular levels.
*   Store parameters reliably because Parameter Store is hosted in multiple Availability Zones in an AWS Region.

Now I hope you are sold on the idea :) Let's see how we use it in our CDK code to use Parameter store, later on we will use the same store to get the values.

#### Side note

> **⚠ NOTE: 
> Tier: Parameter Store provides two parameter tiers – standard and advanced. While standard tier lets you store up to 10,000 parameters and 4 KB per parameter in value size, advanced tier lets you store up to 100,000 parameters, 8 KB per parameter in value size and allows you to add policies to parameters.

### Show me the code

Before showing the nitty gritty details of the code. Let's see a diagram of what we are building and what we have added.
![CDK ParameterStore](http://taswar.zeytinsoft.com/wp-content/uploads/2021/08/CDK-ParameterStore.png, "CDK ParameterStore") 
As you may wonder the parameter store and secret manager are not in your VPC. The reason is Parameter Store and Secret Manager are global services where they are hosted in multiple regions so think of it as a service you are consuming just like S3 etc rather than having everything inside your VPC. Now let's look at the code and see what we will add to our Stack.

``` csharp
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SSM;

namespace CdkRDSParameterSample
{
    public class CdkRDSParameterSampleStack : Stack
    {
        internal CdkRDSParameterSampleStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "MyVpcSample", new VpcProps
            {
                Cidr = "10.0.0.0/16",
                MaxAzs = 2,
                SubnetConfiguration = new ISubnetConfiguration\[\]
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

            \_ = new StringParameter(this, "DbSecretsParameter", new StringParameterProps
            {
                Description = "This is the dev env db secret name",
                ParameterName = "/myvpcsampledev/dbsecretsname",
                StringValue = db.Secret.SecretName
            });
        }
    }
}
```
> **⚠ NOTE: 
> **Note:** ParameterName always starts with a / in front of it. 

As you can see above we have just added a string parameter and we have used the  **db.Secret.SecretName** as a **StringValue** inside of Parameter Store. What is left is for us to build and deploy the solution just like our previous solution with  **dotnet and cdk**.

``` bash
$ dotnet build src
$ cdk deploy
```

Once deployed lets look at the console to see how it would look like. Navigate to your CloudFormation screen on AWS Console, click on resources tab and from there find the Parameter store created link. Click on the link and you should be redirected to parameter store and you should see something like below.
![ParameterStoreDbSecret](http://taswar.zeytinsoft.com/wp-content/uploads/2021/08/ParameterStoreDbSecret.png, " ParameterStoreDbSecret")

### Challenges

*   What happens when you change the parameter type to an encrypted (secure) string. Can you use SecretString?
*   What about SimpleValue, what are the differences?
*   What about StringListValue, what are the differences?
*   Can we use pattern matching for the ParameterName? (Hint: look at AllowedPattern)
*   What other attributes can we store in the parameter store for our db object we created

### Summary

In the above example we saw how to use Parameter store with our CDK code, in the next section we will cover IAM roles and how we will use them to attach roles to our EC2 machine and database, think of it as a glue to allow security and access permission. Source code ([https://github.com/taswar/AWSCDKSamples/tree/main/CdkRDSParameterSample](https://github.com/taswar/AWSCDKSamples/tree/main/CdkRDSParameterSample))
