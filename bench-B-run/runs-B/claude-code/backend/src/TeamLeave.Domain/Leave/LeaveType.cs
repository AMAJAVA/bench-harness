using System.Text.RegularExpressions;
using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Leave;

/// <summary>Type de congé (RB-08, RB-09, RB-24).</summary>
public sealed partial class LeaveType : IVersioned
{
    public static readonly Guid CpId = Guid.Parse("00000000-0000-0000-0001-000000000001");
    public static readonly Guid RttId = Guid.Parse("00000000-0000-0000-0001-000000000002");
    public static readonly Guid SickId = Guid.Parse("00000000-0000-0000-0001-000000000003");
    public static readonly Guid OtherId = Guid.Parse("00000000-0000-0000-0001-000000000004");

    private LeaveType()
    {
        Code = string.Empty;
        Label = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public string Label { get; private set; }

    public bool IsTracked { get; private set; }

    public bool AllowsRetroactive { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public int Version { get; private set; }

    public static bool IsValidCode(string? code) => code is not null && CodeRegex().IsMatch(code);

    public static LeaveType Create(Guid id, string code, string label, bool isTracked, bool allowsRetroactive, bool isSystem = false)
    {
        if (!IsValidCode(code))
        {
            throw new ValidationException("code", "Le code doit comporter 2 à 10 caractères alphanumériques majuscules.");
        }

        var type = new LeaveType
        {
            Id = id,
            Code = code,
            IsTracked = isTracked,
            AllowsRetroactive = allowsRetroactive,
            IsSystem = isSystem,
            IsActive = true,
        };
        type.SetLabel(label);
        return type;
    }

    public void Update(string label, bool isTracked, bool allowsRetroactive, bool isActive)
    {
        SetLabel(label);
        IsTracked = isTracked;
        AllowsRetroactive = allowsRetroactive;
        IsActive = isActive;
    }

    public void Touch() => Version++;

    private void SetLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Trim().Length > 100)
        {
            throw new ValidationException("label", "Le libellé est obligatoire (100 caractères au plus).");
        }

        Label = label.Trim();
    }

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex CodeRegex();
}
