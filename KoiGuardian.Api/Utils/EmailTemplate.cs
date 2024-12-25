namespace KoiGuardian.Api.Utils;

public class EmailTemplate
{
    public static string Register(int code)
    {
        return "Here is your code to confirm account: " + code;
    }

    public static string CodeForResetPass(int code)
    {
        return "Here is code for reset pass: " + code;
    }


    public static string VerifySuccess(string name)
    {
        return $"Wellcome {name} to join Koi Guardian :v Please help me fix email template :v";
    }
}
