namespace OnlineStore.Validation;

public static class PasswordPolicy
{
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{6,100}$";
}
