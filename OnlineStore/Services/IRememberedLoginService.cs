namespace OnlineStore.Services;

public sealed record RememberedLogin(string Email, string Password);

public interface IRememberedLoginService
{

    void Save(string email, string password);

    RememberedLogin? Load();

    void Clear();
}
