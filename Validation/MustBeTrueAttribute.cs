using System.ComponentModel.DataAnnotations;

namespace WaldauCastle.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public MustBeTrueAttribute() => ErrorMessage = "Необходимо дать согласие.";

    public override bool IsValid(object? value) => value is true;
}
