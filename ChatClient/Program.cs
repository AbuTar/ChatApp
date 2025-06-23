// This program is responsible for handling any client logic
// 1 - Connecting to the server
// 2 - Sending user input to the server
// 3 - Receiving and displaying messages from the server
using System.Net.Sockets;

public class Client
{
    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;

    public void Connect_To_Server(string IPAddress, int port)
    {
    // Here I am creating the connection between the client and the server
        client = new TcpClient(IPAddress, port);
        stream = client.GetStream();
        reader = new StreamReader(stream);
        writer = new StreamWriter(stream){ AutoFlush = true };

        Console.WriteLine("You have successfully conntected to the Server!");

        // This loop continuously reads user input and sends it to the server
        // Without it, the client would exit immediately after connecting

        while (true)
        {
            string message = Console.ReadLine();
            Send_Message(message);
        }

    }

    public void Send_Message(string message)
    {
        if (writer != null)
        {
            writer.WriteLine(message);
            Console.WriteLine("Message Sent: " + message);

        }
    }

    public void Read_Messages()
    {
        string message;
        while ((message = reader.ReadLine()) != null)
        {
            Console.WriteLine("Server: " + message);
        }
    }
}




class Program
{
    static void Main()
    {
        Client client = new Client();
        client.Connect_To_Server("127.0.0.1", 2025);
         
    }
}
