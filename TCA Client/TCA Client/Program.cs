using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 서버에 접속
            TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, 7777);

            Console.WriteLine("서버에 접속되었습니다.");

            // 서버에서 보낸 메시지를 받아들이는 Task를 실행
            _ = ReceiveMessages(client);

            // 메시지 입력을 대기
            while (true)
            {
                Console.Write("\n메시지 입력: ");
                string message = Console.ReadLine();
                if (message.ToLower() == "exit")
                    break;

                // 서버로 메시지 전송
                SendMessageToServer(client, message);
            }

            // 서버와의 연결 종료
            client.Close();
            Console.WriteLine("서버와의 연결이 종료되었습니다.");
        }

        static void SendMessageToServer(TcpClient client, string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] bytesToSend = Encoding.UTF8.GetBytes(message);
            stream.Write(bytesToSend, 0, bytesToSend.Length);
        }

        static async Task ReceiveMessages(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                // 서버로부터 메시지를 받음
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, byteCount);

                // 메시지 출력
                Console.WriteLine($"\n서버: {receivedMessage}");
            }
        }
    }
}
