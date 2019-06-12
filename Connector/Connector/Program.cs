using System;
using System.Threading;

namespace Connector
{
    class Program
    {
        private static string MongoConnectionString = "CAN'T REALLY LEAVE THIS HERE IN PLAIN SIGHT FOR ALL THE GITHUBBERS CAN I?";
        private static string MongoDatabaseName = "ConnectorDB";
        private static string MongoCollectionName = "Configuration";
        private static Logger logger;
        static void Main(string[] args)
        {
            Console.Title = "CONNECTOR";
            logger = new Logger();
            MongoDBClient.ConnectAndSet(MongoConnectionString, MongoDatabaseName, MongoCollectionName);
            MongoDBClient.GetStoredGithubToBasecampMappings(logger);

            Console.WriteLine("CONNECTOR ACTIVATED");

            //Commented out since currently github-basecamp mappings are printed out as soon
            //as they arrive back to the connector (to its queue) from the basecamp endpoint;
            //Another option is to print out the mappings every time interval (npr every 10min)
            //Thread printLogsThread = new Thread(new ThreadStart(PrintLogs));
            //printLogsThread.Start();

            Thread GithubEndpointToConnectorThread = new Thread(new ThreadStart(ReceiveMessagesFromGithubEndpoint));
            GithubEndpointToConnectorThread.Start();

            Thread BasecampEndpointToConnectorThread = new Thread(new ThreadStart(ReceiveMessagesFromBasecampEndpoint));
            BasecampEndpointToConnectorThread.Start();
            
            Console.ReadKey();
        }

        private static void ReceiveMessagesFromGithubEndpoint()
        {
            ServiceBusReceiver.ReceiveMessages(logger);
        }

        private static void ReceiveMessagesFromBasecampEndpoint()
        {
            ServiceBusQueueReceiver.ReceiveMessages(logger);
        }

        private static void PrintLogs()
        {
            while (true)
            {
                logger.PrintLoggedDicts();
                Thread.Sleep(1000 * 10);
            }
        }

    }
}
