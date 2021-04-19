using Amazon.CDK;

namespace AppStacks
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            var env = makeEnv("217720823904", "us-west-2");
            
            new IdentityServiceFargateStack(app, "IdentityServiceFargateStack", new StackProps
            {
                Env = env
            });
            
            new MainServiceFargateStack(app, "MainServiceFargateStack", new StackProps
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
