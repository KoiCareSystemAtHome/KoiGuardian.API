namespace KoiGuardian.Api.Utils;

public class EmailTemplate
{
    public static string Register(int code)
    {
        return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Confirm Your Account</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-image: url('https://i.pinimg.com/736x/fa/bd/06/fabd0640dd36345c72cee1f18f618917.jpg'); background-size: cover; background-repeat: no-repeat;"">
    <div style=""background-color: rgba(255, 255, 255, 0.9); max-width: 600px; margin: 50px auto; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0,0,0,0.2);"">
        <h2 style=""text-align: center; color: #333;"">Confirm Your Account</h2>
        <p style=""text-align: center; font-size: 16px; color: #555;"">Here is your code to confirm your account:</p>
        <p style=""text-align: center; font-size: 32px; font-weight: bold; color: #007BFF;"">{code}</p>
        <p style=""text-align: center; color: #999; font-size: 12px;"">This code will expire in 10 minutes.</p>
    </div>
</body>
</html>";
    }


    public static string CodeForResetPass(int code)
    {
        return "Here is code for reset pass: " + code;
    }


    public static string VerifySuccess(string name)
    {
        return $"<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Welcome to Koi Guardian</title>\r\n    <style>\r\n        body {{\r\n            font-family: Arial, sans-serif;\r\n            background-color: #f4f4f4;\r\n            margin: 0;\r\n            padding: 0;\r\n        }}\r\n        .email-container {{\r\n            max-width: 600px;\r\n            margin: 20px auto;\r\n            background: #ffffff;\r\n            border-radius: 8px;\r\n            overflow: hidden;\r\n            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);\r\n        }}\r\n        .header {{\r\n            background: url('https://down-vn.img.susercontent.com/file/a5a5286462ab7d04c7d887f714abd55b.webp') no-repeat center/cover;\r\n            text-align: center;\r\n            padding: 40px 20px;\r\n        }}\r\n        .header h1 {{\r\n            color: #fff;\r\n            font-size: 28px;\r\n            margin: 0;\r\n        }}\r\n        .content {{\r\n            padding: 20px;\r\n            text-align: center;\r\n        }}\r\n        .content p {{\r\n            font-size: 16px;\r\n            color: #333;\r\n        }}\r\n        .button {{\r\n            display: inline-block;\r\n            margin-top: 20px;\r\n            padding: 12px 24px;\r\n            background: #ff4500;\r\n            color: #fff;\r\n            text-decoration: none;\r\n            font-size: 16px;\r\n            border-radius: 5px;\r\n        }}\r\n        .footer {{\r\n            text-align: center;\r\n            padding: 15px;\r\n            font-size: 12px;\r\n            color: #777;\r\n        }}\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"email-container\">\r\n        <div class=\"header\">\r\n            <h1>Welcome to Koi Guardian!</h1>\r\n        </div>\r\n        <div class=\"content\">\r\n            <p>We are excited to have you onboard. Get ready to explore and manage your Koi with the best tools available.</p>\r\n            <a href=\"#\" class=\"button\">Get Started</a>\r\n        </div>\r\n        <div class=\"footer\">\r\n            <p>&copy; 2025 Koi Guardian. All rights reserved.</p>\r\n        </div>\r\n    </div>\r\n</body>\r\n</html>\r\n";
    }
}
