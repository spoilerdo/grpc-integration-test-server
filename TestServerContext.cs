using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GRPCIntegrationTestServer {
    internal class TestServerContext<TStartup, TDataContext> : IDisposable
    where TStartup : class
    where TDataContext : DbContext {
        private readonly ExecutionContext _executionContext;
        private readonly Stopwatch _stopWatch;
        private readonly TestServerFixture<TStartup, TDataContext> _fixture;

        public TestServerContext(TestServerFixture<TStartup, TDataContext> fixture) {
            _executionContext = ExecutionContext.Capture()!;
            _stopWatch = Stopwatch.StartNew();
            _fixture = fixture;
            _fixture.LoggedMessage += WriteMessage;
        }

        private void WriteMessage(LogLevel logLevel, string category, EventId eventId, string message, Exception exception) {
            // Log using the passed in execution context.
            // In the case of NUnit, console output is only captured by the test
            // if it is written in the test's execution context.
            ExecutionContext.Run(_executionContext, s => {
                Console.WriteLine($"{_stopWatch.Elapsed.TotalSeconds:N3}s {category} - {logLevel}: {message}");
            }, null);
        }

        public void Dispose() {
            _fixture.LoggedMessage -= WriteMessage;
            _executionContext?.Dispose();
        }
    }
}