using NBomber.Http.CSharp;
using NBomber.CSharp;
using System;
using NBomber.Http;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DynamicLoadTesting
{
    class Program
    {
        private static List<string> requestItems = new List<string>();

        static void Main(string[] args)
        {
            string stepName, curl = string.Empty;
            string testCurlPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/testCurl.json";
            int numberOfConcurrentThreads, actualDurationInSeconds, warmupDurationInSeconds;

            Console.WriteLine("Enter custom test name:");
            stepName = Console.ReadLine();

            NumberOfConcurrentThreads:
                try
                {
                    Console.WriteLine("Enter number of concurrent threads(e.g. 2, 4, 91):");
                    numberOfConcurrentThreads = int.Parse(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid value, must be a number!");
                    goto NumberOfConcurrentThreads;
                }

            ActualDurationInSeconds:
                try
                {
                    Console.WriteLine("Enter duration of load test in seconds(e.g. 5, 10, 240):");
                    actualDurationInSeconds = int.Parse(Console.ReadLine());

                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid value, must be a number!");
                    goto ActualDurationInSeconds;
                }

            WarmupDurationInSeconds:
                try
                {
                    Console.WriteLine("Enter load test warmup duration in seconds(e.g. 0, 5, 10):");
                    warmupDurationInSeconds = int.Parse(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid value, must be a number!");
                    goto WarmupDurationInSeconds;
                }

            CurlEntry:
                    File.WriteAllText(testCurlPath, "//Replace this text with your curl, save and type anything in terminal pop-up");
                    Console.WriteLine("Copy/Paste your cURL into testCurl.json on your Desktop and type anything");
                    Console.ReadLine();
                    try
                    {
                        curl = File.ReadAllText(testCurlPath);
                    }
                    catch (Exception)
                    {

                    }
                    var methodAndUrl = TryGetMethodAndUrl(curl);
                    if(methodAndUrl.Item1 != "GET" && methodAndUrl.Item1 != "POST" &&
                        string.IsNullOrEmpty(methodAndUrl.Item2))
                    {
                        Console.WriteLine(methodAndUrl.Item1);
                        goto CurlEntry;
                    }


                var httpRequest = Http.CreateRequest(methodAndUrl.Item1, methodAndUrl.Item2);

                //append headers
                httpRequest = AppendHeaders(httpRequest);

                //append body
                if(methodAndUrl.Item1 == "POST")
                {
                    httpRequest = AppendBody(httpRequest);
                }

                //Delete testCurl.json from desktop
                File.Delete(testCurlPath);

                var loadStep = HttpStep.Create(stepName, (context) => httpRequest.WithCheck(response => Task.FromResult(response.IsSuccessStatusCode)));

                var scenario = ScenarioBuilder.CreateScenario(stepName, new NBomber.Contracts.IStep[1] { loadStep })
                                                .WithConcurrentCopies(numberOfConcurrentThreads)
                                                .WithWarmUpDuration(TimeSpan.FromSeconds(warmupDurationInSeconds))
                                                .WithDuration(TimeSpan.FromSeconds(actualDurationInSeconds));


                NBomberRunner.RegisterScenarios(scenario)
                             .RunInConsole();
                
        }



        private static HttpRequest AppendHeaders(HttpRequest httpRequest)
        {
            HttpRequest requestWithHeaders = null;
            foreach(var item in requestItems)
            {
                var currentItem = item.Replace("\n", "").Replace("'","").Trim();
                if(currentItem.StartsWith("-H"))
                {
                    currentItem = currentItem.Replace("-H", "").Trim();
                    requestWithHeaders = httpRequest.WithHeader(currentItem.Split(":")[0], currentItem.Split(":")[1]);
                    httpRequest = requestWithHeaders;
                }
            }
            return requestWithHeaders;
        }

        private static HttpRequest AppendBody(HttpRequest httpRequest)
        {
            var body = requestItems.Where(x => x.StartsWith("\n  -d")).FirstOrDefault();
            if (body != null)
            {
                body = Regex.Replace(body, @"\s+", "");
                body = body.Replace("\n", "");
                var contentType = httpRequest.Headers.Where(x => x.Key == "Content-Type").Select(x => x.Value).FirstOrDefault() == null ? "application/json" 
                    : httpRequest.Headers.Where(x => x.Key == "Content-Type").Select(x => x.Value).FirstOrDefault();

                httpRequest = httpRequest.WithBody(new StringContent(body, Encoding.UTF8, contentType.Trim()));
            }

            return httpRequest;
        }

        private static Tuple<string,string> TryGetMethodAndUrl(string curl)
        {
            try
            {
                requestItems = curl.Split('\\').ToList();
                var method = requestItems[0].Split(" ")[2].Trim();
                var url = requestItems[1].Replace("'","").Trim();
                return new Tuple<string, string>(method,url);
            }
            catch(Exception ex)
            {
                return new Tuple<string, string>($"****************\nERROR: {ex.Message}", "");
            }
        }
    }
}
