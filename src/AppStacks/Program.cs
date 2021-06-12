using Amazon.CDK;

namespace AppStacks
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            var env = makeEnv("217720823904", "us-west-2");
            
            var mainService = new MainServiceFargateStack(app, "MainServiceFargateStack", new StackProps
            {
                Env = env
            });

            new MainServicePipeline(app, "MainServicePipeline", mainService.FargateService, mainService.Cluster, new StackProps
            {
                Env = env
            });
            

            app.Synth();
        }
        private static Environment makeEnv(string account, string region)
        {
            return new Environment
            {
                Account = account,
                Region = region
            };
        }
    }
}
