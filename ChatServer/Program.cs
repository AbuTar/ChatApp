// This is the chat server responsible for:
// 1 - Listening to incoming client connections
// 2 - Routing messages between clients

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

public class TCP_Messaging_Server
{
    private TcpListener listener;
    // This lists helps keep track of the list of the connected clients 
    private List<TcpClient> clients = new List<TcpClient>();
    // I also need a dictionary to associate a client object with their name
    private Dictionary<TcpClient, string> client_names = new Dictionary<TcpClient, string>();

    // I need to add a way of reusing the same writer and not creating a new instance everytime a message is sent
    private Dictionary<TcpClient, StreamWriter> client_writers = new Dictionary<TcpClient, StreamWriter>();

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
        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
        client_writers[client] = writer;

        // Welcome Message
        // writer.WriteLine("Welcome to the server!");
        string client_name = reader.ReadLine();
        client_names[client] = client_name;

        Console.WriteLine($"{client_name} has connected.");
        writer.WriteLine($"Welcome to the server, {client_name}!");

        // I also want to add a way to know who is currently in the server
        string userList = "Current clients: " + string.Join(", ", client_names.Values);
        writer.WriteLine(userList);

        // This notifies existing users about a new one
        Broadcast_Join_Message(client, client_name);



        try
        {
            while (client.Connected)
            {
                string message = reader.ReadLine();
                if (message == null)
                    break; // Client disconnects if no message is detected

                // Console.WriteLine("Received: " + message);
                Console.WriteLine($"{client_name} says: {message}");

                // Still Echoing the message back to the client for debugging purposes
                // writer.WriteLine("Echo: " + message);

                Broadcast_Message(client, client_name, message);
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"There was an error communcating with the {client_name}");
        }

        finally
        {
            // I need to make sure to close the stream and remove the clients
            stream.Close();
            client.Close();
            clients.Remove(client);
            Console.WriteLine($"{client_name} has disconnected.");
            client_names.Remove(client);
            client_writers.Remove(client);

        }

    }

    private void Broadcast_Message(TcpClient sender, string sender_name, string message)
    {
        // Since I want to send this to every client, I can iterate over clients list
        // To make sure I send the message to all of them

        foreach (TcpClient client in clients)
        {
            try
            {
                StreamWriter client_writer = client_writers[client]; //Use stored writer instead of creating a new one

                if (client == sender)
                {
                    client_writer.WriteLine($"✉️ - You: {message}");
                }

                else
                {
                    client_writer.WriteLine($"✉️ - {sender_name}: {message}");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"An error has occured - Failed to send message to a client: {ex.Message}");
            }

        }
    }

    private void Broadcast_Join_Message(TcpClient new_client, string new_client_name)
    {
        // Broadcast to all clients except the one who just joined
        foreach (TcpClient client in clients)
        {
            if (client == new_client) continue;

            try
            {
                StreamWriter writer = client_writers[client];
                writer.WriteLine($"📢 {new_client_name} has joined the server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occured when broadcasting join message: {ex.Message}");
            }
        }
    }
}          

        
public class TCP_File_Transfer_Server
{
    // 1 - Listening to incoming client connections
    // 2 - Routing files between clients
    private TcpListener listener;
    // List to keep track of connected Clients 
    private List<TcpClient> clients = new List<TcpClient>();
    // List needed to Associate Client Object with Client's Name
    private Dictionary<TcpClient, string> client_names = new Dictionary<TcpClient, string>();
    // I need to add a way of reusing the same writer and not creating a new instance everytime a message is sent
    private Dictionary<TcpClient, StreamWriter> client_writers = new Dictionary<TcpClient, StreamWriter>();
    private Dictionary<TcpClient, BinaryReader> client_readers = new Dictionary<TcpClient, BinaryReader>();
    private Dictionary<TcpClient, BinaryWriter> client_binary_writers = new Dictionary<TcpClient, BinaryWriter>();

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
        NetworkStream stream = client.GetStream();
        BinaryReader reader = new BinaryReader(stream);
        BinaryWriter writer = new BinaryWriter(stream);

        client_readers[client] = reader;
        client_binary_writers[client] = writer;

        string client_name = reader.ReadString();
        clients.Add(client);
        client_names[client] = client_name;

        Console.WriteLine($"{client_name} has connected");

        try
        {
            while (client.Connected)
            {
                string command = reader.ReadString();
                if (command == "send")
                {
                    string filename = reader.ReadString();
                    long file_size = reader.ReadInt64();

                    Directory.CreateDirectory("Temp_File_Store");
                    string temp_path = Path.Combine("Temp_File_Store", Guid.NewGuid() + "_" + filename);

                    using (FileStream file_stream = new FileStream(temp_path, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[4096];
                        long data_read = 0;

                        // FIXED: Read binary data directly from stream, not from BinaryReader
                        while (data_read < file_size)
                        {
                            int data_remaining = (int)Math.Min(buffer.Length, file_size - data_read);
                            int bytes_read = stream.Read(buffer, 0, data_remaining); // Correct raw byte reading
                            if (bytes_read == 0)
                            {
                                break; // Client disconnected mid-transfer
                            }
                            file_stream.Write(buffer, 0, bytes_read);
                            data_read += bytes_read;
                        }
                    }

                    Console.WriteLine($"Received file '{filename}' from {client_name}.");
                    Broadcast_File(client, client_name, filename, file_size, temp_path);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occured with client- {client_name}: {ex.Message}");
        }
        finally
        {
            clients.Remove(client);
            client_names.Remove(client);
            client_readers.Remove(client);
            client_binary_writers.Remove(client);
            client.Close();
            Console.WriteLine($"{client_name} has disconnected");
        }
    }

    private void Broadcast_File(TcpClient sender, string sender_name, string filename, long file_size, string temp_path)
    {
        foreach (TcpClient client in clients)
        {
            if (client == sender)
            {
                continue;
            }

            try
            {
                BinaryWriter writer = client_binary_writers[client];
                BinaryReader reader = client_readers[client];

                // Inform clients about the incoming file
                writer.Write("Incoming File");
                Console.WriteLine($"{sender_name} is sending a file: {filename} of size {file_size} bytes. ");
                Console.WriteLine("Enter 'Yes' to accept");
                Console.WriteLine("Enter anything else to decline");

                string client_response = reader.ReadString().Trim().ToLower();
                // Depending on client choice, they will either get the file or they won't
                if (client_response == "yes")
                {
                    Send_To_Cient(client, filename, file_size, temp_path);
                }

                else
                {
                    writer.Write("File Transfer has been declined");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Failed to ask client about File Transfer: {ex.Message}");
            }
        }
    }

    private void Send_To_Cient(TcpClient client, string filename, long file_size, string temp_path)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            BinaryWriter writer = client_binary_writers[client];

            writer.Write("Incoming File");
            writer.Write(filename);
            writer.Write(file_size);

            using (FileStream file_stream = new FileStream(temp_path, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4096];
                int bytes_read;
                while ((bytes_read = file_stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytes_read);
                }
            }

            Console.WriteLine($"Sent {filename} successfully to the client");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send file to client: {ex.Message}");
        }
    }
}


class Program
{
    static void Main()
    {
        Console.WriteLine("\nWelcome to the Multi-Service Server");
        Console.WriteLine("1 - Launch Messaging Server");
        Console.WriteLine("2 - Launch File Transfer Server");
        Console.WriteLine("3 - Exit");
        Console.WriteLine("Please enter your Choice:  ");
        while (true)
        {

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":

                    TCP_Messaging_Server my_messaging_server = new TCP_Messaging_Server();
                    my_messaging_server.Start_Server(2025); // This port can be anything
                    break;

                case "2":
                    TCP_File_Transfer_Server my_file_transfer_server = new TCP_File_Transfer_Server();
                    my_file_transfer_server.Start_Server(2024);
                    break;

                case "3":
                    Console.WriteLine("Exiting....");
                    return;

                default:
                    Console.WriteLine("Your selection was Invalid");
                    break;

            }
        }
        // TCP_Messaging_Server my_server = new TCP_Messaging_Server();
        // my_server.Start_Server(2025); // This port can be anything

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