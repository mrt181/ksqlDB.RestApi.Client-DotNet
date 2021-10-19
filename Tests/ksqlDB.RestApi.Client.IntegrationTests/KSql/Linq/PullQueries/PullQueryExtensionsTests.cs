﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ksqlDB.Api.Client.IntegrationTests.KSql.RestApi;
using ksqlDB.Api.Client.IntegrationTests.Models.Sensors;
using ksqlDB.RestApi.Client.KSql.Linq.PullQueries;
using ksqlDB.RestApi.Client.KSql.Query.Context;
using ksqlDB.RestApi.Client.KSql.Query.Functions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ksqlDB.Api.Client.IntegrationTests.KSql.Linq.PullQueries
{
  [TestClass]
  public class PullQueryExtensionsTests
  {
    private SensorsPullQueryProvider pullQueryProvider;

    private KSqlDBContextOptions contextOptions;
    private KSqlDBContext context;

    [TestInitialize]
    public async Task TestInitialize()
    {
      contextOptions = new KSqlDBContextOptions(KSqlDbRestApiProvider.KsqlDbUrl);
      
      context = new KSqlDBContext(contextOptions);

      pullQueryProvider = new SensorsPullQueryProvider();

      await pullQueryProvider.ExecuteAsync();
    }

    [TestMethod]
    public async Task CreatePullQuery()
    {
      //Arrange
      string sensorId = "sensor-1";

      //Act
      var result = await context.CreatePullQuery<IoTSensorStats>(SensorsPullQueryProvider.MaterializedViewName)
        .Where(c => c.SensorId == sensorId)
        .FirstOrDefaultAsync();
      
      //Assert
      result.Should().NotBeNull();
      result.SensorId.Should().Be(sensorId);
      result.WindowStart.Should().NotBe(null);
      result.WindowEnd.Should().NotBe(null);
    }

    [TestMethod]
    public async Task GetManyAsync()
    {
      //Arrange
      string sensorId = "sensor-1";

      //Act
      var asyncEnumerable = context.CreatePullQuery<IoTSensorStats>(SensorsPullQueryProvider.MaterializedViewName)
        .Where(c => c.SensorId == sensorId)
        .GetManyAsync();

      var results = new List<IoTSensorStats>();

      await foreach(var item in asyncEnumerable.ConfigureAwait(false))
        results.Add(item);

      //Assert
      results.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task SelectSingleColumn()
    {
      //Arrange
      string sensorId = "sensor-1";

      //Act
      var result = await context.CreatePullQuery<IoTSensorStats>(SensorsPullQueryProvider.MaterializedViewName)
        .Where(c => c.SensorId == sensorId)
        .Select(c => c.SensorId)
        .FirstOrDefaultAsync();
      
      //Assert
      result.Should().NotBeNull();
      result.Should().Be(sensorId);
    }

    [TestMethod]
    public async Task SelectColumns()
    {
      //Arrange
      string sensorId = "sensor-1";

      //Act
      var result = await context.CreatePullQuery<IoTSensorStats>(SensorsPullQueryProvider.MaterializedViewName)
        .Where(c => c.SensorId == sensorId)
        .Select(c => new { c.SensorId, Start = c.WindowStart })
        .FirstOrDefaultAsync();
      
      //Assert
      result.Start.Should().NotBe(null);
      result.SensorId.Should().Be(sensorId);
    }

    [TestMethod]
    public async Task CreatePullQuery_WithBounds()
    {
      //Arrange
      string sensorId = "sensor-1";

      string windowStart = "2019-10-03T21:31:16";
      string windowEnd = "2225-10-03T21:31:16";

      //Act
      var result = await context.CreatePullQuery<IoTSensorStats>(SensorsPullQueryProvider.MaterializedViewName)
        .Where(c => c.SensorId == sensorId)
        .Where(c => Bounds.WindowStart > windowStart && Bounds.WindowEnd <= windowEnd)
        .FirstOrDefaultAsync();
      
      //Assert
      result.Should().NotBeNull();
      result.SensorId.Should().Be(sensorId);
      result.WindowStart.Should().NotBe(null);
      result.WindowEnd.Should().NotBe(null);
    }

    [TestMethod]
    public async Task CreatePullQuery_FromPlainStringQuery()
    {
      //Arrange
      string sensorId = "sensor-1";
      string ksql = $"SELECT * FROM {SensorsPullQueryProvider.MaterializedViewName} WHERE SensorId = '{sensorId}';";

      //Act
      var result = await context.ExecutePullQuery<IoTSensorStats>(ksql);
      
      //Assert
      result.Should().NotBeNull();
      result.SensorId.Should().Be(sensorId);
    }
  }
}