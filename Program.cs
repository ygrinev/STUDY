using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using ParseLogFile.Repo;
using Microsoft.EntityFrameworkCore;

namespace ParseLogFile
{
    class Program
    {
        public static IConfigurationRoot configuration;
        static void Main(string[] args)
        {
            // Parse emails
            //List<string> list = new List<string> { "riya riya@gmail.com", "julia julia@julia.me", "julia sjulia@gmail.com", "julia julia@gmail.com", "samantha samantha@gmail.com", "tanya tanya@gmail.com" };
            //foreach (string name in list.Where(el=>Regex.IsMatch(el.Split(' ')[1], "[a-z.]+[@]gmail[.]com$")).OrderBy(p => p.Split(' ')[0]).Select(pr=>pr.Split(' ')[0]))
            //{ 
            //    Console.WriteLine(name);
            //}
            //Console.ReadLine();
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            try
            {
                // Start the actual workflow
                serviceCollection.BuildServiceProvider().GetService<FileParser>().Parse(args); //Async .Wait();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed!!! \r\n"+ ex.InnerException.Message??ex.Message);
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton(configuration);

            serviceCollection.AddSingleton<FileParser>();
            serviceCollection.AddEntityFrameworkSqlServer()
            .AddDbContext<LogContext>(options => options.UseSqlServer(configuration.GetConnectionString("DataMartConnection")));
        }
    }
}
