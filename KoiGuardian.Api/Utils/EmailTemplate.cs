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
        return $"<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Welcome to Koi Guardian</title>\r\n    <style>\r\n        body {{\r\n            font-family: Arial, sans-serif;\r\n            background-color: #f4f4f4;\r\n            margin: 0;\r\n            padding: 0;\r\n        }}\r\n        .email-container {{\r\n            max-width: 600px;\r\n            margin: 20px auto;\r\n            background: #ffffff;\r\n            border-radius: 8px;\r\n            overflow: hidden;\r\n            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);\r\n        }}\r\n        .header {{\r\n            background: url('https://down-vn.img.susercontent.com/file/a5a5286462ab7d04c7d887f714abd55b.webp') no-repeat center/cover;\r\n            text-align: center;\r\n            padding: 40px 20px;\r\n        }}\r\n        .header h1 {{\r\n            color: #fff;\r\n            font-size: 28px;\r\n            margin: 0;\r\n        }}\r\n        .content {{\r\n            padding: 20px;\r\n            text-align: center;\r\n        }}\r\n        .content p {{\r\n            font-size: 16px;\r\n            color: #333;\r\n        }}\r\n        .button {{\r\n            display: inline-block;\r\n            margin-top: 20px;\r\n            padding: 12px 24px;\r\n            background: #ff4500;\r\n            color: #fff;\r\n            text-decoration: none;\r\n            font-size: 16px;\r\n            border-radius: 5px;\r\n        }}\r\n        .footer {{\r\n            text-align: center;\r\n            padding: 15px;\r\n            font-size: 12px;\r\n            color: #777;\r\n        }}\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"email-container\">\r\n        <div class=\"header\">\r\n            <h1>Welcome to Koi Guardian!</h1>\r\n        </div>\r\n        <div class=\"content\">\r\n            <p>We are excited to have you onboard. Get ready to explore and manage your Koi with the best tools available.</p>\r\n            <a href=\"#\" class=\"button\">Get Started</a>\r\n        </div>\r\n        <div class=\"footer\">\r\n            <p>&copy; 2025 Koi Guardian. All rights reserved.</p>\r\n        </div>\r\n    </div>\r\n</body>\r\n</html>\r\n";
    }
}
