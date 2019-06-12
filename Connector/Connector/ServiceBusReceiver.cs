using Connector.Protobuf;
using Microsoft.Azure.ServiceBus;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Connector
{
    class ServiceBusReceiver
    {
        private const string Commit = "COMMIT";
        private const string File = "FILE";

        private static string serviceBusConnectionString;
        private static string topicName;
        private static string subscriptionName;
        private static ISubscriptionClient subscriptionClient;
        private static Logger logger;
        private static bool printDetails = true;

        public static void ReceiveMessages(Logger logger)
        {
            ServiceBusReceiver.logger = logger;

            MainAsync().GetAwaiter().GetResult();
        }


        static async Task MainAsync()
        {
            subscriptionClient = new SubscriptionClient(serviceBusConnectionString, topicName, subscriptionName);

            // Register subscription message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();
            await subscriptionClient.CloseAsync();
        }

        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false
            };

            // Register the function that processes messages.
            subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            if (printDetails)
            {
                Console.WriteLine();
                Console.WriteLine("**********************************************************");
                Console.WriteLine("Received message from the topic: " + Encoding.UTF8.GetString(message.Body));
                Console.WriteLine("**********************************************************");
                Console.WriteLine();
            }

            GithubFile file = GithubFile.Parser.ParseFrom(message.Body);
            GithubCommit commit = GithubCommit.Parser.ParseFrom(message.Body);

            switch (commit.Message.First().Type)
            {
                case Commit:
                    logger.ProcessGithubCommit(message);
                    break;

                case File:
                    logger.ProcessGithubFile(message);
                    break;
            }
            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is the default).
            await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        // Use this handler to examine the exceptions received on the message pump.
        static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

        internal static void SetServiceBusConnectionString(string serviceBusConnectionString)
        {
            ServiceBusReceiver.serviceBusConnectionString = serviceBusConnectionString;
        }

        internal static void SetTopicName(string topicName)
        {
            ServiceBusReceiver.topicName = topicName;
        }

        internal static void SetSubscriptionName(string subscriptionName)
        {
            ServiceBusReceiver.subscriptionName = subscriptionName;
        }
    }
}
