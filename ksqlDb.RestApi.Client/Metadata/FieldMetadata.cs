using System.Reflection;

namespace ksqlDb.RestApi.Client.Metadata
{
  internal record FieldMetadata
  {
    public FieldMetadata(FieldMetadata fieldMetadata)
    {
      MemberInfo = fieldMetadata.MemberInfo;
      Ignore = fieldMetadata.Ignore;
      IgnoreInDML = fieldMetadata.IgnoreInDML;
      IgnoreInDDL = fieldMetadata.IgnoreInDDL;
      HasHeaders = fieldMetadata.HasHeaders;
      IsStruct = fieldMetadata.IsStruct;
      Path = fieldMetadata.Path;
      FullPath = fieldMetadata.FullPath;
      ColumnName = fieldMetadata.ColumnName;
    }

    internal MemberInfo MemberInfo { get; init; } = null!;
    public bool Ignore { get; internal set; }
    public bool IgnoreInDML { get; internal set; }
    internal bool IgnoreInDDL { get; set; }
    public bool HasHeaders { get; internal set; }
    internal bool IsStruct { get; set; }
    public bool IsPseudoColumn { get; internal set; }
    internal string Path { get; init; } = null!;
    internal string FullPath { get; init; } = null!;
    public string? ColumnName { get; set; }
  }
}
