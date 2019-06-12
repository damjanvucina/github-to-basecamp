using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static Connector.Logger;

namespace Connector
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

                string serviceBusConnectionString = (string)obj["serviceBusConnectionString"];
                string topicName = (string)obj["topicName"];
                string subscriptionName = (string)obj["subscriptionName"];
                string connectorToBasecampQueueName = (string)obj["connectorToBasecampQueueName"];
                string basecampToConnectorQueueName = (string)obj["basecampToConnectorQueueName"];

                SetUpConnectionInfrastructureStrings(serviceBusConnectionString, topicName,
                                                     subscriptionName, connectorToBasecampQueueName,
                                                     basecampToConnectorQueueName);
            }
            else
            {
                CreateDBCollection(collectionName);
            }
        }

        internal static void GetStoredGithubToBasecampMappings(Logger logger)
        {
            IMongoCollection<BsonDocument> currentCollection = null;
            List<BsonDocument> currentList = null;

            Console.WriteLine("RECREATING BASECAMP DATA STORED IN MONGODB");
            Console.WriteLine("======================================================================");

            Console.WriteLine("RECREATING STORED COMMIT SHA TO CAMPFIRE LINE DATA");

            currentCollection = db.GetCollection<BsonDocument>("CommitShaToCampfireLineData");
            currentList = currentCollection.Find<BsonDocument>(FilterDefinition<BsonDocument>.Empty)
                                                .Project<BsonDocument>("{_id: 0}").ToList();

            currentList.ForEach(doc =>
            {
                JObject obj = JObject.Parse(doc.ToJson());

                string commitSha = obj["commitSha"].ToString();
                string campfireLineId = obj["campfireLineId"].ToString();
                string campfireId = obj["campfireId"].ToString();
                string projectId = obj["projectId"].ToString();

                CampfireLineData lineData = new CampfireLineData(campfireLineId, campfireId, projectId);
                logger.CreateCommitShaToCampfireLineMapping(commitSha, lineData);
            });

            Console.WriteLine("----------------------------------------------------------------------");
            Console.WriteLine("RECREATING STORED COMMIT SHA TO BASECAMP UPLOAD DATA");

            currentCollection = db.GetCollection<BsonDocument>("GithubFileNameToBasecampUploadData");
            currentList = currentCollection.Find<BsonDocument>(FilterDefinition<BsonDocument>.Empty)
                                           .Project<BsonDocument>("{_id: 0}").ToList();

            currentList.ForEach(doc =>
            {
                JObject obj = JObject.Parse(doc.ToJson());

                string githubFileName = obj["githubFileName"].ToString();
                string uploadId = obj["uploadId"].ToString();
                string basecampFileName = obj["fileName"].ToString();
                string bucketId = obj["bucketId"].ToString();
                string appDownloadUrl = obj["appDownloadUrl"].ToString();

                BasecampUploadData uploadData = new BasecampUploadData(uploadId, basecampFileName, bucketId, appDownloadUrl);
                logger.CreateGithubFileNameToBasecampUploadDataMapping(githubFileName, uploadData);
            });

            Console.WriteLine("----------------------------------------------------------------------");
            Console.WriteLine("RECREATING STORED COMMIT SHA TO MESSAGE BOARD MESSAGE DATA");

            currentCollection = db.GetCollection<BsonDocument>("CommitShaToMessageBoardMessageData");
            currentList = currentCollection.Find<BsonDocument>(FilterDefinition<BsonDocument>.Empty)
                                           .Project<BsonDocument>("{_id: 0}")
                                           .ToList();

            currentList.ForEach(doc =>
            {
                JObject obj = JObject.Parse(doc.ToJson());

                string commitSha = obj["commitSha"].ToString();
                string messageBoardMessageId = obj["messageBoardMessageId"].ToString();
                string projectId = obj["projectId"].ToString();

                MessageBoardMessageData mbMessageData = new MessageBoardMessageData(messageBoardMessageId, projectId);
                logger.CreateCommitShaToMessageBoardMessageDataMapping(commitSha, mbMessageData);
            });

            Console.WriteLine("======================================================================");
            Console.WriteLine("DONE RECREATING BASECAMP DATA STORED IN MONGODB");
            Console.WriteLine();
        }

        private static void SetUpConnectionInfrastructureStrings(string serviceBusConnectionString,
                                                                 string topicName, string subscriptionName,
                                                                 string connectorToBasecampQueueName,
                                                                 string basecampToConnectorQueueName)
        {
            ServiceBusReceiver.SetServiceBusConnectionString(serviceBusConnectionString);
            ServiceBusReceiver.SetTopicName(topicName);
            ServiceBusReceiver.SetSubscriptionName(subscriptionName);

            ServiceBusQueueSender.SetServiceBusConnectionString(serviceBusConnectionString);
            ServiceBusQueueSender.SetConnectorToBasecampQueueName(connectorToBasecampQueueName);

            ServiceBusQueueReceiver.SetServiceBusConnectionString(serviceBusConnectionString);
            ServiceBusQueueReceiver.SetBasecampToConnectorQueueName(basecampToConnectorQueueName);
        }

        private static void CreateDBCollection(string collectionName)
        {
            db.CreateCollection(collectionName);
            collection = db.GetCollection<BsonDocument>(collectionName);

            var document = new BsonDocument();

            string serviceBusConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
            string topicName = "githubendpointtoconnector";
            string subscriptionName = "connectorsubscription";
            string connectorToBasecampQueueName = "connectortobasecampqueue";
            string basecampToConnectorQueueName = "basecamptoconnectorqueue";

            document.Add("serviceBusConnectionString", serviceBusConnectionString);
            document.Add("topicName", topicName);
            document.Add("subscriptionName", subscriptionName);
            document.Add("connectorToBasecampQueueName", connectorToBasecampQueueName);
            document.Add("basecampToConnectorQueueName", basecampToConnectorQueueName);

            collection.InsertOne(document);

            SetUpConnectionInfrastructureStrings(serviceBusConnectionString, topicName,
                                                 subscriptionName, connectorToBasecampQueueName,
                                                 basecampToConnectorQueueName);
        }

        internal static void StoreCommitShaToCampfireLineData(string commitSha, CampfireLineData lineData)
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("CommitShaToCampfireLineData");

            var document = new BsonDocument();

            document.Add("commitSha", commitSha);
            document.Add("campfireLineId", lineData.campfireLineId);
            document.Add("campfireId", lineData.campfireId);
            document.Add("projectId", lineData.projectId);

            collection.InsertOne(document);
        }

        internal static void StoreGithubFileToBasecampUploadData(string githubFileName, BasecampUploadData uploadData)
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("GithubFileNameToBasecampUploadData");

            var document = new BsonDocument();

            document.Add("githubFileName", githubFileName);
            document.Add("uploadId", uploadData.uploadId);
            document.Add("fileName", uploadData.fileName);
            document.Add("bucketId", uploadData.bucketId);
            document.Add("appDownloadUrl", uploadData.appDownloadUrl);

            collection.InsertOne(document);
        }

        internal static void StoreCommitShaToMessageBoardMessageData(string commitSha, MessageBoardMessageData mbMessageData)
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("CommitShaToMessageBoardMessageData");

            var document = new BsonDocument();

            document.Add("commitSha", commitSha);
            document.Add("messageBoardMessageId", mbMessageData.messageBoardMessageId);
            document.Add("projectId", mbMessageData.projectId);

            collection.InsertOne(document);
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

        private static bool CollectionExists(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var options = new ListCollectionNamesOptions { Filter = filter };

            return database.ListCollectionNames(options).Any();
        }
    }
}
