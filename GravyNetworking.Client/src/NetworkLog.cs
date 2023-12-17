namespace GravyNetworking.Client
{
    public delegate void NetworkLogEvent(object message);

    public static class NetworkLog
    {
        public static event NetworkLogEvent Log;

        public static void WriteLine(object message)
        {
            if(Log != null)
            {
                Log(message);
            }
            else
            {
                System.Console.ForegroundColor = System.ConsoleColor.DarkGreen;
                System.Console.Write(System.DateTime.Now + " ");
                System.Console.ForegroundColor = System.ConsoleColor.White;
                System.Console.WriteLine(message);
            }
        }
    }
}