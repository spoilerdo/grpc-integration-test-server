# GRPC Integration test server
This Nuget package contains an TestServerFixture that is implemented by a FunctionalTestBase class. The class uses the fixture and configures it by:
- creating a GRPC-channel
- configuring the service collection
- add logging
- disposing everything nice and clean during a teardown.

The ReadOnlyFunctionalTestBase also seeds the in memory database with an abstract class that can be called in your integration test.

An example code of the ReadOnlyFunctionalTestBase would be:
``` 
[TestFixture]
public class IntegrationTests : ReadOnlyFunctionalTestBase<Startup, DbContext> {

    protected override void SeedMemoryDb(DbContext context) {
        if (!context.DbSet.Any()) {
            context.SaveChanges();
        }
    }

    [Test]
    public async Task GetById_Valid() {
    }
}
```