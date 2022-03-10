using System;
using System.Net;
using System.Net.Http;

namespace AKSTest // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static HttpClient? client;
        static String testURL = String.Empty;
        static int numTests = 100;
        static int delay = 1000;
        static String logFilePath = "AKSTestLog-" + DateTime.Now.Ticks + ".log";
        private static readonly object logLock = new object();
        static bool useLegacyThreading = true;
        static bool disableKeepAlive = false;

        static void Main(string[] args)
        {
            PrintBanner();

            if (args.Count() > 0)
            {
                testURL = args[0];
                numTests = Convert.ToInt32(args[1]);
                delay = Convert.ToInt32(args[2]);
                if (args.Count() > 3)
                {
                    if (args[3] == "0")
                    {
                       useLegacyThreading = false;
                    }
                    else if(args[3] == "1")
                    {
                        useLegacyThreading = true;
                    }
                    else if(args[3] == "2")
                    {
                        useLegacyThreading = true;
                        disableKeepAlive = true;
                    }
                }
            }
            else
            {
                testURL = "https://bing.com";
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\r\nStarting test: URL{0}\r\nPassess={1}\r\nTotal Requests={2}\r\nDelay={3}", testURL, numTests, numTests * numTests, delay);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Log file={0}", logFilePath);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (useLegacyThreading)
            {
                RunConnectivityTest2(testURL, numTests, delay * 1000); // thread.start()
            }
            else
            {
                RunConnectivityTest(testURL, numTests, delay * 1000); // async await
            }

            Console.ReadKey();
        }

        // Make i iterations of r requests so numPasses = 10 = 10 passes (i) of 10 request blocks (r) each = 100 requests
        static async void RunConnectivityTest(string URL, int numPasses, int waitDelay)
        {
            for (int i = 0; i < numPasses; i++)
            {
                for (int r = 0; r < numPasses; r++)
                {
                    try
                    {
                        client = new HttpClient();
                        var result = await client.GetAsync(URL);
                        Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt"), result.StatusCode);
                        AppendLog(i + " " + DateTime.Now.ToLongTimeString() + ": " + result.StatusCode + "\r\n");
                        if (client != null)
                        {
                            client.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, DateTime.Now.ToString("MM/dd/yy hh: mm:ss.fffffff tt"), "ERROR: " + ex.Message);
                        AppendLog(i + " " + DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt") + ": " + "ERROR: " + ex.Message + "\r\n");
                    }

                    System.Threading.Thread.Sleep(200);
                }
                System.Threading.Thread.Sleep(waitDelay);
            }
        }

        // Make i iterations of r requests so numPasses = 10 = 10 passes (i) of 10 request blocks (r) each = 100 requests
        static void RunConnectivityTest2(string URL, int numPasses, int waitDelay)
        {
            for (int i = 0; i < numPasses; i++)
            {
                for (int r = 0; r < numPasses; r++)
                {
                    if (disableKeepAlive)
                    {
                        Thread t = new Thread(() => MakeRequest2(URL, numPasses, waitDelay, i, r));
                        t.Start();
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        Thread t = new Thread(() => MakeRequest(URL, numPasses, waitDelay, i, r));
                        t.Start();
                        System.Threading.Thread.Sleep(50);
                    }
                }
                System.Threading.Thread.Sleep(waitDelay);
            }
        }

        // uses keep-alive
        static void MakeRequest(string URL, int numPasses, int waitDelay, int i, int r)
        {
            try
            {
                client = new HttpClient();
                var result = client.GetAsync(URL);
                Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, GetTimeStamp(), result.Result.StatusCode);
                AppendLog(i + "/" + r + " " + GetTimeStamp() + ": " + result.Result.StatusCode + "\r\n");
            }
            catch (Exception ex)
            {
                string message = String.Empty;
                if(ex.InnerException != null && ex.InnerException.HResult == -2147467259)
                {
                    message = "Repro? --> " + ex.InnerException.Message;
                }
                else
                {
                    message = ex.Message;
                }
                Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, GetTimeStamp(), "ERROR: " + message);
                AppendLog(i + "/" + r + " " + GetTimeStamp() + ": " + message + "\r\n");
            }
        }

        // disables keep-alive
        static void MakeRequest2(string URL, int numPasses, int waitDelay, int i, int r)
        {
            try
            {
                HttpWebRequest client = (HttpWebRequest)WebRequest.Create(URL);
                client.KeepAlive = false;
                HttpWebResponse response = (HttpWebResponse)client.GetResponse();
                Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, GetTimeStamp(), response.StatusDescription);
                AppendLog(i + "/" + r + " " + GetTimeStamp() + ": " + response.StatusDescription + "\r\n");
            }
            catch (Exception ex)
            {
                string message = String.Empty;
                if (ex.InnerException != null && ex.InnerException.HResult == -2147467259)
                {
                    message = "Repro? --> " + ex.InnerException.Message;
                }
                else
                {
                    message = ex.Message;
                }
                Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, GetTimeStamp(), "ERROR: " + message);
                AppendLog(i + "/" + r + " " + GetTimeStamp() + ": " + message + "\r\n");
            }

        }
        static String GetTimeStamp()
        {
            return DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt");
        }
        static void AppendLog(string msg)
        {
            lock (logLock)
            {
                try
                {
                    StreamWriter SW;
                    SW = File.AppendText(logFilePath);
                    SW.WriteLine(msg);
                    SW.Close();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error writing to log {0} \r\n {1}", logFilePath, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        static void PrintBanner()
        {
            Console.WriteLine("====================================================================");
            Console.WriteLine("            AKS Pod Test Client         \r\n");
            Console.WriteLine("Usage: AKSRequestClient.exe [<URL> <NumRequets> <Delay>]            ");
            Console.WriteLine("If all args are ommited, https://bing.com is used with 100 requests.");
            Console.WriteLine("====================================================================");
        }
    }
}