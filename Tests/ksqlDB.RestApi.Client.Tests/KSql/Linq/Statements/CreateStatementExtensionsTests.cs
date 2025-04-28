using FluentAssertions;
using ksqlDB.RestApi.Client.KSql.Linq;
using ksqlDB.RestApi.Client.KSql.Linq.Statements;
using ksqlDB.RestApi.Client.KSql.Query.Windows;
using ksqlDB.RestApi.Client.KSql.RestApi.Serialization;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements;
using ksqlDb.RestApi.Client.Tests.Models;
using ksqlDb.RestApi.Client.Tests.Models.Movies;
using NUnit.Framework;
using UnitTests;
using TestParameters = ksqlDb.RestApi.Client.Tests.Helpers.TestParameters;

namespace ksqlDb.RestApi.Client.Tests.KSql.Linq.Statements;

#pragma warning disable IDE0037

public class CreateStatementExtensionsTests : TestBase
{
  private TestableDbProvider DbProvider { get; set; } = null!;

  protected virtual string StatementResponse { get; set; } = @"[{""@type"":""currentStatus"", ""commandSequenceNumber"":2174,""warnings"":[]}]";

  [SetUp]
  public override void TestInitialize()
  {
    base.TestInitialize();

    DbProvider = new TestableDbProvider(TestParameters.KsqlDbUrl, StatementResponse);
  }

  [TearDown]
  public override void TestCleanup()
  {
    DbProvider.Dispose();
    base.TestCleanup();
  }

  private const string StreamName = "TestStream";

  [Test]
  public void CreateOrReplaceStreamStatement_ToStatementString_CalledTwiceWithSameResult()
  {
    //Arrange
    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Location>();

    //Act
    var ksql1 = query.ToStatementString().ReplaceLineEndings();
    var ksql2 = query.ToStatementString().ReplaceLineEndings();

    //Assert
    ksql1.Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT * FROM {nameof(Location)}s EMIT CHANGES;".ReplaceLineEndings());

    ksql1.Should().BeEquivalentTo(ksql2);
  }

  [Test]
  public void CreateOrReplaceStreamStatement_ToStatementString_ComplexQueryWasGenerated()
  {
    //Arrange
    var creationMetadata = new CreationMetadata
    {
      KafkaTopic = "moviesByTitle",
      KeyFormat = SerializationFormats.Json,
      ValueFormat = SerializationFormats.Json,
      Replicas = 1,
      Partitions = 1
    };

    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .With(creationMetadata)
      .As<Movie>()
      .Where(c => c.Id < 3)
      .Select(c => new {c.Title, ReleaseYear = c.Release_Year})
      .PartitionBy(c => c.Title);

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName}
 WITH ( KAFKA_TOPIC='moviesByTitle', KEY_FORMAT='Json', VALUE_FORMAT='Json', PARTITIONS='1', REPLICAS='1' ) AS 
SELECT Title, Release_Year AS ReleaseYear FROM Movies
 WHERE Id < 3 PARTITION BY Title EMIT CHANGES;".ReplaceLineEndings());
  }

  private const string TableName = "TestTable";

  [Test]
  public async Task CreateOrReplaceTableStatement_ExecuteStatementAsync_ResponseWasReceived()
  {
    //Arrange
    DbProvider.RegisterKSqlDbRestApiClient = false;
    var query = DbProvider.CreateOrReplaceTableStatement(TableName)
      .As<Location>();

    //Act
    var httpResponseMessage = await query.ExecuteStatementAsync();

    //Assert
    string responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
    responseContent.Should().BeEquivalentTo(StatementResponse);
  }

  [Test]
  public void GroupByHaving()
  {
    //Arrange
    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Movie>()
      .GroupBy(c => c.Title)
      .Having(c => c.Count() > 2);

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT * FROM Movies GROUP BY Title HAVING Count(*) > 2 EMIT CHANGES;".ReplaceLineEndings());
  }

  [Test]
  public void Take()
  {
    //Arrange
    int limit = 3;

    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Movie>()
      .Take(limit);

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT * FROM Movies EMIT CHANGES LIMIT {limit};".ReplaceLineEndings());
  }

  [Test]
  public void WindowedBy()
  {
    //Arrange
    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Movie>()
      .GroupBy(c => new { c.Title })
      .WindowedBy(new TimeWindows(Duration.OfMinutes(2)));

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT * FROM Movies WINDOW TUMBLING (SIZE 2 MINUTES) GROUP BY Title EMIT CHANGES;".ReplaceLineEndings());
  }

  [Test]
  public void Join()
  {
    //Arrange
    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Movie>()
      .Join(
        Source.Of<Lead_Actor>("Actors"),
        movie => movie.Title,
        actor => actor.Title,
        (movie, actor) => new
        {
          Title = movie.Title, ActorName = actor.Actor_Name
        }
      );

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT movie.Title Title, actor.Actor_Name AS ActorName FROM Movies movie
INNER JOIN Actors actor
ON movie.Title = actor.Title
EMIT CHANGES;".ReplaceLineEndings());
  }

  [Test]
  public void FullOuterJoin()
  {
    //Arrange
    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Movie>()
      .FullOuterJoin(
        Source.Of<Lead_Actor>("Actors"),
        movie => movie.Title,
        actor => actor.Title,
        (movie, actor) => new
        {
          Title = movie.Title, ActorName = actor.Actor_Name
        }
      );

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT movie.Title Title, actor.Actor_Name AS ActorName FROM Movies movie
FULL OUTER JOIN Actors actor
ON movie.Title = actor.Title
EMIT CHANGES;".ReplaceLineEndings());
  }

  [Test]
  public void LeftJoin()
  {
    //Arrange
    var query = DbProvider.CreateOrReplaceStreamStatement(StreamName)
      .As<Movie>()
      .LeftJoin(
        Source.Of<Lead_Actor>("Actors"),
        movie => movie.Title,
        actor => actor.Title,
        (movie, actor) => new
        {
          Title = movie.Title, ActorName = actor.Actor_Name
        }
      );

    //Act
    var ksql = query.ToStatementString();

    //Assert
    ksql.ReplaceLineEndings().Should().BeEquivalentTo(@$"CREATE OR REPLACE STREAM {StreamName} AS 
SELECT movie.Title Title, actor.Actor_Name AS ActorName FROM Movies movie
LEFT JOIN Actors actor
ON movie.Title = actor.Title
EMIT CHANGES;".ReplaceLineEndings());
  }
}

#pragma warning restore IDE0037
