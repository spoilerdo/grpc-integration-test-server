#nullable enable
using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GRPCIntegrationTestServer {
    public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception exception);

    public class TestServerFixture<TStartup, TDataContext> : IDisposable
        where TStartup : class
        where TDataContext : DbContext {
        private readonly TestServer _server;
        private readonly IHost _host;

        public TDataContext? Context;

        public event LogMessage? LoggedMessage;

        public TestServerFixture() : this(null) { }

        public LoggerFactory LoggerFactory { get; }
        public HttpMessageHandler Handler { get; }

        public TestServerFixture(Action<IServiceCollection>? initialConfigureServices) {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new ForwardingLoggerProvider((logLevel, category, eventId, message, exception) => {
                LoggedMessage?.Invoke(logLevel, category, eventId, message, exception);
            }));

            var builder = new HostBuilder()
                .ConfigureWebHostDefaults(webHost => {
                    webHost.UseTestServer()
                        .UseStartup<TStartup>();
                })
                .ConfigureServices(services => {
                    services.AddSingleton<ILoggerFactory>(LoggerFactory);

                    var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<TDataContext>));
                    if (descriptors.Count() > 0) {
                        foreach (var descriptor in descriptors.ToArray()) {
                            services.Remove(descriptor);
                        }
                    }

                    services.AddDbContext<TDataContext>(options =>
                        options.UseInMemoryDatabase(new Guid().ToString()));

                    initialConfigureServices?.Invoke(services);
                });
            _host = builder.Start();
            _server = _host.GetTestServer();

            Handler = _server.CreateHandler();
        }

        public void Dispose() {
            Handler.Dispose();
            _host.Dispose();
            _server.Dispose();
        }

        public IDisposable GetTestContext() {
            return new TestServerContext<TStartup, TDataContext>(this);
        }
    }
}
#nullable disable