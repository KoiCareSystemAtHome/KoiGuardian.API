namespace KoiGuardian.Api.Services;

public interface ICurrentUser
{
    string UserName();

    string Rolename(); 

}

public class CurrentUser : ICurrentUser
{
    public string Rolename()
    {
        throw new NotImplementedException();
    }

    public string UserName()
    {
        throw new NotImplementedException();
    }
}