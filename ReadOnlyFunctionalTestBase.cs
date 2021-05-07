using System;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

#nullable enable
namespace GRPCIntegrationTestServer {
    public abstract class ReadOnlyFunctionalTestBase<TStartup, TDataContext>
        where TStartup : class
        where TDataContext : DbContext {
        private GrpcChannel? _channel;
        private IDisposable? _testContext;

        protected TestServerFixture<TStartup, TDataContext> Fixture { get; private set; } = default!;

        protected ILoggerFactory LoggerFactory => (ILoggerFactory)Fixture.LoggerFactory;
        protected GrpcChannel Channel => _channel ??= CreateChannel();
        protected GrpcChannel CreateChannel() {
            return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions {
                LoggerFactory = LoggerFactory,
                HttpHandler = Fixture.Handler
            });
        }

        protected abstract void SeedMemoryDb(TDataContext context);

        protected virtual void ConfigureServices(IServiceCollection services) {
            var context = services.BuildServiceProvider().GetService<TDataContext>();
            if (context != null)
                this.SeedMemoryDb(context);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            Fixture = new TestServerFixture<TStartup, TDataContext>(ConfigureServices);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            Fixture.Dispose();
        }

        [SetUp]
        public void SetUp() {
            _testContext = Fixture.GetTestContext();
        }

        [TearDown]
        public void TearDown() {
            _testContext?.Dispose();
            _channel = null;
        }
    }
}
#nullable disable