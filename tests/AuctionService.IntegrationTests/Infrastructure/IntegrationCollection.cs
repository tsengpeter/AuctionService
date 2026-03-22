using AuctionService.IntegrationTests.Infrastructure;

namespace AuctionService.IntegrationTests;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
}
