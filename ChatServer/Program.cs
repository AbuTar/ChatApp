// This is the chat server responsible for:
// 1 - Listening to incoming client connections
// 2 - Routing messages between clients

using System.ComponentModel.DataAnnotations;
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


public class TCP_TicTacToe_Server
{
    private TcpListener listener;

    public void Start_Server(int port)
    {
        // Same as before, code that initialises the Server
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine("The Tic Tac Toe Server has Started");
        Console.WriteLine("Waiting for Players");

        Thread accept_thread = new Thread(Accept_Clients);
        accept_thread.Start();
    }

    public void Accept_Clients()
    {
        while (true)
        {
            try
            {
                // Once the users connect to the server, the game can begin
                // Handle the game (and the different users) on a seperate thread

                TcpClient player_1 = listener.AcceptTcpClient();
                Console.WriteLine("Player 1 has connected to the Server.");

                TcpClient player_2 = listener.AcceptTcpClient();
                Console.WriteLine("Player 2 has connected to the Server.");

                Console.WriteLine("Starting...");

                Thread game_thread = new Thread(() => Handle_Game(player_1, player_2));
                game_thread.Start();
            }

            catch (Exception ex)
            {
                Console.WriteLine("An error occured when accepting Players: " + ex.Message);
            }
        }
    }

    private void Handle_Game(TcpClient player_1, TcpClient player_2)
    {
        // Both players require seperate streams to send and receive data
        // Player 1
        NetworkStream player_1_stream = player_1.GetStream();
        StreamReader player_1_reader = new StreamReader(player_1_stream);
        StreamWriter player_1_writer = new StreamWriter(player_1_stream) { AutoFlush = true };

        // Player 2
        NetworkStream player_2_stream = player_2.GetStream();
        StreamReader player_2_reader = new StreamReader(player_2_stream);
        StreamWriter player_2_writer = new StreamWriter(player_2_stream) { AutoFlush = true };

        // Board Logic
        char[] board = "123456789".ToCharArray();
        char symbol_X = 'X';
        char symbol_O = 'O';
        bool game_ended = false;

        player_1_writer.WriteLine($"Welcome Player 1! You are {symbol_X}");
        player_2_writer.WriteLine($"Welcome Player 2! You are {symbol_O}");

        // Now I need to broadcast every move to both Player 1 and Player 2
        // I'll handle this logic in a different method - Broadcast Board
        BroadCast_Board(player_1_writer, player_2_writer, board);

        // If I make player 1 current and I make player 2 the opponent
        // It makes the following easier
        StreamReader current_reader = player_1_reader;
        StreamWriter current_writer = player_1_writer;
        StreamWriter opponent_writer = player_2_writer;
        char current_symbol = symbol_X;


        while (!game_ended)
        {
            current_writer.WriteLine("Your Move (1-9): ");
            opponent_writer.WriteLine("Waiting for the Opponent to Move...");

            string move = current_reader.ReadLine();
            // Incase of a disconnection or error
            // Now to handle the different potential user errors that could occur
            if (move == null)
            {
                current_writer.WriteLine("Your connection with the server has been Lost.");
                opponent_writer.WriteLine("Your opponent has disconnected - you win by default");
                break;
            }

            if (!int.TryParse(move, out int position) || position < 1 || position > 9)
            {
                current_writer.WriteLine("That was an invalid move. Please enter a valid move");
                continue;
            }

            if (board[position - 1] == 'X' || board[position] == 'O')
            {
                current_writer.WriteLine("This spot is already taken - Try again.");
                continue;
            }

            board[position - 1] = current_symbol;
            BroadCast_Board(player_1_writer, player_2_writer, board);

            // Now I need to handle the game-ending logic
            // This involves:
            // Draws
            // Losses

            if (Win_Check(board, current_symbol))
            {
                current_writer.WriteLine("Congratulations, you win!");
                opponent_writer.WriteLine("You lose! Better luck next time.");
                game_ended = true;
                break;
            }

            if (Is_Board_Full(board))
            {
                player_1_writer.WriteLine("The game has ended in a draw!");
                player_2_writer.WriteLine("The game has ended in a draw");
                break;
            }

            // So far this program would only allow player 1 to enter moves
            // To deal with this we need to swap player 1 and player 2 around
            // player 1 --> oppoent
            // player 2 --> current player

            // Swapping Players

            if (current_reader == player_1_reader)
            {
                current_reader = player_2_reader;
                current_writer = player_2_writer;
                opponent_writer = player_1_writer;
                current_symbol = symbol_O;
            }

            else
            {
                current_reader = player_1_reader;
                current_writer = player_1_writer;
                opponent_writer = player_2_writer;
                current_symbol = symbol_X;
            }
        }

        // Once the game is over, both clients need to know and be disconnected
        player_1_writer.WriteLine("Game Over. Now Disconnecting...");
        player_2_writer.WriteLine("Game Over. Now Disconnecting...");
        player_1_stream.Close();
        player_2_stream.Close();
        player_1.Close();
        player_2.Close();

        Console.WriteLine("The Game Session has ended. ");

    }

    private void BroadCast_Board(StreamWriter writer_1, StreamWriter writer_2, char[] board) 
    {
        // Basic function to display the game board to both players
        string[] rows =
         {
             "",
             $" {board[0]} | {board[1]} | {board[2]} ",
             "---+---+---",
             $" {board[3]} | {board[4]} | {board[5]} ",
             "---+---+---",
             $" {board[6]} | {board[7]} | {board[8]} ",
             ""
        };

        foreach (var row in rows)
        {
            writer_1.WriteLine(row);
            writer_2.WriteLine(row);
        }
    }

    private bool Win_Check(char[] board, char symbol)
    {
        int[,] winning_positions = new int[,]
        {
            // Winning Rows
            {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
            // Winning Columns
            {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
            // Winning Diagonals
            { 0, 4, 8}, {2, 4 ,6}
        };

        for (int i = 0; i < winning_positions.GetLength(0); i++)
        {
            if (board[winning_positions[i, 0]] == symbol &&
                board[winning_positions[i, 1]] == symbol &&
                board[winning_positions[i, 2]] == symbol)
            {
                return true;

            }
        }

        return false;

    }

    private bool Is_Board_Full(char[] board)
    {
        foreach (char character in board)
        {
            if (character != 'X' && character != 'O')
            {
                return false;
            }
        }

        return true;
    }


}

// public class TCP_TicTacToe_Server
// {
//     private TcpListener _listener;

//     public void Start_Server(int port)
//     {
//         _listener = new TcpListener(IPAddress.Any, port);
//         _listener.Start();

//         Console.WriteLine("The Tic Tac Toe Server has Started.\nWaiting for players...");

//         Thread accept_thread = new Thread(Accept_Clients);
//         accept_thread.Start();
//     }

//     private void Accept_Clients()
//     {
//         while (true)
//         {
//             try
//             {
//                 TcpClient player1 = _listener.AcceptTcpClient();
//                 Console.WriteLine("Player 1 connected.");

//                 TcpClient player2 = _listener.AcceptTcpClient();
//                 Console.WriteLine("Player 2 connected.");

//                 Thread game_thread = new Thread(() => Handle_Game(player1, player2));
//                 game_thread.Start();
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("Error accepting players: " + ex.Message);
//             }
//         }
//     }

//     private void Handle_Game(TcpClient player1, TcpClient player2)
//     {
//         NetworkStream stream1 = player1.GetStream();
//         StreamReader reader1 = new StreamReader(stream1);
//         StreamWriter writer1 = new StreamWriter(stream1) { AutoFlush = true };

//         NetworkStream stream2 = player2.GetStream();
//         StreamReader reader2 = new StreamReader(stream2);
//         StreamWriter writer2 = new StreamWriter(stream2) { AutoFlush = true };

//         char[] board = "123456789".ToCharArray();
//         char symbol1 = 'X';
//         char symbol2 = 'O';
//         bool gameEnded = false;

//         writer1.WriteLine("Welcome Player 1. You are X.");
//         writer2.WriteLine("Welcome Player 2. You are O.");
//         Broadcast_Board(writer1, writer2, board);

//         StreamReader currentReader = reader1;
//         StreamWriter currentWriter = writer1;
//         StreamWriter opponentWriter = writer2;
//         char currentSymbol = symbol1;

//         while (!gameEnded)
//         {
//             currentWriter.WriteLine("Your move (1-9):");
//             opponentWriter.WriteLine("Waiting for opponent to move...");

//             string move = currentReader.ReadLine();
//             if (move == null)
//             {
//                 currentWriter.WriteLine("Connection lost.");
//                 opponentWriter.WriteLine("Opponent disconnected. You win by default.");
//                 break;
//             }

//             if (!int.TryParse(move, out int position) || position < 1 || position > 9)
//             {
//                 currentWriter.WriteLine("Invalid input. Please enter a number between 1 and 9.");
//                 continue;
//             }

//             if (board[position - 1] == 'X' || board[position - 1] == 'O')
//             {
//                 currentWriter.WriteLine("That spot is already taken. Try again.");
//                 continue;
//             }

//             board[position - 1] = currentSymbol;
//             Broadcast_Board(writer1, writer2, board);

//             if (Check_Win(board, currentSymbol))
//             {
//                 currentWriter.WriteLine("Congratulations, you win!");
//                 opponentWriter.WriteLine("You lose! Better luck next time.");
//                 gameEnded = true;
//                 break;
//             }

//             if (Is_Board_Full(board))
//             {
//                 writer1.WriteLine("Game ended in a draw!");
//                 writer2.WriteLine("Game ended in a draw!");
//                 break;
//             }

//             // Swap turn
//             if (currentReader == reader1)
//             {
//                 currentReader = reader2;
//                 currentWriter = writer2;
//                 opponentWriter = writer1;
//                 currentSymbol = symbol2;
//             }
//             else
//             {
//                 currentReader = reader1;
//                 currentWriter = writer1;
//                 opponentWriter = writer2;
//                 currentSymbol = symbol1;
//             }
//         }

//         writer1.WriteLine("Game over. Disconnecting...");
//         writer2.WriteLine("Game over. Disconnecting...");
//         stream1.Close(); stream2.Close();
//         player1.Close(); player2.Close();

//         Console.WriteLine("A game session has ended.");
//     }

//     private void Broadcast_Board(StreamWriter w1, StreamWriter w2, char[] board)
//     {
//         string[] lines =
//         {
//             "",
//             $" {board[0]} | {board[1]} | {board[2]} ",
//             "---+---+---",
//             $" {board[3]} | {board[4]} | {board[5]} ",
//             "---+---+---",
//             $" {board[6]} | {board[7]} | {board[8]} ",
//             ""
//         };

//         foreach (var line in lines)
//         {
//             w1.WriteLine(line);
//             w2.WriteLine(line);
//         }
//     }

//     private bool Check_Win(char[] board, char symbol)
//     {
//         int[,] winning_positions = new int[,]
//         {
//             { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, // Rows
//             { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, // Columns
//             { 0, 4, 8 }, { 2, 4, 6 }              // Diagonals
//         };

//         for (int i = 0; i < winning_positions.GetLength(0); i++)
//         {
//             if (board[winning_positions[i, 0]] == symbol &&
//                 board[winning_positions[i, 1]] == symbol &&
//                 board[winning_positions[i, 2]] == symbol)
//             {
//                 return true;
//             }
//         }
//         return false;
//     }

//     private bool Is_Board_Full(char[] board)
//     {
//         foreach (char c in board)
//         {
//             if (c != 'X' && c != 'O') return false;
//         }
//         return true;
//     }
// }




class Program
{
    static void Main()
    {
        Console.WriteLine("\nWelcome to the Multi-Service Server");
        Console.WriteLine("1 - Launch Messaging Server");
        Console.WriteLine("2 - Launch File Transfer Server - Coming Soon");
        Console.WriteLine("3 - Play Tic Tac Toe");
        Console.WriteLine("4 - Exit");
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
                    TCP_TicTacToe_Server my_server = new TCP_TicTacToe_Server();
                    my_server.Start_Server(2026);
                    break;


                case "4":
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