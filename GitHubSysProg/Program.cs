using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHubSysProg{
    public class Program{

        public static readonly HttpClient HttpClient = new();
        
        public static void Main(string[] args){
            HttpListener listener = new HttpListener();
            listener.Prefixes
                    .Add("http://localhost:8080/");
            listener.Start();

            var baseDir = Directory.GetCurrentDirectory();
            if (baseDir != null){
                var path = Path.Combine(baseDir, ".env");
                DotEnv.Load(path);
            }
            
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubSysProg");
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(HandleRequest, context);
            }
            
        }
        private static void HandleRequest(object? state){
            if (state == null)
                return;

            var context = (HttpListenerContext)state;

            try
            {
                var query = context.Request.Url?.Query?.Remove(0, 1)?.Split("&");

                if (query == null || query.Length != 2)
                {
                    throw new Exception("Invalid query parameters. The query must contain exactly two parameters.");
                }

                var ownerParam = query[0].Split("=");
                var repoParam = query[1].Split("=");

                if (ownerParam.Length != 2 || ownerParam[0] != "owner")
                {
                    throw new Exception("Invalid owner parameter. The owner parameter must be specified.");
                }

                if (repoParam.Length != 2 || repoParam[0] != "repo")
                {
                    throw new Exception("Invalid repo parameter. The repo parameter must be specified.");
                }

                var owner = ownerParam[1];
                var repo = repoParam[1];

                List<Contributor> contributors;

                var key = $"{owner}/{repo}";

                if (Cache.Contains(key))
                {
                    contributors = Cache.ReadFromCache(key);
                }
                else
                {
                    var apiUrl = $"https://api.github.com/repos/{key}/stats/contributors";
                    var response = HttpClient.GetAsync(apiUrl).Result;
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode == HttpStatusCode.Accepted) {
                        Console.WriteLine("Request accepted. Waiting for response...");
                        contributors = new List<Contributor>();
                    } else {
                        var content = response.Content.ReadAsStringAsync().Result;
                        contributors = JsonConvert.DeserializeObject<List<Contributor>>(content);
                        Cache.WriteToCache(key, contributors);
                    }
                }

                Console.WriteLine(key);
                var totalCommits = 0;
                foreach (var contributor in contributors)
                {
                    Console.WriteLine($"{contributor.Author.Login}: {contributor.Total}");
                    totalCommits += contributor.Total;
                }
                Console.WriteLine($"Total: {totalCommits}\n");

                var json = JsonConvert.SerializeObject(contributors);
                var buffer = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                var output = context.Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}