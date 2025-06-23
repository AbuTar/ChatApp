// This is the chat server responsible for:
// 1 - Listening to incoming client connections
// 2 - Routing messages between clients

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

public class TCP_Server
{
    private TcpListener listener;
    // This lists helps keep track of the list of the connected clients 
    private List<TcpClient> clients = new List<TcpClient>();

    public void Start_Server(int port)
    // This function here is reponisble for intialising and staring the server
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("The Server has started!");
        Console.WriteLine("Waiting for Connections........");

        // Without threading, I'm blocking new clients from connecting to the server
        // So I must make every Client connection on a new thread
        Thread client_thread = new Thread(Accept_Clients);
        client_thread.Start();
    }

    private void Accept_Clients()
    {
        while (true)
        // Here we wait until a client connects
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);
                Console.WriteLine("A Client has connected.");

                Thread handle_client_thread = new Thread(() => Handle_Client(client));
                handle_client_thread.Start();
            }

            catch (Exception ex)
            {
                Console.WriteLine("There was an error accepting the client:" + ex.Message);
            }
        }
    }

    private void Handle_Client(TcpClient client)
    {
        // Network Steam is used to either send or receive messages
        NetworkStream stream = client.GetStream();

        // I was converting bytes manually before and having to deal with buffer mangagement
        // By using StreamReader and StreamWriter so that I can receive and send how lines
        // Not individual characters

        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream){ AutoFlush = true };

        // Welcome Message
        writer.WriteLine("Welcome to the server!");


        try
        {
            while (client.Connected)
            {
                string message = reader.ReadLine();
                if (message == null)
                    break; // Client disconnects if no message is detected

                Console.WriteLine("Received: " + message);

                // Still Echoing the message back to the client for debugging purposes
                writer.WriteLine("Echo: " + message);
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine("There was an error communcating with the client");
        }

        finally
        {
            stream.Close();
            client.Close();
            clients.Remove(client);
            Console.WriteLine("Client has disconnected. ");
        }
        
            

        


    }
}          

        

class Program
{
    static void Main()
    {
        TCP_Server my_server = new TCP_Server();
        my_server.Start_Server(2025); // This port can be anything

    }
}



























//private void Handle_Client(TcpClient client)
   // {
        // Network Steam is used to either send or receive messages
        // NetworkStream stream = client.GetStream();
        // StreamReader reader = new StreamReader(stream);
        // StreamWriter writer = new StreamWriter(stream);

        // byte[] buffer = new byte[1024];
        // int received_bytes;

        // try
        // {
        //     byte[] welcome_message = Encoding.Default.GetBytes("Welcome to the Server");
        //     stream.Write(welcome_message, 0, welcome_message.Length); // Sends welcome message to Client

        //     while ((received_bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
        //     {
        //         string message = Encoding.Default.GetString(buffer, 0, received_bytes);
        //         Console.WriteLine("Received: " + message);

        //         // This ecodes the messsage back to the client which will be useful for debugging
        //         byte[] response = Encoding.Default.GetBytes("Echo: " + message);
        //         stream.Write(response, 0, response.Length);
        //     }
        // }

        // catch (Exception ex)
        // {
        //     Console.WriteLine("There was an Error when handling the client: " + ex.Message);
        // }

        // finally
        // {
        //     stream.Close();
        //     client.Close();
        //     clients.Remove(client);
        //     Console.WriteLine("Client disconnected.");
        // }




        // byte[] msg_out = new byte[1024]; // All messages must be serialised i.e. converted to byte arra

        // msg_out = Encoding.Default.GetBytes("Welcome");
        // stream.Write(msg_out, 0, msg_out.Length); // This sends the message

        // while (client.Connected)
        // {
        //     byte[] msg_in = new byte[1024];
        //     stream.Read(msg_in, 0, msg_in.Length);
        //     Console.WriteLine(Encoding.Default.GetString(msg_in).Trim(' '));
        // }


   // }