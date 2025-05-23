using System.Reflection;
using System.Text;
using ksqlDb.RestApi.Client.FluentAPI.Builders;
using ksqlDB.RestApi.Client.Infrastructure.Extensions;
using ksqlDB.RestApi.Client.KSql.RestApi.Enums;
using ksqlDb.RestApi.Client.KSql.RestApi.Parsers;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements;
using ksqlDB.RestApi.Client.KSql.RestApi.Statements.Properties;
using ksqlDb.RestApi.Client.Metadata;
using static ksqlDB.RestApi.Client.KSql.RestApi.Enums.IdentifierEscaping;

namespace ksqlDB.RestApi.Client.KSql.RestApi.Generators;

internal sealed class TypeGenerator : EntityInfo
{
  private readonly IMetadataProvider metadataProvider;

  public TypeGenerator(IMetadataProvider metadataProvider) : base(metadataProvider)
  {
    this.metadataProvider = metadataProvider;
  }

  internal string Print<T>(TypeProperties properties)
  {
    StringBuilder stringBuilder = new();
    var typeName = EntityProvider.GetFormattedName<T>(properties, EscapeName);
    stringBuilder.Append($"CREATE TYPE {typeName} AS {KSqlTypes.Struct}<");

    PrintProperties<T>(stringBuilder, properties.IdentifierEscaping);

    stringBuilder.Append(">;");

    return stringBuilder.ToString();
  }

  private void PrintProperties<T>(StringBuilder stringBuilder, IdentifierEscaping escaping)
  {
    var ksqlProperties = new List<string>();

    KSqlTypeTranslator<T> typeTranslator = new(metadataProvider);

    foreach (var memberInfo in Members<T>())
    {
      var type = GetMemberType(memberInfo);

      var ksqlType = typeTranslator.Translate(type, memberInfo, escaping);

      var memberName = memberInfo.GetMemberName(metadataProvider);
      var columnDefinition = $"{EscapeName(memberName, escaping)} {ksqlType}{typeTranslator.ExploreAttributes(typeof(T), memberInfo, type)}";
      ksqlProperties.Add(columnDefinition);
    }

    stringBuilder.Append(string.Join(", ", ksqlProperties));
  }

  private static string EscapeName(string name, IdentifierEscaping escaping) =>
    (escaping, IdentifierUtil.IsValid(name)) switch
    {
      (Never, _) => name,
      (Keywords, true) => name,
      (Keywords, false) => $"`{name}`",
      (Always, _) => $"`{name}`",
      _ => throw new ArgumentOutOfRangeException(nameof(escaping), escaping, "Non-exhaustive match")
    };

  protected override bool IncludeMemberInfo<TEntity>(Type type, EntityMetadata? entityMetadata, MemberInfo memberInfo, bool? includeReadOnly = null)
  {
    var fieldMetadata = entityMetadata?.GetFieldMetadataBy(memberInfo);
    if (fieldMetadata is { IgnoreInDDL: true })
      return false;

    return base.IncludeMemberInfo<TEntity>(type, entityMetadata, memberInfo, includeReadOnly) && !memberInfo.GetCustomAttributes().OfType<IgnoreInDDLAttribute>().Any();
  }
}
