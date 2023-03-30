using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        private const int PORT = 7777;
        private static ConcurrentDictionary<int, TcpClient> clients = new ConcurrentDictionary<int, TcpClient>();
        private static int clientIdCounter = 0;

        static async Task Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            Console.WriteLine($"Waiting for clients on port {PORT}...");

            string previousFilePath = null;

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                clientIdCounter++;
                clients.TryAdd(clientIdCounter, client);
                Console.WriteLine($"Client {clientIdCounter} connected");
                // 클라이언트 핸들러 생성 및 시작
                _ = Task.Run(() => ClientHandler(clientIdCounter, client));
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync("http://r741.realserver2.com/api/post.php");
                    response.EnsureSuccessStatusCode();
                    string currentValue = await response.Content.ReadAsStringAsync();
                    if (previousFilePath != currentValue && currentValue != null)
                    {
                        SendMessageToAllClients(currentValue);
                        previousFilePath = currentValue;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        static void SendMessageToAllClients(string message)
        {
            byte[] sendBytes = Encoding.ASCII.GetBytes(message);
            foreach (TcpClient client in clients.Values)
            {
                client.GetStream().Write(sendBytes, 0, sendBytes.Length);
            }
        }

        private static async void ClientHandler(int clientId, TcpClient client)
        {
            try
            {
                while (true)
                {
                    byte[] bytes = new byte[1024];
                    int bytesRead = await client.GetStream().ReadAsync(bytes, 0, bytes.Length);
                    string message = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                    Console.WriteLine($"Received message from client {clientId}: {message}");

                    using (HttpClient httpClient = new HttpClient())
                    {
                        string url = "http://r741.realserver2.com/api/testJSON.php";
                        HttpContent content = new StringContent(message, Encoding.UTF8, "text/plain");
                        HttpResponseMessage response = await httpClient.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(responseBody);
                        }
                        else
                        {
                            Console.WriteLine($"Error: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                clients.TryRemove(clientId, out _);
                Console.WriteLine($"Client {clientId} disconnected");
            }
        }
    }
}

//Task.Run(async () =>
//{
//    while (true)
//    {
//        ConsoleKeyInfo keyInfo = Console.ReadKey();
//        if (keyInfo.Key == ConsoleKey.Enter)
//        {
//            SendMessageToAllClients("Hello, clients!");
//        }
//    }
//});