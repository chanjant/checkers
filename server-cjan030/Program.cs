using System.Net;
using System.Net.Sockets;
using System.Text;
using server_cjan030;
using Newtonsoft.Json;

namespace Server
{
    public class Program
    {
        private static Socket listener;
        private static Dictionary<string, GameRecord> gameRecord = new Dictionary<string, GameRecord>();
        private static Dictionary<string, object> gameRecordLock = new Dictionary<string, object>();
        private static Dictionary<string, string> registeredUsername = new Dictionary<string, string>();
        private static Dictionary<string, object> registeredUsernameLock = new Dictionary<string, object>();
        private static Queue<string> waitGame = new Queue<string>();
        private static Queue<object> waitGameLock = new Queue<object>();
        
        static void Main(string[] args)
        {
           
            StartServer();
           
        }

        public static void StartServer()
        {

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

            listener.Bind(localEndPoint);
            listener.Listen(10);

            Console.WriteLine($"Listening at {localEndPoint.Address}:{localEndPoint.Port}");

            while(true)
            {
                Socket client = listener.Accept();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        public static void AcceptCallBack(IAsyncResult ar)
        {
            Socket listenerSocket = (Socket)ar.AsyncState;
            Socket clientSocket = listenerSocket.EndAccept(ar);

            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(clientSocket);
        }

        private static void HandleClient(object clientObj)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Socket client = (Socket)clientObj;
            IPEndPoint clientEndPoint = (IPEndPoint)client.RemoteEndPoint;
            string clientInfo = $"{clientEndPoint.Address}:{clientEndPoint.Port}";
            Console.WriteLine($"Connection established with {clientInfo}");
            byte[] buffer = new byte[2048];
            int bytesRead;
            string player = "";

            try
            {
                while ((bytesRead = client.Receive(buffer)) != 0)
                {
                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string[] requestLine = lines[0].Split(' ');
                    string method = requestLine[0];

                    string path = requestLine[1].Trim();

                    if (method == "GET")
                    {
                        string response = "";
                        if (path == "/register")
                        {
                            //TODO: generates a random username for a player
                            string username = GenerateUsername();
                            player = username;

                            // TODO: registers this name
                            lock (registeredUsernameLock)
                            {
                                registeredUsername.Add(username, "");
                            }

                            //TODO: return to the user the registered name
                            string jsonResponse = JsonConvert.SerializeObject(username);

                            response =
                                $"HTTP/1.1 200OK\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: application/json\r\n" +
                                $"Content-Length:{jsonResponse.Length}\r\n\r\n{jsonResponse}";

                            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                            client.Send(responseBytes);
                        }
                        else if (path.StartsWith("/pairme"))
                        {
                            // retrieve username
                            int playerIndex = path.IndexOf("player=");

                            if (playerIndex != -1)
                            {
                                string username = path.Substring(playerIndex + "player=".Length);

                                if (registeredUsername.ContainsKey(username))
                                {
                                    string jsonResponse = "";
                                    string gameId = registeredUsername[username];

                                    if (gameId != "")
                                    {
                                        // TODO: allow players to check status
                                        GameRecord playerGame = gameRecord[gameId];
                                        jsonResponse = JsonConvert.SerializeObject(playerGame);
                                    }
                                    else
                                    {
                                        bool createNewGame = false;
                                        GameRecord joinGame;
                                        if (waitGame.Any())
                                        {
                                            // TODO: pair with another player
                                            string joingGameId;
                                            lock (waitGameLock)
                                            {
                                                joingGameId = waitGame.Dequeue();

                                            }

                                            if (gameRecord.ContainsKey(joingGameId))
                                            {
                                                if (!gameRecordLock.ContainsKey(joingGameId))
                                                {
                                                    gameRecordLock[joingGameId] = new object();
                                                }
                                                lock (gameRecordLock[joingGameId])
                                                {
                                                    joinGame = gameRecord[joingGameId];
                                                    registeredUsername[username] = joinGame.GameId;
                                                    joinGame.SecondPlayer = username;
                                                    joinGame.GameState = "progress";
                                                }

                                                jsonResponse = JsonConvert.SerializeObject(joinGame);
                                            }
                                            else
                                            {
                                                createNewGame = true;
                                            }

                                        }
                                        else
                                        {
                                            createNewGame = true;
                                        }
                                        if (createNewGame)
                                        {
                                            // TODO: create new game
                                            string newGameId = Guid.NewGuid().ToString();
                                            if (!registeredUsernameLock.ContainsKey(username))
                                            {
                                                registeredUsernameLock[username] = new object();
                                            }
                                            lock (registeredUsernameLock[username])
                                            {
                                                registeredUsername[username] = newGameId;
                                            }
                                            GameRecord newGame = new GameRecord();
                                            newGame.GameId = newGameId;
                                            newGame.FirstPlayer = username;
                                            newGame.GameState = "wait";
                                            gameRecord.Add(newGameId, newGame);
                                            waitGame.Enqueue(newGameId);
                                            jsonResponse = JsonConvert.SerializeObject(newGame);
                                        }
                                    }
                                    // TODO: return game record
                                    response =
                                    $"HTTP/1.1 200OK\r\nConnection: keep-alive\r\n" +
                                    $"Access-Control-Allow-Origin: *\r\n" +
                                    $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                    $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                    $"Content-Type: application/json\r\n" +
                                    $"Content-Length:{jsonResponse.Length}\r\n\r\n{jsonResponse}";
                                }
                                else
                                {
                                    // username not registered
                                    string message = "Not OK";
                                    string jsonResponse = JsonConvert.SerializeObject(message);
                                    response =
                                    $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                    $"Access-Control-Allow-Origin: *\r\n" +
                                    $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                    $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                    $"Content-Type: application/json\r\n" +
                                    $"Content-Length:{jsonResponse.Length}\r\n\r\n{jsonResponse}";
                                }
                            } else
                            {
                                string message = "Invalid request";
                                string jsonResponse = JsonConvert.SerializeObject(message);
                                response =
                                $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: application/json\r\n" +
                                $"Content-Length:{jsonResponse.Length}\r\n\r\n{jsonResponse}";
                            }
                            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                            client.Send(responseBytes);
                        }
                        else if (path.StartsWith("/mymove"))
                        {
                            int playerIndex = path.IndexOf("player=");
                            int gameIndex = path.IndexOf("&id=");
                            int moveIndex = path.IndexOf("&move=");

                            if (playerIndex != -1 && gameIndex != -1 && moveIndex != -1)
                            {
                                string username = path.Substring(playerIndex + "player=".Length, gameIndex - (playerIndex + "player=".Length));
                                string gameId = path.Substring(gameIndex + "&id=".Length, moveIndex - (gameIndex + "&id=".Length));
                                string lastMove = path.Substring(moveIndex + "&move=".Length);
                                if (gameRecord.ContainsKey(gameId))
                                {
                                    if (!gameRecordLock.ContainsKey(gameId))
                                    {
                                        gameRecordLock[gameId] = new object();
                                    }
                                    lock (gameRecordLock[gameId])
                                    {
                                        GameRecord playerGame = gameRecord[gameId];
                                        // TODO: update the supplied move;
                                        if (playerGame.FirstPlayer == username)
                                        {
                                            playerGame.FirstPlayerLastMove = lastMove;
                                        }
                                        else
                                        {
                                            playerGame.SecondPlayerLastMove = lastMove;
                                        }
                                    }
                                    string message = "OK";
                                    response =
                               $"HTTP/1.1 200OK\r\nConnection: keep-alive\r\n" +
                               $"Access-Control-Allow-Origin: *\r\n" +
                               $"Access-Control-Allow-Methods: GET, POST\r\n" +
                               $"Access-Control-Allow-Headers: Content-Type\r\n" +
                               $"Content-Type: text/plain\r\n" +
                               $"Content-Length:{message.Length}\r\n\r\n{message}";

                                }
                                else
                                {
                                    // game id not found;
                                    string message = "Not OK";
                                    response =
                                $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length:{message.Length}\r\n\r\n{message}";

                                }
                                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                                client.Send(responseBytes);
                            }
                            else
                            {
                                string message = "Invalid request";
                                string jsonResponse = JsonConvert.SerializeObject(message);
                                response =
                                $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length:{message.Length}\r\n\r\n{message}";
                                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                                client.Send(responseBytes);
                            }
                        }
                        else if (path.StartsWith("/theirmove"))
                        {
                            int playerIndex = path.IndexOf("player=");
                            int gameIndex = path.IndexOf("&id=");
                            if (playerIndex != -1 && gameIndex != -1)
                            {
                                string username = path.Substring(playerIndex + "player=".Length, gameIndex - (playerIndex + "player=".Length));
                                string gameId = path.Substring(gameIndex + "&id=".Length);
                                string message = "Not OK";
                                if (gameRecord.ContainsKey(gameId))
                                {

                                    GameRecord playerGame = gameRecord[gameId];
                                    if (registeredUsername.ContainsKey(playerGame.FirstPlayer) && registeredUsername.ContainsKey(playerGame.SecondPlayer))
                                    {
                                        string opponentLastMove = playerGame.FirstPlayer == username ? playerGame.SecondPlayerLastMove : playerGame.FirstPlayerLastMove;

                                        // TODO: return last move of another player
                                        string jsonResponse = JsonConvert.SerializeObject(opponentLastMove);
                                        response =
                                        $"HTTP/1.1 200OK\r\nConnection: keep-alive\r\n" +
                                        $"Access-Control-Allow-Origin: *\r\n" +
                                        $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                        $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                        $"Content-Type: application/json\r\n" +
                                        $"Content-Length:{jsonResponse.Length}\r\n\r\n{jsonResponse}";
                                    }
                                    else
                                    {
                                        message = "Disconnected";
                                    }


                                }
                                else
                                {
                                    // game id not found;
                                    response =
                                    $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                    $"Access-Control-Allow-Origin: *\r\n" +
                                    $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                    $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                    $"Content-Type: text/plain\r\n" +
                                    $"Content-Length:{message.Length}\r\n\r\n{message}";
                                }


                            }
                            else
                            {
                                string message = "Invalid request";
                                response = 
                                $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length:{message.Length}\r\n\r\n{message}";
                             
                            }
                            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                            client.Send(responseBytes);
                        }
                        else if (path.StartsWith("/quit"))
                        {
                            int playerIndex = path.IndexOf("player=");
                            int gameIndex = path.IndexOf("&id=");
                            if (playerIndex != -1 && gameIndex != -1)
                            {
                                string username = path.Substring(playerIndex + "player=".Length, gameIndex - (playerIndex + "player=".Length));
                                string gameId = path.Substring(gameIndex + "&id=".Length);
                                // TODO: remove all record

                                if (gameRecord.ContainsKey(gameId))
                                {
                                    GameRecord quitGame = gameRecord[gameId];
                                    lock (registeredUsernameLock)
                                    {
                                        registeredUsername.Remove(quitGame.FirstPlayer);
                                        if (quitGame.SecondPlayer != null)
                                        {
                                            registeredUsername.Remove(quitGame.SecondPlayer);
                                        }
                                    }
                                    lock (gameRecordLock)
                                    {
                                        gameRecord.Remove(gameId);
                                    }
                                }
                                string message = "OK";
                                response =
                                $"HTTP/1.1 200OK\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length:{message.Length}\r\n\r\n{message}";

                                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                                client.Send(responseBytes);
                                Console.WriteLine($"Thread {threadId} closing connection with {clientInfo} and terminating");
                                break;
                            }
                            else
                            {
                                string message = "Invalid request";
                                response =
                                $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                $"Access-Control-Allow-Origin: *\r\n" +
                                $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                $"Content-Type: text/plain\r\n" +
                                $"Content-Length:{message.Length}\r\n\r\n{message}";
                                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                                client.Send(responseBytes);
                            }
                        } else
                        {
                            string message = $"{path} not exist!";
                            response =
                                    $"HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n" +
                                    $"Access-Control-Allow-Origin: *\r\n" +
                                    $"Access-Control-Allow-Methods: GET, POST\r\n" +
                                    $"Access-Control-Allow-Headers: Content-Type\r\n" +
                                    $"Content-Type: text/plain\r\n" +
                                    $"Content-Length:{message.Length}\r\n\r\n{message}";
                            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                            client.Send(responseBytes);
                        }

                    }

                    Console.WriteLine($"Thread {threadId} sent response to {clientInfo} for {path}");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {

                if (registeredUsername.ContainsKey(player) && gameRecord.ContainsKey(registeredUsername[player]))
                {
                    string gameId = registeredUsername[player];
                    GameRecord quitGame = gameRecord[registeredUsername[player]];
                    lock (registeredUsernameLock)
                    {
                        registeredUsername.Remove(quitGame.FirstPlayer);
                        if (quitGame.SecondPlayer != null)
                        {
                            registeredUsername.Remove(quitGame.SecondPlayer);
                        }
                    }
                    lock (gameRecordLock)
                    {
                        gameRecord.Remove(gameId);
                    }
                }
                Console.WriteLine($"Thread {threadId} closing connection with {clientInfo} and terminating");
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        private static string GenerateUsername()
        {
            string[] usernames = { "pinkPanda", "khakiKoala", "charcoalCat", "fluffyFirefly", "lilacLion", "redRhino", "hilariousHusky", "silverShark", "dandelionDolphin", "emeraldElephant", "greenGrasshopper", "giantGorilla" };
            

            for(int i = 0; i < usernames.Length; i++)
            {
                if (!registeredUsername.ContainsKey(usernames[i]))
                {
                    return usernames[i];
                }
            }

            Random r = new Random();
            int randomNum = r.Next(0, usernames.Length);
            string newUsername = usernames[randomNum];
            int count = 0;
            while (registeredUsername.ContainsKey(newUsername))
            {
                
                newUsername = usernames[randomNum] + count;
                count++;
            }
            return newUsername;
        }
    }
}