using FluentAssertions;
using ksqlDb.RestApi.Client.IntegrationTests.KSql.RestApi;
using ksqlDb.RestApi.Client.IntegrationTests.Models;
using ksqlDB.RestApi.Client.KSql.Linq;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements.Properties;
using NUnit.Framework;

namespace ksqlDb.RestApi.Client.IntegrationTests.KSql.Linq;

public class AggregationTimeTests : Infrastructure.IntegrationTests
{
  public static readonly Times Times1 = new()
  {
    Id = 1,
    Created = DateTime.Now,
  };

  public static readonly Times Times2 = new()
  {
    Id = 2,
    Created = new DateTime(1986, 1, 1),
    CreatedWithOffset = new DateTimeOffset(new DateTime(1986, 1, 1), TimeSpan.FromHours(1)),
    CreatedTime = new TimeSpan(11, 11, 42),
  };

  [OneTimeSetUp]
  public static async Task ClassInitialize()
  {
    RestApiProvider = KSqlDbRestApiProvider.Create();

    var timesStreamMetadata = new EntityCreationMetadata(TimesStreamName, 1)
    {
      EntityName = TimesStreamName,
      ShouldPluralizeEntityName = false,
      Replicas = 1,
    };

    await RestApiProvider.CreateOrReplaceStreamAsync<Times>(timesStreamMetadata);

    var insertProperties = new InsertProperties()
    {
      EntityName = TimesStreamName,
      ShouldPluralizeEntityName = false
    };

    await RestApiProvider.InsertIntoAsync(Times1, insertProperties);

    await RestApiProvider.InsertIntoAsync(Times2, insertProperties);
  }

  [OneTimeTearDown]
  public static async Task ClassCleanup()
  {

    await RestApiProvider.DropStreamAsync(TimesStreamName, useIfExistsClause: false, deleteTopic: true);
  }

  private static readonly string TimesStreamName = "times_test";

  [Test]
  public async Task MinDateTime()
  {
    await MinDateTime(Context.CreatePushQuery<Times>(TimesStreamName));
  }

  private static async Task MinDateTime(IQbservable<Times> querySource)
  {
    //Arrange
    int expectedItemsCount = 2;

    var source = querySource
      .GroupBy(c => c.Id)
      .Select(l => new { Id = l.Key, MinDateTime = l.Min(c => c.Created) })
      .ToAsyncEnumerable();

    //Act
    var actualValues = await CollectActualValues(source, expectedItemsCount);

    //Assert
    var result = actualValues.First(c => c.Id == Times2.Id);
    result.MinDateTime.Year.Should().Be(1986);
  }

  [Test]
  public async Task MaxTime()
  {
    await MaxTime(Context.CreatePushQuery<Times>(TimesStreamName));
  }

  private static async Task MaxTime(IQbservable<Times> querySource)
  {
    //Arrange
    int expectedItemsCount = 2;

    var source = querySource
      .GroupBy(c => c.Id)
      .Select(l => new { Id = l.Key, MaxTime = l.Max(c => c.CreatedTime) })
      .ToAsyncEnumerable();

    //Act
    var actualValues = await CollectActualValues(source, expectedItemsCount);

    //Assert
    var result = actualValues.First(c => c.Id == Times2.Id);
    result.MaxTime.Hours.Should().Be(11);
  }

  [Test]
  public async Task MinDateTimeOffset()
  {
    await MinDateTimeOffset(Context.CreatePushQuery<Times>(TimesStreamName));
  }

  private static async Task MinDateTimeOffset(IQbservable<Times> querySource)
  {
    //Arrange
    int expectedItemsCount = 2;

    var source = querySource
      .GroupBy(c => c.Id)
      .Select(l => new { Id = l.Key, MinDateTimeOffset = l.Min(c => c.CreatedWithOffset) })
      .ToAsyncEnumerable();

    //Act
    var actualValues = await CollectActualValues(source, expectedItemsCount);

    //Assert
    var result = actualValues.First(c => c.Id == Times2.Id);
    result.MinDateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(1));
  }
}
