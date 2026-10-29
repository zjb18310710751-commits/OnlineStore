namespace OnlineStore.Services;

public sealed record RememberedLogin(string Email);

public interface IRememberedLoginService
{

    void Save(string email);

    RememberedLogin? Load();

    void Clear();
}
