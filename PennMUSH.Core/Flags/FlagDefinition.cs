namespace PennMUSH.Core.Flags;

using PennMUSH.Core.Types;

public class FlagDefinition
{
    public string Name { get; init; } = string.Empty;
    public char Letter { get; init; } = '\0';
    public ObjectType TargetType { get; init; } = ObjectType.None; // ObjectType.None means any type
    public FlagPrivileges Permissions { get; init; } = FlagPrivileges.Any;
    public FlagPrivileges NegatePermissions { get; init; } = FlagPrivileges.Any;
    public bool IsPower { get; init; } = false;
}