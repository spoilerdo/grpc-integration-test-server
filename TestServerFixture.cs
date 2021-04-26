using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace GRPCIntegrationTestServer {
    /// <summary>
    /// Taken from https://github.com/P7CoreOrg/gRPC-dotnetcore-play/tree/master/src , minor edits made.
    /// </summary>
    /// <typeparam name="TStartup">The startup to use</typeparam>
    /// <typeparam name="TDataContext">The data context to use</typeparam>
    public abstract class TestServerFixture<TStartup, TDataContext> :
           ITestServerFixture
           where TStartup : class
        where TDataContext : DbContext {
        private string _environmentUrl;
        public bool IsUsingInProcTestServer { get; set; }

        public HttpMessageHandler MessageHandler { get; }
        public TestServer TestServer { get; }

        protected abstract string RelativePathToHostProject { get; }

        public TestServerFixture() {
            var contentRootPath = GetContentRootPath();
            var builder = new WebHostBuilder();

            builder.UseContentRoot(contentRootPath)
                .UseEnvironment("Development")
                .ConfigureAppConfiguration(configureDelegate => {
                })
                .ConfigureTestServices(services => {
                    // All DI is done by the normal startup, loop through...
                    // Find the dbcontext normally used and replace it with a proper dbcontext.
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TDataContext>));
                    if (descriptor != null) {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<TDataContext>(options =>
                        options.UseInMemoryDatabase(new Guid().ToString()));
                })
                .ConfigureAppConfiguration(ConfigureAppConfiguration);
            UseSettings(builder);

            // Uses Start up class from your API Host project to configure the test server.
            builder.UseStartup<TStartup>();
            string environmentUrl = Environment.GetEnvironmentVariable("TestEnvironmentUrl");
            IsUsingInProcTestServer = false;
            if (string.IsNullOrWhiteSpace(environmentUrl)) {
                environmentUrl = "http://localhost/";

                TestServer = new TestServer(builder);

                MessageHandler = TestServer.CreateHandler();
                IsUsingInProcTestServer = true;

                // We need to suppress the execution context because there is no boundary between the client and server while using TestServer
                MessageHandler = new SuppressExecutionContextHandler(MessageHandler);
            } else {
                if (environmentUrl.Last() != '/') {
                    environmentUrl = $"{environmentUrl}/";
                }
                MessageHandler = new HttpClientHandler();
            }

            _environmentUrl = environmentUrl;
        }

        protected abstract void ConfigureAppConfiguration(
            WebHostBuilderContext hostingContext,
            IConfigurationBuilder config);

        protected virtual void ConfigureServices(IServiceCollection services) {
        }

        protected virtual void UseSettings(WebHostBuilder builder) {
        }

        protected virtual void ConfigureAppConfiguration(IConfigurationBuilder configureDelegate) {
        }

        public HttpClient Client =>
            new HttpClient(new SessionMessageHandler(MessageHandler)) {
                BaseAddress = new Uri(_environmentUrl)
            };

        private string GetContentRootPath() {
            var testProjectPath = PlatformServices.Default.Application.ApplicationBasePath;
            return Path.Combine(testProjectPath, RelativePathToHostProject);
        }
    }
}