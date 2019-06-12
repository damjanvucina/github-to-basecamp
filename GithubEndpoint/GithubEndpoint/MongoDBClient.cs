using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace GithubEndpoint
{
    class MongoDBClient
    {
        private static MongoClient client;
        private static IMongoDatabase db;
        private static IMongoCollection<BsonDocument> collection;

        internal static void ConnectAndSet(string connectionString, string databaseName, string collectionName)
        {
            Console.WriteLine("CONNECTING TO MONGODB");
            client = new MongoClient(connectionString);

            db = client.GetDatabase(databaseName);

            if (CollectionExists(db, collectionName))
            {
                collection = db.GetCollection<BsonDocument>(collectionName);

                BsonDocument document = collection.Find(FilterDefinition<BsonDocument>.Empty)
                                                  .Project<BsonDocument>("{_id: 0}")
                                                  .First();

                string json = document.ToJson();

                JObject obj = JObject.Parse(json);

                string repoName = (string)obj["repoName"];
                string githubOAuthToken = (string)obj["githubOAuthToken"];
                string userLogin = (string)obj["userLogin"];
                string lastProcessedCommitDate = (string)obj["lastProcessedCommitDate"];
                string storageConnectionString = (string)obj["storageConnectionString"];
                string containerName = (string)obj["containerName"];
                string serviceBusConnectionString = (string)obj["serviceBusConnectionString"];
                string topicName = (string)obj["topicName"];

                SetUpConnectionInfrastructureStrings(repoName, githubOAuthToken, userLogin, lastProcessedCommitDate,
                                                     storageConnectionString, containerName, serviceBusConnectionString,                            topicName);
            }
            else
            {
                CreateDBCollection(collectionName);
            }
        }

        private static void CreateDBCollection(string collectionName)
        {
            db.CreateCollection(collectionName);
            collection = db.GetCollection<BsonDocument>(collectionName);

            var document = new BsonDocument();

            string repoName = "githubapitesting";
            string githubOAuthToken = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string userLogin = "damjanvucina";
            string lastProcessedCommitDate = "1/1/0001 12:00:00 AM +00:00";
            string storageConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string containerName = "githubfiles";
            string serviceBusConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string topicName = "githubendpointtoconnector";

            document.Add("repoName", repoName);
            document.Add("githubOAuthToken", githubOAuthToken);
            document.Add("userLogin", userLogin);
            document.Add("lastProcessedCommitDate", lastProcessedCommitDate);

            document.Add("storageConnectionString", storageConnectionString);
            document.Add("containerName", containerName);
            document.Add("serviceBusConnectionString", serviceBusConnectionString);
            document.Add("topicName", topicName);

            collection.InsertOne(document);

            SetUpConnectionInfrastructureStrings(repoName, githubOAuthToken, userLogin, lastProcessedCommitDate,
                                                 storageConnectionString, containerName, serviceBusConnectionString,                            topicName);
        }

        private static bool DatabaseExists(string databaseName)
        {
            using (var cursor = client.ListDatabases())
            {
                foreach (BsonDocument db in cursor.ToList<BsonDocument>())
                {
                    JObject obj = JObject.Parse(db.ToJson());
                    string name = (string)obj["name"];

                    if (name.Equals(databaseName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void SetUpConnectionInfrastructureStrings(string repoName, string githubOAuthToken, 
                                                                 string userLogin,
                                                                 string lastProcessedCommitDate,
                                                                 string storageConnectionString,
                                                                 string containerName, string serviceBusConnectionString,                                       string topicName)
        {
            Program.SetRepoName(repoName);
            EndpointClient.SetGithubOAuthToken(githubOAuthToken);
            EndpointClient.SetUserLogin(userLogin);
            EndpointClient.SetLastProcessedCommitDate(lastProcessedCommitDate);
            BlobStorageUploader.SetStorageConnectionString(storageConnectionString);
            BlobStorageUploader.SetContainerName(containerName);
            ServiceBusSender.SetServiceBusConnectionString(serviceBusConnectionString);
            ServiceBusSender.SetTopicName(topicName);
        }

        internal static void UpdateLastProcessedCommitDate(DateTimeOffset lastProcessedCommitDate)
        {
            var update = Builders<BsonDocument>.Update.Set("lastProcessedCommitDate", lastProcessedCommitDate.ToString());
            collection.UpdateOne(FilterDefinition<BsonDocument>.Empty, update);
        }

        private static bool CollectionExists(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var options = new ListCollectionNamesOptions { Filter = filter };

            return database.ListCollectionNames(options).Any();
        }
    }
}
