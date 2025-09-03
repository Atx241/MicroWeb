namespace MicroWeb
{
    public class Program
    {
        static bool loadAllIntoMemory;
        static Dictionary<string,string> memFiles = [];
        static string[] authKeys = [];
        static string? root = null;
        static string? rootFile = null;
        static string? authKeysFile = null;
        static char sep = Path.DirectorySeparatorChar;
        //Version
        const int majorVersion = 0;
        const int minorVersion = 0;
        const int incrementalVersion = 0;
        struct BasicResult : IResult
        {
            public string Data;
            public int Code;
            public static BasicResult Fail = new BasicResult("", 404);
            public BasicResult(string Data, int Code)
            {
                this.Data = Data;
                this.Code = Code;
            }
            public async Task ExecuteAsync(HttpContext httpContext)
            {
                httpContext.Response.StatusCode = Code;
                await httpContext.Response.WriteAsync(Data);
            }
        }
        public static void Main(string[] args)
        {
            Console.WriteLine($"Version {majorVersion}.{minorVersion}.{incrementalVersion}");
            //User input
            List<string> customMemFiles = new List<string>();
            if (args.Length == 0)
            {
                Console.WriteLine("Load all files into memory? (y/n)");
                loadAllIntoMemory = Console.ReadLine() == "y";
                if (!loadAllIntoMemory)
                {
                    Console.WriteLine("Enter files to load into memory (leave blank to finish)");
                    string? file = "";
                    while (true)
                    {
                        file = Console.ReadLine();
                        if (file == null || file == "")
                        {
                            break;
                        }
                        customMemFiles.Add(file);
                    }
                }
                Console.WriteLine("Enter root folder");
                root = Console.ReadLine();
                Console.WriteLine("Enter root file (leave blank to skip)");
                rootFile = Console.ReadLine();
                if (rootFile == "")
                {
                    rootFile = null;
                }
                Console.WriteLine("Enter authentication keys file (leave blank to automatically detect)");
                authKeysFile = Console.ReadLine();
                if (authKeysFile == "")
                {
                    authKeysFile = null;
                }
            }
            else
            {
                foreach (string arg in args)
                {
                    var splitArg = arg.Split(':');
                    if (arg.Length > 1)
                    {
                        if (splitArg[0] == "root")
                        {
                            root = splitArg[1];
                        }
                        if (splitArg[0] == "memfiles")
                        {
                            var files = splitArg[1].Split(",");
                            foreach (var file in files)
                            {
                                customMemFiles.Add(file);
                            }
                        }
                        if (splitArg[0] == "rootfile")
                        {
                            rootFile = splitArg[1];
                        }
                        if (splitArg[0] == "authkeysfile")
                        {
                            authKeysFile = splitArg[1];
                        }
                    }
                    else
                    {
                        if (arg == "loadallintomemory")
                        {
                            loadAllIntoMemory = true;
                        }
                    }
                }
            }
            if (authKeysFile == null)
            {
                if (File.Exists(".auth"))
                {
                    authKeysFile = ".auth";
                } else
                {
                    Console.WriteLine("WARNING: Authentication keys file was not specified, and could not automatically be found");
                }
            }
            if (File.Exists(authKeysFile))
            {
                StreamReader reader = new StreamReader(authKeysFile);
                List<string> keys = new List<string>();
                while (!reader.EndOfStream)
                {
                    var key = reader.ReadLine();
                    if (key != null) {
                        keys.Add(key);
                    }
                }
                authKeys = keys.ToArray();
            } else if (authKeysFile != null)
            {
                Err("Specified authentication keys file could not be found");
            }
            Assert(root != null, "Root folder was not specified");
            Assert(Directory.Exists(root) && !root.EndsWith(sep), "The root folder is invalid. Ensure that the directory exists, and does not end with a delimiter");
            Assert(!Directory.Exists(root + sep + "api"), "The directory {root folder}/api cannot exist");
            //Loading files into memory
            if (loadAllIntoMemory)
            {
                foreach (var file in Directory.EnumerateFiles(root!))
                {
                    memFiles.Add(GetRelativeFilePath(file,root!, $"File {file} is not relative to the root folder"), File.ReadAllText(file));
                    Console.WriteLine($"{file} was successfully added to memory");
                }
            } else
            {
                foreach (var file in customMemFiles)
                {
                    memFiles.Add(file, File.ReadAllText(root! + sep + file));
                    Console.WriteLine($"{file} was successfully added to memory");
                }
            }
            BuildWeb();
        }
        static void Err(string message)
        {
            Console.WriteLine(message);
            Environment.Exit(-1);
        }
        static void Assert(bool condition, string message) {
            if (!condition)
            {
                Err(message);
            }
        }
        static BasicResult GetFileData(string filePath)
        {
            if (filePath == ".root")
            {
                return BasicResult.Fail;
            }
            if (memFiles.ContainsKey(filePath))
            {
                return new BasicResult(memFiles[filePath],200);
            }
            else
            {
                var path = root + sep + filePath;
                if (File.Exists(path)) {
                    return new BasicResult(File.ReadAllText(path),200);
                }
            }
            return BasicResult.Fail;
        }
        static string GetRelativeFilePath(string file, string parent, string failMessage = "Failed to create relative directory")
        {
            Assert(file.StartsWith(parent), failMessage);
            return file.Substring(parent.Length + 1);
        }
        public static async Task<byte[]> BinaryReadAllBytes(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                byte[] buf = new byte[1];
                int read = await stream.ReadAsync(buf);
                if (read == 0)
                {
                    break;
                } else
                {
                    bytes.Add(buf[0]);
                }
            }
            return bytes.ToArray();
        }
        static void BuildWeb()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            if (rootFile != null)
            {
                app.MapGet("/", () => GetFileData(rootFile));
            }
            app.MapGet("/{file}", (string file) => GetFileData(file));
            app.MapGet("/api/testauth/{authToken}", (string authToken) => {
                return authKeys.Contains(authToken) ? new BasicResult("Success", 200) : new BasicResult("Fail", 401);
            });
            app.MapPost("/api/fc/{authToken}/{file}", async (HttpRequest req) =>
            {
                var authToken = req.RouteValues["authToken"];
                var file = req.RouteValues["file"];
                byte[] body = await BinaryReadAllBytes(req.Body);
                if (authKeys.Contains(authToken))
                {
                    var path = root + sep + file;
                    File.WriteAllBytes(path, body);
                    return new BasicResult("Successfully wrote to file",200);
                } else
                {
                    return new BasicResult("", 401);
                }
            });
            app.Run();
        }
    }
}
