using System;
using BasecampEndpoint.Protobuf;
using Microsoft.Azure.ServiceBus;

namespace BasecampEndpoint
{
    class Program
    {
        private static string MongoConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
        private static string MongoDatabaseName = "BasecampEndpointDB";
        private static string MongoCollectionName = "Configuration";

        static void Main(string[] args)
        {
            Console.Title = "BASECAMP ENDPOINT";
            MongoDBClient.ConnectAndSet(MongoConnectionString, MongoDatabaseName, MongoCollectionName);

            Console.WriteLine("BASECAMP ENDPOINT ACTIVATED");
            EndpointClient.SetDefaultHeaders();
            ServiceBusQueueReceiver.ReceiveMessages();

            Console.ReadKey();
        }

        public static void ProcessCommit(GithubCommit commit)
        {
            Message lineMessage = EndpointClient.CreateCampfireLine(commit);
            ServiceBusQueueSender.SendMessage(lineMessage);

            Message messageBoardMessage = EndpointClient.CreateMessageBoardMessage(commit);
            ServiceBusQueueSender.SendMessage(messageBoardMessage);
        }

        public static void ProcessFile(GithubFile file)
        {
            string attachmentID = EndpointClient.CreateAttachment(file);
            Message uploadMessage = EndpointClient.CreateDocumentUpload(attachmentID, file);
           
            ServiceBusQueueSender.SendMessage(uploadMessage);
        }
    }
}
