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
        writer = new StreamWriter(stream) { AutoFlush = true };

        // We need to implement names so we must ask new clients for an identifier

        Console.WriteLine("Please enter your name: ");
        string name = Console.ReadLine();
        writer.WriteLine(name);  // Send name to server immediately
        writer.Flush();

        Console.WriteLine("You have successfully conntected to the Server!");
        Console.WriteLine("Enter a message or type exit to leave the server");
        // This loop continuously reads user input and sends it to the server
        // Without it, the client would exit immediately after connecting

        Thread readThread = new Thread(Read_Messages);
        readThread.Start();

        while (true)
        {
            // Here I have decided to add the option of sending a message, or leaving the server

            string message = Console.ReadLine();

            if (message == "exit")
            {
                Console.WriteLine("Disconnecting......");
                Disconnect();
            }
            
            else
            {
                Send_Message(message);
            }



        }

    }

    public void Send_Message(string message)
    {
        if (writer != null)
        {
            writer.WriteLine(message);
            // Console.WriteLine("Message Sent: " + message);

        }
    }

    public void Read_Messages()
    {
        string message;
        while ((message = reader.ReadLine()) != null)
        {
            Console.WriteLine(message);
        }
    }

    private void Disconnect()
    {
        // This is here to allow users to disconnect from the server when they want to
        stream.Close();
        client.Close();
        Environment.Exit(0);

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

