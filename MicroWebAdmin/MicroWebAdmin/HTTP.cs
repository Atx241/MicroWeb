namespace MicroWebAdmin
{
    internal class HTTP
    {
        public struct Response
        {
            public int Code;
            public string Message;
        }
        static HttpClient? client = null;
        public static bool InitializeHTTP(string baseAddress)
        {
            try 
            {
                client = new HttpClient();
                client.BaseAddress = new Uri(baseAddress);
                return SendGetReq("/api/ping").Code == 200;
            } catch
            {
                return false;
            }
        }
        public static bool TestKey(string token)
        {
            try
            {
                var response = SendGetReq("/api/testauth/" + token);
                return response.Code == 200;
            }
            catch
            {
                return false;
            }
        }
        public static Response SendGetReq(string location)
        {
            if (client == null)
            {
                throw new Exception("HTTP was not properly initialized");
            }
            try
            {
                var response = client.Send(new HttpRequestMessage(HttpMethod.Get, location));
                var code = response.StatusCode;
                var message = response.Content.ReadAsStringAsync().Result;
                return new Response { Code = (int)code, Message = message };
            } catch
            {
                throw new Exception($"GET request to {location} failed");
            }
        }
        public static Response SendPostReq(string location, byte[] body)
        {
            if (client == null)
            {
                throw new Exception("HTTP was not properly initialized");
            }
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, location);
                req.Content = new ByteArrayContent(body);
                var response = client.Send(req);
                var code = response.StatusCode;
                var message = response.Content.ReadAsStringAsync().Result;
                return new Response { Code = (int)code, Message = message };
            } catch
            {
                throw new Exception($"POST request to {location} failed");
            }
        }
        public static Response SendDeleteReq(string location)
        {
            if (client == null)
            {
                throw new Exception("HTTP was not properly initialized");
            }
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Delete, location);
                var response = client.Send(req);
                var code = response.StatusCode;
                var message = response.Content.ReadAsStringAsync().Result;
                return new Response { Code = (int)code, Message = message };
            }
            catch
            {
                throw new Exception($"DELETE request to {location} failed");
            }
        }
    }
}
