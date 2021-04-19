using Amazon.CDK;

namespace AppStacks
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new AppStacksStack(app, "AppStacksStack");

            app.Synth();
        }
    }
}
