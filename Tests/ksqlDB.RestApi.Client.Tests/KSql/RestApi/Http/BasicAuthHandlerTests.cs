using FluentAssertions;
using ksqlDB.RestApi.Client.KSql.Query.Context;
using ksqlDB.RestApi.Client.KSql.RestApi.Http;
using NUnit.Framework;
using UnitTests;

namespace ksqlDb.RestApi.Client.Tests.KSql.RestApi.Http;

public class BasicAuthHandlerTests : TestBase
{
  [Test]
  public async Task SendAsync()
  {
    //Arrange
    var options = new KSqlDBContextOptions("https://tests.com/");
    options.SetBasicAuthCredentials("fred", "letmein");

    var handler = new BasicAuthHandler(options);
    handler.InnerHandler = new HttpClientHandler();
    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://tests.com/");
    var invoker = new HttpMessageInvoker(handler);

    //Act
    var result = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

    //Assert
    httpRequestMessage.Headers!.Authorization!.Scheme.Should().Be("basic");
    httpRequestMessage.Headers.Authorization.Parameter.Should().Be("ZnJlZDpsZXRtZWlu");
  }
}
