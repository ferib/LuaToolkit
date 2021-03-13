using System;
using Web.Nancy;
using Nancy.Hosting.Self;
using System.Threading;

namespace Web
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Launching Nancy on 127.0.0.1:9934");
            NancyHost host = new NancyHost(new Uri("http://127.0.0.1:9934/"));
            host.Start();
            Thread.Sleep(-1);
        }
    }
}
