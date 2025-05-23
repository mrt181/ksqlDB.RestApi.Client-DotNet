using FluentAssertions;
using ksqlDb.RestApi.Client.IntegrationTests.KSql.RestApi;
using ksqlDb.RestApi.Client.IntegrationTests.Models.Movies;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements.Properties;

namespace ksqlDb.RestApi.Client.IntegrationTests.KSql.Linq;

public class MoviesProvider(KSqlDbRestApiProvider restApiProvider)
{
  private readonly KSqlDbRestApiProvider restApiProvider = restApiProvider ?? throw new ArgumentNullException(nameof(restApiProvider));

  public static readonly string MoviesTableName = "movies_test";
  public static readonly string ActorsTableName = "lead_actor_test";

  public async Task<bool> CreateTablesAsync()
  {
    var createMoviesTable = $@"CREATE OR REPLACE TABLE {MoviesTableName} (
        title VARCHAR PRIMARY KEY,
        id INT,
        release_year INT
      ) WITH (
        KAFKA_TOPIC='{MoviesTableName}',
        PARTITIONS=1,
        VALUE_FORMAT = 'JSON'
      );";
      
    KSqlDbStatement ksqlDbStatement = new(createMoviesTable);

    var result = await restApiProvider.ExecuteStatementAsync(ksqlDbStatement);
    var isSuccess = result.IsSuccess();
      
    isSuccess.Should().BeTrue();

    var createActorsTable = $@"CREATE OR REPLACE TABLE {ActorsTableName} (
        title VARCHAR PRIMARY KEY,
        actor_name VARCHAR
      ) WITH (
        KAFKA_TOPIC='{ActorsTableName}',
        PARTITIONS=1,
        VALUE_FORMAT='JSON'
      );";

    ksqlDbStatement = new KSqlDbStatement(createActorsTable);

    result = await restApiProvider.ExecuteStatementAsync(ksqlDbStatement);
    isSuccess = result.IsSuccess();
      
    isSuccess.Should().BeTrue();

    return true;
  }

  public static readonly Movie Movie1 = new()
  {
    Id = 1,
    Release_Year = 1986,
    Title = "Aliens",
    RowTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
  };

  public static readonly Movie Movie2 = new()
  {
    Id = 2,
    Release_Year = 1988,
    Title = "Die Hard",
    RowTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
  };

  public static readonly Lead_Actor LeadActor1 = new()
  {
    Actor_Name = "Sigourney Weaver",
    Title = "Aliens",
    RowTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
  };

  public static readonly Lead_Actor LeadActor2 = new()
  {
    Actor_Name = "Al Pacino",
    Title = "The Godfather",
    RowTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
  };

  public async Task<bool> InsertMovieAsync(Movie movie)
  {
    var insertProperties = new InsertProperties()
    {
      EntityName = MoviesTableName,
      ShouldPluralizeEntityName = false
    };

    var result = (await restApiProvider.InsertIntoAsync(movie, insertProperties)).IsSuccess(); 

    result.Should().BeTrue();

    return result;
  }

  public async Task<bool> InsertLeadAsync(Lead_Actor actor)
  {
    var insertProperties = new InsertProperties()
    {
      EntityName = ActorsTableName,
      ShouldPluralizeEntityName = false
    };
      
    var result = (await restApiProvider.InsertIntoAsync(actor, insertProperties)).IsSuccess(); 

    result.Should().BeTrue();

    return result;
  }

  public async Task DropTablesAsync()
  {
    await restApiProvider.DropTableAndTopic(ActorsTableName);
    await restApiProvider.DropTableAndTopic(MoviesTableName);
  }
}
