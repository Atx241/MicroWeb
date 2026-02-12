using System.Net;
using System.Text.Json.Nodes;
namespace MicroWeb_Refactored
{
    public class Program
    {
        static bool loadAllIntoMemory;
        static Dictionary<string, byte[]> memFiles = [];
        static string[] authKeys = [];
        static Dictionary<string, string[]> authKeyAllowedFiles = [];
        static string? root = null;
        static string? rootFile = null;
        static string? authKeysFile = null;
        static char sep = Path.DirectorySeparatorChar;
        static string httpPort = "80";
        static string httpsPort = "443";
        static byte[] fileNotFoundData = (byte[])[];
        //Version
        const int majorVersion = 1;
        const int minorVersion = 0;
        const int incrementalVersion = 1;
        const bool localDebug = false;
        public struct BasicResult
        {
            public byte[] Data;
            public int Code;
            public static BasicResult Fail = new BasicResult(fileNotFoundData, 404);
            public BasicResult(byte[] Data, int Code)
            {
                this.Data = Data;
                this.Code = Code;
            }
            public BasicResult(string Data, int Code)
            {
                this.Data = S2B(Data);
                this.Code = Code;
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
                Console.WriteLine("Enter HTTP port (leave blank for default)");
                httpPort = Console.ReadLine() ?? httpPort;
                if (httpPort == "")
                {
                    httpPort = "80";
                }
                Console.WriteLine("Enter HTTPS port (leave blank for default)");
                httpsPort = Console.ReadLine() ?? httpsPort;
                if (httpsPort == "")
                {
                    httpsPort = "443";
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
                }
                else
                {
                    Console.WriteLine("WARNING: Authentication keys file was not specified, and could not automatically be found");
                }
            }
            if (File.Exists(authKeysFile))
            {
                StreamReader reader = new StreamReader(authKeysFile);
                List<string> keys = new List<string>();
                int restrictedKeys = 0;
                while (!reader.EndOfStream)
                {
                    var entry = reader.ReadLine()?.Split(',');
                    if (entry != null)
                    {
                        var key = entry[0];
                        if (key.Length < 10)
                        {
                            Err("An authentication key was shorter than 10 characters, and will therefore not be allowed");
                        }
                        keys.Add(key);
                        if (entry.Length > 1)
                        {
                            restrictedKeys++;
                            authKeyAllowedFiles.Add(key, entry[1..]);
                        }
                    }
                }
                authKeys = keys.ToArray();
                Console.WriteLine("Added " + authKeys.Length + " auth keys, with " + restrictedKeys + " containing access restrictions");
            }
            else if (authKeysFile != null)
            {
                Err("Specified authentication keys file could not be found");
            }
            if (File.Exists(root + "/404"))
            {
                fileNotFoundData = File.ReadAllBytes(root + "/404");
            }
            Assert(root != null, "Root folder was not specified");
            Assert(Directory.Exists(root) && !root.EndsWith(sep), "The root folder is invalid. Ensure that the directory exists, and does not end with a delimiter");
            Assert(!Directory.Exists(root + sep + "api"), "The directory {root folder}/api cannot exist");
            //Loading files into memory
            if (loadAllIntoMemory)
            {
                foreach (var file in Directory.EnumerateFiles(root!))
                {
                    memFiles.Add(GetRelativeFilePath(file, root!, $"File {file} is not relative to the root folder"), File.ReadAllBytes(file));
                    Console.WriteLine($"{file} was successfully added to memory");
                }
            }
            else
            {
                foreach (var file in customMemFiles)
                {
                    memFiles.Add(file, File.ReadAllBytes(root! + sep + file));
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
        static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Err(message);
            }
        }
        static byte[] S2B(string str)
        {
            return System.Text.Encoding.ASCII.GetBytes(str);
        }
        static BasicResult GetFileData(string filePath)
        {
            if (filePath == ".root")
            {
                return BasicResult.Fail;
            }
            if (memFiles.ContainsKey(filePath))
            {
                return new BasicResult(memFiles[filePath], 200);
            }
            else
            {
                var path = root + sep + filePath;
                if (File.Exists(path))
                {
                    Console.WriteLine("Retrieving data from file at: " + filePath);
                    return new BasicResult(File.ReadAllBytes(path), 200);
                }
            }
            return BasicResult.Fail;
        }
        static string GetRelativeFilePath(string file, string parent, string failMessage = "Failed to create relative directory")
        {
            Assert(file.StartsWith(parent), failMessage);
            return file.Substring(parent.Length + 1);
        }
        static void BuildWeb()
        {
            HttpListener listener = new();
            //Ignores "unreachable code detected" warning
#pragma warning disable
            if (localDebug)
            {
                listener.Prefixes.Add("http://localhost:" + httpPort + "/");
                listener.Prefixes.Add("https://localhost:" + httpsPort + "/");
            }
            else
            {
                listener.Prefixes.Add("http://*:" + httpPort + "/");
                listener.Prefixes.Add("https://*:" + httpsPort + "/");
            }
#pragma warning restore
            listener.Start();
            while (true)
            {
                WebLoop(listener).Wait();
            }
        }
        static async Task WebLoop(HttpListener listener)
        {
            try
            {
                var context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                var url = string.Join(null, request.Url?.Segments[1..] ?? []);
                var urlSegs = url.Split('/');
                var response = context.Response;
                var uri = request.Url;
                Console.WriteLine($"Handling {request.HttpMethod} request with a URL of {url} from the address {request.RemoteEndPoint.Address}");
                async Task<BasicResult> GetResponse()
                {
                    try
                    {
                        if (uri != null)
                        {
                            if (request.HttpMethod == "POST")
                            {
                                if (urlSegs.Length >= 4 && urlSegs[0] == "api" && urlSegs[1] == "fc")
                                {
                                    if (authKeys.Contains(urlSegs[2]))
                                    {
                                        var nPath = string.Join("/", urlSegs[3..]);
                                        if (authKeyAllowedFiles.ContainsKey(urlSegs[2]) && !authKeyAllowedFiles[urlSegs[2]].Contains(nPath))
                                        {
                                            Console.WriteLine("An unauthorized user tried to modify a file at the location " + nPath + ". They have access to the locations " + string.Join(',', authKeyAllowedFiles[urlSegs[2]]));
                                            return new BasicResult("Unauthorized", 401);
                                        }
                                        using (MemoryStream ms = new())
                                        {
                                            Console.WriteLine(nPath);
                                            var path = root + sep + nPath;
                                            request.InputStream.CopyTo(ms);
                                            byte[] buffer = ms.ToArray();
                                            var splitPath = nPath.Split("/");
                                            if (splitPath.Length > 1)
                                            {
                                                var createdPath = root + sep + string.Join(sep, splitPath[..^1]);
                                                Console.WriteLine(createdPath);
                                                Directory.CreateDirectory(createdPath);
                                            }
                                            File.WriteAllBytes(path, buffer);
                                            return new BasicResult("Successfully wrote to file", 200);
                                        }
                                    }
                                    else
                                    {
                                        return BasicResult.Fail;
                                    }
                                }
                                else
                                {
                                    return BasicResult.Fail;
                                }
                            }
                            else if (request.HttpMethod == "GET")
                            {
                                if (string.IsNullOrEmpty(urlSegs[0]))
                                {
                                    return GetFileData(rootFile ?? ".root");
                                }
                                else if (urlSegs.Length == 3 && urlSegs[0] == "api" && urlSegs[1] == "testauth")
                                {
                                    if (authKeys.Contains(urlSegs[2]))
                                    {
                                        return new BasicResult("", 200);
                                    }
                                    else
                                    {
                                        return BasicResult.Fail;
                                    }
                                }
                                else if (urlSegs.Length == 2 && urlSegs[0] == "api" && urlSegs[1] == "ping")
                                {
                                    return new BasicResult("Ping acknowledged", 200);
                                }
                                else if (urlSegs.Length == 2 && urlSegs[0] == "api" && urlSegs[1] == "version")
                                {
                                    return new BasicResult($"V {majorVersion}.{minorVersion}.{incrementalVersion}", 200);
                                }
                                else if (urlSegs.Length == 2 && urlSegs[0] == "api" && urlSegs[1] == "nhaqi")
                                {
                                    HttpClient client = new HttpClient();
                                    var req = await client.GetAsync("https://website-api.airvisual.com/v1/cities/5bc9937ab3f912fa4c105d52/measurements?units.temperature=fahrenheit&units.distance=miles&units.pressure=mercury&units.system=imperial&AQI=US&language=en-US");
                                    var json = await req.Content.ReadAsStringAsync();
                                    var node = JsonNode.Parse(json);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                    var measurements = node["measurements"]["hourly"].AsArray().Last();
                                    if (!measurements.AsObject().ContainsKey("aqi"))
                                    {
                                        measurements = node["measurements"]["hourly"].AsArray()[^2];
                                    }
                                    var aqi = measurements["aqi"];
                                    var pm25 = measurements["pm25"]["concentration"];
                                    return new BasicResult(aqi.ToString() + ";" + pm25.ToString() + ";" + DateTime.Now.ToString("MMM d h:mm tt"), 200);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                }
                                else
                                {
                                    return GetFileData(url);
                                }
                            }
                            else if (request.HttpMethod == "DELETE")
                            {
                                try
                                {
                                    if (urlSegs.Length == 4 && urlSegs[0] == "api" && urlSegs[1] == "fc")
                                    {
                                        if (authKeys.Contains(urlSegs[2]))
                                        {
                                            var filePath = urlSegs[3];
                                            var path = root + sep + filePath;
                                            File.Delete(path);
                                            return new BasicResult("Successfully deleted file", 200);
                                        }
                                        else
                                        {
                                            return BasicResult.Fail;
                                        }
                                    }
                                    else
                                    {
                                        return BasicResult.Fail;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Malformed deletion request!");
                                    return BasicResult.Fail;
                                }
                            }
                            else
                            {
                                return BasicResult.Fail;
                            }
                        }
                        else
                        {
                            return new BasicResult("Invalid URL", 404);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An exception was handled when processing a request, stating: " + ex.ToString());
                        return new BasicResult("An unexpected server error has occured", 404);
                    }
                }
                response.Respond(await GetResponse());
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception was handled, stating: \n" + ex.ToString());
            }
        }
    }
    public static class Extensions
    {
        public static void Respond(this HttpListenerResponse res, Program.BasicResult response)
        {
            byte[] buffer = response.Data;
            res.StatusCode = response.Code;
            res.ContentLength64 = buffer.Length;
            res.OutputStream.Write(buffer, 0, buffer.Length);
            res.OutputStream.Close();
        }
    }
}
