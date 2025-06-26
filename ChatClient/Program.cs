// This program is responsible for handling any client logic
// 1 - Connecting to the server
// 2 - Sending user input to the server
// 3 - Receiving and displaying messages from the server
using System;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;

// This class is reponsible for handling the client logic for sending and reading messages to and from the Server
public class Messaging_Client
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

// In order to extend the usability of my application, I have decided to incorporate the ability to send files to and from a server as well, though this would be seperate
public class File_Transfer_Client
{
    private TcpClient client;
    private NetworkStream stream;
    private BinaryReader reader;
    private BinaryWriter writer;
    private string client_name;

    public void Connect_To_Server(string IPAddress, int port)
    {
        client = new TcpClient(IPAddress, port);
        stream = client.GetStream();
        reader = new BinaryReader(stream);
        writer = new BinaryWriter(stream);

        // Responsible for sending the Client's Name to the Server
        Console.WriteLine("Please Enter your name: ");
        client_name = Console.ReadLine();
        writer.Write(client_name);

        // Make user aware of their Choices
        Console.WriteLine("You have connected to the File Server");
        Console.WriteLine("Commands: ");
        Console.WriteLine("send - Select a File to send");
        Console.WriteLine("exit - Disconnect from the Server");

        Thread receive_thread = new Thread(Receive_Loop);

        while (true)
        {
            string user_choice = Console.ReadLine().Trim().ToLower();

            if (user_choice == "send")
            {
                string file_path = File_Selector();

                if (string.IsNullOrEmpty(file_path) || !File.Exists(file_path))
                {
                    Console.WriteLine("The selected file does not exist");
                }

                else
                {
                    Send_File(file_path);
                }
            }

            else if (user_choice == "exit")
            {
                Disconnect();
            }

            else
            {
                Console.WriteLine("You have entered an Invalid Command");
            }
        }

    }

    private void Send_File(string file_path)
    {
        try
        {
            string file_name = Path.GetFileName(file_path);
            long file_size = new FileInfo(file_path).Length;

            writer.Write("send"); // We need to notify server about file transfer
            writer.Write(file_name);  // Send filename
            writer.Write(file_size);  // Send file size

            using (FileStream file_stream = new FileStream(file_path, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4096];
                int bytes_read;

                while ((bytes_read = file_stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytes_read);
                }
            }

            Console.WriteLine($"Successfully sent the file: {file_name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending file: " + ex.Message);
        }

    }

    private string File_Selector()
    {
        Console.WriteLine("Please enter the full path to the file you wish to send: ");
        return Console.ReadLine().Trim();
    }

    // private string File_Selector()
    // {
    //     using (OpenFileDialog dialog = new OpenFileDialog())
    //     {
    //         dialog.Title = "Select a file to send";
    //         dialog.Filter = "All files (*.*)|*.*";
    //         dialog.Multiselect = false;

    //         if (dialog.ShowDialog() == DialogResult.OK)
    //         {
    //             return dialog.FileName;
    //         }
    //     }
    //     return null;
    // }

    private void Receive_Loop()
    {
        // Client should be able to receive file whenever
        try
        {
            while (true)
            {
                string command = reader.ReadString();

                if (command == "Incoming File")
                {
                    Console.WriteLine("A file is being offered to you. Do you want to accept it? (yes/no)");
                    string response = Console.ReadLine()?.Trim().ToLower();
                    writer.Write(response);

                    if (response == "yes")
                    {
                        string filename = reader.ReadString();
                        long file_size = reader.ReadInt64();

                        string save_path = Path.Combine("Downloads", filename);
                        Directory.CreateDirectory("Downloads");

                        using (FileStream filestream = new FileStream(save_path, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[4096];
                            long data_read = 0;

                            while (data_read < file_size)
                            {
                                int data_remaining = (int)Math.Min(buffer.Length, file_size - data_read);
                                int bytes_read = stream.Read(buffer, 0, data_remaining);
                                if (bytes_read == 0)
                                {
                                    break;
                                }

                                filestream.Write(buffer, 0, bytes_read);
                                data_read += bytes_read;
                            }
                        }

                        Console.WriteLine($"File received and saved as: {save_path}");
                    }
                    else
                    {
                        Console.WriteLine("File declined.");
                    }
                }
                else
                {
                    // Console.WriteLine("Server message: " + command);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Disconnected from server: " + ex.Message);
        }
    }

    private void Disconnect()
    {
        stream.Close();
        client.Close();
        Console.WriteLine("You have disconnected from the Server");
        Environment.Exit(0);
    }


}

public class TCP_TicTacToe_Client
{
    private TcpClient client;
    private StreamReader client_reader;
    private StreamWriter client_writer;
    private NetworkStream client_stream;
    private bool is_running = true;

    public void Connect_To_Server(string server_ip, int server_port)
    {
        try
        {
            client = new TcpClient(server_ip, server_port);
            client_stream = client.GetStream();
            client_reader = new StreamReader(client_stream);
            client_writer = new StreamWriter(client_stream) { AutoFlush = true };

            Console.WriteLine("You have connected successfully to the Server");

            Thread receive_thread = new Thread(Receive_Messages);
            receive_thread.Start();

            while (is_running)
            {
                string user_input = Console.ReadLine();

                if (user_input.Trim().ToLower() == "exit")
                {
                    Console.WriteLine("Disconnecting from the Tic Tac Toe Server");
                    Disconnect();
                    break;
                }

                if (!string.IsNullOrWhiteSpace(user_input))
                {
                    client_writer.WriteLine(user_input);
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine("You have failed to connect or lost connection to the Server");
            Disconnect();
        }
    }

    private void Receive_Messages()
    {
        try
        {
            string server_message;

            while ((server_message = client_reader.ReadLine()) != null)
            {
                Console.WriteLine(server_message);

                if (server_message.ToLower().Contains("your move"))
                {
                    Console.Write("Enter your move (1-9): ");
                }

                if (server_message.ToLower().Contains("game over") ||
                    server_message.ToLower().Contains("you win") ||
                    server_message.ToLower().Contains("draw") ||
                    server_message.ToLower().Contains("server wins"))
                {
                    is_running = false;
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine("You have beend disconnected from the Server: " + ex.Message);
        }

        finally
        {
            Disconnect();
        }
    }

    private void Disconnect()
    {
        client_stream.Close();
        client_reader.Close();
        client_writer.Close();
        client.Close();
        Environment.Exit(0);
    }
}

// public class TCP_TicTacToe_Client
// {
//     private TcpClient client;
//     private StreamReader client_reader;
//     private StreamWriter client_writer;
//     private NetworkStream client_stream;
//     private bool is_running = true;

//     public void Connect_To_Server(string server_ip, int server_port)
//     {
//         try
//         {
//             client = new TcpClient(server_ip, server_port);
//             client_stream = client.GetStream();
//             client_reader = new StreamReader(client_stream);
//             client_writer = new StreamWriter(client_stream) { AutoFlush = true };

//             Console.WriteLine("You have connected successfully to the Server");

//             Thread receive_thread = new Thread(Receive_Messages);
//             receive_thread.Start();

//             while (is_running)
//             {
//                 string user_input = Console.ReadLine();

//                 if (user_input.Trim().ToLower() == "exit")
//                 {
//                     Console.WriteLine("Disconnecting from the Tic Tac Toe Server");
//                     Disconnect();
//                     break;
//                 }

//                 if (!string.IsNullOrWhiteSpace(user_input))
//                 {
//                     client_writer.WriteLine(user_input);
//                 }
//             }
//         }

//         catch (Exception ex)
//         {
//             Console.WriteLine("You have failed to connect or lost connection to the Server");
//             Disconnect();
//         }
//     }

//     private void Receive_Messages()
//     {
//         try
//         {
//             string server_message;

//             while ((server_message = client_reader.ReadLine()) != null)
//             {
//                 Console.WriteLine(server_message);

//                 if (server_message.ToLower().Contains("your move"))
//                 {
//                     Console.Write("Enter your move (1-9): ");
//                 }

//                 if (server_message.ToLower().Contains("game over") ||
//                     server_message.ToLower().Contains("you win") ||
//                     server_message.ToLower().Contains("draw") ||
//                     server_message.ToLower().Contains("you lose") ||
//                     server_message.ToLower().Contains("opponent disconnected"))
//                 {
//                     is_running = false;
//                 }
//             }
//         }

//         catch (Exception ex)
//         {
//             Console.WriteLine("You have been disconnected from the Server: " + ex.Message);
//         }

//         finally
//         {
//             Disconnect();
//         }
//     }

//     private void Disconnect()
//     {
//         client_stream?.Close();
//         client_reader?.Close();
//         client_writer?.Close();
//         client?.Close();
//         Environment.Exit(0);
//     }
// }



class Program
{
    static void Main()
    {
        // Implemented a Menu to allow Clients to decide which Server they want to connect to
        while (true)
        {
            Console.WriteLine("\nWelcome to the Multi-Service Client");
            Console.WriteLine("1 - Connect to Messaging Server");
            Console.WriteLine("2 - Connect to File Transfer Server");
            Console.WriteLine("3 - Connect to Tic Tac Toe Server");
            Console.WriteLine("4 - Exit");
            Console.WriteLine("Please enter your Choice:  ");
            string user_choice = Console.ReadLine();

            switch (user_choice)
            {
                case "1":

                    Messaging_Client message_client = new Messaging_Client();
                    message_client.Connect_To_Server("127.0.0.1", 2025);
                    break;

                case "2":
                    File_Transfer_Client file_client = new File_Transfer_Client();
                    file_client.Connect_To_Server("127.0.0.1", 2024);
                    break;

                case "3":
                    TCP_TicTacToe_Client client = new TCP_TicTacToe_Client();
                    client.Connect_To_Server("127.0.0.1", 2026);
                    break;

                case "4":
                    Console.WriteLine("Exiting....");
                    return;

                default:
                    Console.WriteLine("Your selection was Invalid");
                    break;

            }
        }


        // Messaging_Client client = new Messaging_Client();
        // client.Connect_To_Server("127.0.0.1", 2025);

    }
}   

