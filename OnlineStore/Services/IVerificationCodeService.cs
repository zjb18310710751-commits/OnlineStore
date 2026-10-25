namespace OnlineStore.Services;

public interface IVerificationCodeService
{

    Task<string> GenerateAsync(string target);

    Task<bool> VerifyAsync(string target, string code);
}
