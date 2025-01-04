using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace KoiGuardian.DataAccess.MongoDB;

public interface IKoiMongoDb
{
    public Task<object> GetDataFromMongo();
    public Task<object> GetDataFromMongo(string userId);
}

public class KoiMongoDb : IKoiMongoDb
{
    public async Task<object> GetDataFromMongo()
    {
        const string connectionUri = "mongodb+srv://koiguadian:Koisp25%40@koiguardian.btv0j.mongodb.net/?retryWrites=true&w=majority&appName=KoiGuardian";


        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);
            
        try
        {
            var result = client.GetDatabase("KoiGuardian").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");

            var database = client.GetDatabase("KoiGuardian");
            var collection = database.GetCollection<BsonDocument>("EWallet");

            var users = collection.Find(new BsonDocument()).ToList();

            foreach (var user in users)
            {
                Console.WriteLine(user.ToString());
            }

            var newUser = new BsonDocument
    {
                { "name", "John Doe" },
                { "email", "john.doe@example.com" },
                { "age", 30 },
                { "createdAt", DateTime.Now }
            };

            collection.InsertOne(newUser);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }


        return null;
    }

    public async Task<object> GetDataFromMongo(string userId)
    {
        const string connectionUri = "mongodb+srv://koiguadian:Koisp25%40@koiguardian.btv0j.mongodb.net/?retryWrites=true&w=majority&appName=KoiGuardian";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        // Set the ServerApi field of the settings object to set the version of the Stable API on the client
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        // Create a new client and connect to the server
        var client = new MongoClient(settings);
        // Send a ping to confirm a successful connection
        try
        {
            var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }


        return null;
    }
}
