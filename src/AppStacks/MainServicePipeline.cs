using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using StageProps = Amazon.CDK.AWS.CodePipeline.StageProps;

namespace AppStacks
{
    public class MainServicePipeline : Stack
    {
        internal MainServicePipeline(Construct scope, string id, ApplicationLoadBalancedFargateService service, Cluster cluster, IStackProps props = null) : base(scope, id, props)
        {
            
            // Code Build 
            var repo = new Repository(this, "MainServiceRepo");
            
            var githubSource = Source.GitHubEnterprise(new GitHubEnterpriseSourceProps()
            {
                HttpsCloneUrl = "https://github.com/YellowStoneApp/MainService.git",
                // Webhook = false,
                // WebhookFilters = new []
                // {
                //     // this will trigger deploy when push to deploy
                //     FilterGroup.InEventOf(EventAction.PUSH).AndBranchIs("deploy")
                // },
            });


            var project = new PipelineProject(this, "MainServiceDeploymentProject", new PipelineProjectProps
            {
                ProjectName = "MainServiceProject",
                Environment = new BuildEnvironment
                {
                    BuildImage = LinuxBuildImage.AMAZON_LINUX_2_2,
                    Privileged = true,
                },
                //BuildSpec = BuildSpec.FromSourceFilename("./MainServiceBuildSpec.yaml")
                BuildSpec = BuildSpec.FromObject(new Dictionary<string, object>
                {
                    ["version"] = "0.2",
                    ["phases"] = new Dictionary<string, object>
                    {
                        ["pre_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new []
                            {
                                "echo Logging in to Amazon ECR...",
                                "aws --version",
                                "$(aws ecr get-login --region us-west-2 --no-include-email)",
                                //"REPOSITORY_URI=217720823904.dkr.ecr.us-west-2.amazonaws.com/mainservicepipeline-mainservicerepod5e19e6c-k1sgs9p3qfxx",
                                $"REPOSITORY_URI={repo.RepositoryUri}",
                                "COMMIT_HASH=$(echo $CODEBUILD_RESOLVED_SOURCE_VERSION | cut -c 1-7)",
                                "IMAGE_TAG=${COMMIT_HASH:=latest}"
                            }
                        },
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new []
                            {
                                "echo Build started on `date`",
                                "echo Building the Docker image...",
                                "docker build -t $REPOSITORY_URI:latest .",
                                "docker tag $REPOSITORY_URI:latest $REPOSITORY_URI:$IMAGE_TAG",
                            }
                        },
                        ["post_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new []
                            {
                                "echo Pushing the Docker images...",
                                "docker push $REPOSITORY_URI:latest",
                                "docker push $REPOSITORY_URI:$IMAGE_TAG",
                                //"printf '[{\"name\":\"$REPOSITORY_URI:latest\",\"imageUri\":\"$REPOSITORY_URI:latest\"}]' > imagedefinitions.json",
                            }
                        }
                    },
                    ["artifacts"] = new Dictionary<string, object>
                    {
                        ["files"] = new []
                        {
                            "**/*"
                        },
                        ["name"] = "builds/$COMMIT_HASH/my-artifacts"
                    }
                })

            });
            
            // Pipeline Actions
            var sourceOutput = new Artifact_();
            var buildOutput = new Artifact_();

            var sourceAction = new GitHubSourceAction(new GitHubSourceActionProps()
            {
                ActionName = "GithubSource",
                Owner = "YellowStoneApp",
                Branch = "deploy",
                OauthToken = SecretValue.PlainText("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"),
                Repo = "MainService",
                Output = sourceOutput,
            });

            var buildAction = new CodeBuildAction(new CodeBuildActionProps
            {
                ActionName = "MainServiceBuild",
                Project = project,
                Input = sourceOutput,
                Outputs = new[] {buildOutput}
            });
            
            var deployAction = new EcsDeployAction(new EcsDeployActionProps
            {
                ActionName = "MainServiceDeploy",
                Service = service.Service,
                //Input = buildOutput,
                ImageFile = buildOutput.AtPath("imageDetail.json")
            });

            var pipeline = new Pipeline(this, "MainServicePipeline", new PipelineProps
            {
                Stages = new[]
                {
                    new StageProps
                    {
                        StageName = "Source",
                        Actions = new[] {sourceAction}
                    },
                    new StageProps
                    {
                        StageName = "Build",
                        Actions = new[] {buildAction}
                    },
                    new StageProps
                    {
                        StageName = "DeployToECS",
                        Actions = new[] {deployAction}
                    }
                }
            });

            repo.GrantPullPush(project.Role);
            project.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
                {
                    Actions = new []
                    {
                        "ecs:DescribeCluster",
                        "ecr:GetAuthorizationToken",
                        "ecr:BatchCheckLayerAvailability",
                        "ecr:BatchGetImage",
                        "ecr:GetDownloadUrlForLayer"
                    },
                    Resources = new []
                    {
                        $"{cluster.ClusterArn}"
                    }
                    
                }) 
            );
        }
            

    }
}