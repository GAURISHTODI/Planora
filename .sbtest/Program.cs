using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace sbtest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = "Endpoint=sb://planorabus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=d1eblf4SaQRJsu8d3mH/Jds+4yPSVCuvS+ASbF9RfWw=";
            
            try
            {
                var client = new ServiceBusClient(connectionString);
                var sender = client.CreateSender("ai-generation-jobs");
                Console.WriteLine("Connection and sender creation successful.");
                
                // Try to send a blank message just to test if the queue exists.
                // If the queue doesn't exist, this will throw MessagingEntityNotFoundException
                await sender.SendMessageAsync(new ServiceBusMessage("TEST"));
                Console.WriteLine("Message sent successfully. Queue exists!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
