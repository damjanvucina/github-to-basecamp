using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.ServiceBus;
using Connector.Protobuf;

namespace Connector
{
    class ServiceBusQueueReceiver
    {
        private const string CampfireLine = "CAMPFIRE_LINE";
        private const string BasecampUpload = "BASECAMP_UPLOAD";
        private const string BasecampMessageBoardMessage = "BASECAMP_MESSAGE_BOARD_MESSAGE";

        private static string serviceBusConnectionString;
        private static string basecampToConnectorQueueName;
        private static IQueueClient queueClient;
        private static bool printDetails = true;
        private static Logger logger;

        public static void ReceiveMessages(Logger logger)
        {
            ServiceBusQueueReceiver.logger = logger;

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            queueClient = new QueueClient(serviceBusConnectionString, basecampToConnectorQueueName);
            
            // Register the queue message handler and receive messages in a loop
            RegisterOnMessageHandlerAndReceiveMessages();

            Console.ReadKey();
            await queueClient.CloseAsync();
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
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            if (printDetails)
            {
                Console.WriteLine();
                Console.WriteLine("**********************************************************");
                Console.WriteLine("Received message from the queue: " + Encoding.UTF8.GetString(message.Body));
                Console.WriteLine("**********************************************************");
                Console.WriteLine();
            }

            CampfireLine line = Protobuf.CampfireLine.Parser.ParseFrom(message.Body);
            BasecampUpload upload = Protobuf.BasecampUpload.Parser.ParseFrom(message.Body);
            BasecampMessageBoardMessage mbMessage = Protobuf.BasecampMessageBoardMessage.Parser.ParseFrom(message.Body);

            switch (line.Message.First().Type)
            {
                case CampfireLine:
                    logger.ProcessBasecampCampfireLine(line);
                    break;

                case BasecampUpload:
                    logger.ProcessBasecampUpload(upload);
                    break;

                case BasecampMessageBoardMessage:
                    logger.ProcessBasecampMessageBoardMessage(mbMessage);
                    break;

            }

            // Complete the message so that it is not received again.
            // This can be done only if the queue Client is created in ReceiveMode.PeekLock mode (which is the default).
            await queueClient.CompleteAsync(message.SystemProperties.LockToken);
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
            ServiceBusQueueReceiver.serviceBusConnectionString = serviceBusConnectionString;
        }

        internal static void SetBasecampToConnectorQueueName(string basecampToConnectorQueueName)
        {
            ServiceBusQueueReceiver.basecampToConnectorQueueName = basecampToConnectorQueueName;
        }
    }
}
