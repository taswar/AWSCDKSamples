Now that we have learned how to create a VPC with C# using AWS CDK, let's talk about how do we create RDS Database Instance with C# using AWS CDK.

### What is Amazon RDS?

Amazon Relational Database Service (Amazon RDS) in simple terms is easy to set up, operate, and scale relational database. It provides cost-efficient and resizable capacity while automating time-consuming administration tasks such as hardware provisioning, database setup, patching and backups. When we use Amazon RDS, we can set up, operate, and scale a relational database in the cloud. Amazon RDS also supports several different database engines, including PostgreSQL, MySQL, Oracle, and Microsoft SQL Server.

### What do we plan to build?

We will be using Microsoft SQL Server instance in the example below, you can use any other RDS instance of your choice also, but I thought SQL Server would be ideal for .NET developer. Supported Databases include MariaDB, Microsoft SQLServer, Postgres, MySQL and Oracle. For more information refer to ([https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.DBInstanceClass.html#Concepts.DBInstanceClass.Support](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Concepts.DBInstanceClass.html#Concepts.DBInstanceClass.Support)) The architectural diagram below shows that we will place our primary database instance in Availability Zone A inside the private subnet. Amazon RDS requires at least two Availability Zones for fault tolerance, but since we are using Microsoft SQL Server Express engine it doesn't create a secondary instance. 

![VPC-subnet-RDS](http://taswar.zeytinsoft.com/wp-content/uploads/2021/04/VPC-subnet-RDS.png, "VPC-subnet-RDS")


> **Tip:** When deploying an application in production environment always create a secondary 
> Amazon RDS DB instance in another Availability Zone. 


### Where is the code

We will first need to use Nuget to install Amazon.CDK.AWS.RDS package. We can either use Visual Studio or the command line to install it into our project. Make sure to have the latest version of it.
``` powershell
PM> Install-Package Amazon.CDK.AWS.RDS
```
Once installed you can add this code into your Stack class file. In our code based it will be **CdkRDSSampleStack.cs**.

``` csharp
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;

namespace CdkRDSSample
{
    public class CdkRDSSampleStack : Stack
    {
        internal CdkRDSSampleStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
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
                BackupRetention = Duration.Seconds(0) //not a good idea in prod, for this sample code it's ok
            });
        }
    }
}
```

In order for us to deploy this code we will again use the command line with **cdk** to deploy it. You may wonder where will the login and password be for the database, we will talk about that shortly. Remember to have your aws login pre-configured or else cdk deploy will fail, refer to the previous [Creating VPC](http://taswar.zeytinsoft.com/using-aws-cdk-to-create-a-vpc-in-c/) for additional information.

``` bash
$ dotnet build src
$ cdk deploy
```

### Viewing your information in the AWS Console

Once the **cdk** process is finished we can login to our AWS console to see what has been deployed so far. Go into the cloudformation section and we should see that our stack should have a CREATE\_COMPLETE status like below. 
![RDS-stack-created](http://taswar.zeytinsoft.com/wp-content/uploads/2021/04/RDS-stack-created.png, "RDS-stack-created") 

We can then go into the Resource section of the stack and see that our **DBSecret** for the database has been created and it is actually using AWS SecretManager that contains the details of the login and password. We can also go to our database section in RDS and see our instance is up and running. We will not be able to connect to it from our desktop since it is in a private network and public connection accessibility is set to **No**. 

![RDS-SQLServer-MyInstance](http://taswar.zeytinsoft.com/wp-content/uploads/2021/04/RDS-SQLServer-MyInstance.png, "RDS-SQLServer-MyInstance")

### Challenges

*   What if you try to use a different database engine, e.g MariaDB or Postgres what would you change in your code?
*   What happens when you change the current SQLServer to another version?
*   The current machine is not powerful for sqlserver. what other instance type are supported to run this?


### Summary

This shows how we can easily use AWS CDK to create RDS Database Instance with C#, in the next part we will go over System Manager Parameter Store that one can use to store configurations. Source code: ([https://github.com/taswar/AWSCDKSamples/tree/main/CdkRDSSample](https://github.com/taswar/AWSCDKSamples/tree/main/CdkRDSSample))
