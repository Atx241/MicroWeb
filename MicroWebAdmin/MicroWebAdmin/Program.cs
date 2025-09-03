namespace MicroWebAdmin
{
    internal class Program
    {
        static string website = "";
        static string key = "";
        static void Main()
        {
            bool websiteValid = false;
            while (!websiteValid)
            {
                Console.Write("Enter web server location (including protocol): ");
                website = Console.ReadLine() ?? "";
                websiteValid = HTTP.InitializeHTTP(website);
                if (!websiteValid)
                {
                    Console.WriteLine("Web server does not exist");
                }
            }
            bool tokenValid = false;
            while (!tokenValid)
            {
                Console.Write("Enter auth key: ");
                var input = Console.ReadKey(true);
                while (input.Key != ConsoleKey.Enter)
                {
                    if (input.Key == ConsoleKey.Backspace)
                    {
                        if (key.Length > 0)
                        {
                            key = key.Remove(key.Length - 1, 1);
                            Console.Write("\b \b");
                        }
                    }
                    else
                    {
                        Console.Write('∙');
                        key += input.KeyChar;
                    }
                    input = Console.ReadKey(true);
                }
                Console.WriteLine("\nTesting auth key...");
                tokenValid = HTTP.TestKey(key);
                if (!tokenValid)
                {
                    Console.WriteLine("Invalid key");
                    key = "";
                }
            }
            Console.WriteLine("Logging in was successful\n");
            MainLoop();
            Console.WriteLine("Exiting admin console...");
        }
        static void MainLoop()
        {
            while (true)
            {
                Console.WriteLine("What do you want to do?\n1: Create/Modify a file\n2: Delete a file\nAny other key: Exit");
                string? option = Console.ReadLine();
                if (option == "1")
                {
                    Console.Write("Enter web server file location: ");
                    var nPath = Console.ReadLine() ?? "";
                    Console.Write("Enter local file path: ");
                    var lPath = Console.ReadLine() ?? "";
                    if (File.Exists(lPath))
                    {
                        if (HTTP.SendPostReq("/api/fc/" + key + "/" + nPath, File.ReadAllBytes(lPath)).Code == 200)
                        {
                            Console.WriteLine("Successfully wrote to file");
                        } else
                        {
                            Console.WriteLine("Could not write to file");
                        }
                    } else
                    {
                        Console.WriteLine("Local file path was invalid");
                    }
                } 
                else if (option == "2")
                {
                    Console.Write("Enter web server file location: ");
                    var nPath = Console.ReadLine() ?? "";
                    if (HTTP.SendDeleteReq("/api/fc/" + key + "/" + nPath).Code == 200)
                    {
                        Console.WriteLine("Successfully deleted file");
                    }
                    else
                    {
                        Console.WriteLine("Could not delete file. Does it exist?");
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
