using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace BasecampEndpoint
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

                string serviceBusConnectionString = (string) obj["serviceBusConnectionString"];
                string basecampToConnectorQueueName = (string)obj["basecampToConnectorQueueName"];
                string connectorToBasecampQueueName = (string)obj["connectorToBasecampQueueName"];
                string authorizationToken = (string)obj["authorizationToken"];
                string createCampfireLineUri = (string)obj["createCampfireLineUri"];
                string createAttachmentUri = (string)obj["createAttachmentUri"];
                string createUploadUri = (string)obj["createUploadUri"];
                string createMessageBoardMessageUri = (string)obj["createMessageBoardMessageUri"];
                string storageConnectionString = (string)obj["storageConnectionString"];
                string containerName = (string)obj["containerName"];

                SetUpConnectionInfrastructureStrings(serviceBusConnectionString, basecampToConnectorQueueName,
                                                     connectorToBasecampQueueName, authorizationToken, 
                                                     createCampfireLineUri, createAttachmentUri, createUploadUri,
                                                     createMessageBoardMessageUri, storageConnectionString, containerName);
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

            string serviceBusConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string basecampToConnectorQueueName = "basecamptoconnectorqueue";
            string connectorToBasecampQueueName = "connectortobasecampqueue";
            string authorizationToken = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string createCampfireLineUri = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string createAttachmentUri = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string createUploadUri = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string createMessageBoardMessageUri = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string storageConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string containerName = "githubfiles";

            document.Add("serviceBusConnectionString", serviceBusConnectionString);
            document.Add("basecampToConnectorQueueName", basecampToConnectorQueueName);
            document.Add("connectorToBasecampQueueName", connectorToBasecampQueueName);
            document.Add("authorizationToken", authorizationToken);
            document.Add("createCampfireLineUri", createCampfireLineUri);
            document.Add("createAttachmentUri", createAttachmentUri);
            document.Add("createUploadUri", createUploadUri);
            document.Add("createMessageBoardMessageUri", createMessageBoardMessageUri);
            document.Add("storageConnectionString", storageConnectionString);
            document.Add("containerName", containerName);

            collection.InsertOne(document);

            SetUpConnectionInfrastructureStrings(serviceBusConnectionString, basecampToConnectorQueueName,
                                                 connectorToBasecampQueueName, authorizationToken, createCampfireLineUri,
                                                 createAttachmentUri, createUploadUri, createMessageBoardMessageUri,
                                                 storageConnectionString, containerName);
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

        private static void SetUpConnectionInfrastructureStrings(string serviceBusConnectionString, string basecampToConnectorQueueName,
                                                                 string connectorToBasecampQueueName, string authorizationToken,
                                                                 string createCampfireLineUri, string createAttachmentUri,
                                                                 string createUploadUri, string createMessageBoardMessageUri,
                                                                 string storageConnectionString, string containerName)
        {
            ServiceBusQueueSender.SetServiceBusConnectionString(serviceBusConnectionString);
            ServiceBusQueueSender.SetBasecampToConnectorQueueName(basecampToConnectorQueueName);
            ServiceBusQueueReceiver.SetServiceBusConnectionString(serviceBusConnectionString);
            ServiceBusQueueReceiver.SetConnectorToBasecampQueueName(connectorToBasecampQueueName);
            EndpointClient.SetAuthorizationToken(authorizationToken);
            EndpointClient.SetCreateCampfireLineUri(createCampfireLineUri);
            EndpointClient.SetCreateAttachmentUri(createAttachmentUri);
            EndpointClient.SetCreateUploadUri(createUploadUri);
            EndpointClient.SetCreateMessageBoardMessageUri(createMessageBoardMessageUri);
            BlobStorageClient.SetStorageConnectionString(storageConnectionString);
            BlobStorageClient.SetContainerName(containerName);
        }

        private static bool CollectionExists(IMongoDatabase database, string collectionName)
        {

            var filter = new BsonDocument("name", collectionName);
            var options = new ListCollectionNamesOptions { Filter = filter };

            return database.ListCollectionNames(options).Any();
        }
    }
}
