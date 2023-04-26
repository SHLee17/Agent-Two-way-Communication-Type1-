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
        static string logFile;

        static async Task Main(string[] args)
        {

            string log = Path.Combine(Directory.GetCurrentDirectory(), "Log");
            NewFolder(log, "LogFolder");
            logFile = Path.Combine(log, "Log");
            NewFile(logFile, "Log");

            TcpListener listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            Console.WriteLine($"Waiting for clients on port {PORT}...");

            string previousValue = null;

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
                    if (previousValue != currentValue && currentValue != null)
                    {
                        SendMessageToAllClients(currentValue);
                        previousValue = currentValue;

                        File.AppendAllText(logFile, $"{currentValue}\n");
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

                            string logMessage = $"[{DateTime.Now}] Received message from client {clientId}: {message}\n";
                            logMessage += $"[{DateTime.Now}] Response from web API: {responseBody}\n";
                            File.AppendAllText(logFile, logMessage);
                        }
                        else
                        {
                            Console.WriteLine($"Error: {response.StatusCode}");

                            string logMessage = $"[{DateTime.Now}] Received message from client {clientId}: {message}\n";
                            logMessage += $"[{DateTime.Now}] Error: {response.StatusCode}\n";
                            File.AppendAllText(logFile, logMessage);
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
        static void NewFile(string path, string name)
        {
            if (File.Exists(path))
            {
                Console.WriteLine("중복된 파일 이름이 존재합니다.");
            }
            else
            {
                // 파일 생성
                File.WriteAllText(path, "");
                Console.WriteLine($"{name} 파일이 생성되었습니다.");
            }
        }
        static void NewFolder(string path, string name)
        {
            if (!Directory.Exists(path))
            {
                // 폴더가 없는 경우 폴더 만들기
                Directory.CreateDirectory(path);
                Console.WriteLine($"New {name} folder created.");
            }
            else
            {
                Console.WriteLine($"{name} Folder already exists.");
            }
            Console.WriteLine($"{name} Folder path: " + path);
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