namespace KoiGuardian.Api.Utils;

public class SD
{
    public static int RandomCode()
    {
        Random random = new Random();
        int randomNumber = random.Next(100000, 1000000);
        return randomNumber;
    }


}
