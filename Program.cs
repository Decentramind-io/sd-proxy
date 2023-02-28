using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace sd_proxy
{
	public class Program
	{
		private static string _listenPort;

		public static void Main(string[] args)
		{
			foreach (var arg in args)
			{
				if (arg.Contains("listen-port", System.StringComparison.InvariantCultureIgnoreCase))
				{
					var tokens = arg.Split('=');
					if (tokens.Count() > 1 && !string.IsNullOrEmpty(tokens[1]))
					{
						_listenPort = tokens[1];
						break;
					}
				}
			}

			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					string listenPort = "5001";

					if (string.IsNullOrEmpty(_listenPort))
					{
						var configPort = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("AppSettings:ProxyListenPort").Value;
						if (!string.IsNullOrEmpty(configPort))
							listenPort = configPort;
					}
					else
						listenPort = _listenPort;

					webBuilder.UseStartup<Startup>();
					webBuilder.UseUrls($"http://0.0.0.0:{listenPort}");
				});
	}
}
