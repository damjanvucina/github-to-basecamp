using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Connector
{
    class ServiceBusQueueSender
    {
        private static string serviceBusConnectionString;
        private static string connectorToBasecampQueueName;
        private static IQueueClient queueClient;
        private static bool printDetails = true;

        public static void SendMessages(Message message)
        {
            MainAsync(message).GetAwaiter().GetResult();
        }

        static async Task MainAsync(Message message)
        {
            queueClient = new QueueClient(serviceBusConnectionString, connectorToBasecampQueueName);

            await SendMessagesAsync(message);

            await queueClient.CloseAsync();
        }

        static async Task SendMessagesAsync(Message message)
        {
            try
            {
                if (printDetails)
                {
                    Console.WriteLine();
                    Console.WriteLine("**********************************************************");
                    Console.WriteLine("Sending message to the queue: " + Encoding.UTF8.GetString(message.Body));
                }
                // Send the message to the queue.
                await queueClient.SendAsync(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }

            if (printDetails)
            {
                Console.WriteLine("Message successfully sent to the queue.");
                Console.WriteLine("**********************************************************");
                Console.WriteLine();
            }
        }

        internal static void SetServiceBusConnectionString(string serviceBusConnectionString)
        {
            ServiceBusQueueSender.serviceBusConnectionString = serviceBusConnectionString;
        }

        internal static void SetConnectorToBasecampQueueName(string connectorToBasecampQueueName)
        {
            ServiceBusQueueSender.connectorToBasecampQueueName = connectorToBasecampQueueName;
        }
    }
}
