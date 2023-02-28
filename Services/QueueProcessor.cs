using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestSharp;
using sd_proxy.DTOs;
using sd_proxy.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace sd_proxy.Services
{
	public class QueueProcessor : NonReenterableTimer, IHostedService
	{
		private readonly List<QueueItem> _queue = new List<QueueItem>();
		private readonly List<QueueItem> _failed = new List<QueueItem>();
		private QueueItem _currentItem = null;
		private readonly SemaphoreLocker _locker = new SemaphoreLocker();
		private readonly List<ReadyItem> _readyItems = new List<ReadyItem>();
		private string _sdAddress = null;

		private readonly ILogger _logger;
		public QueueProcessor(ILogger<QueueProcessor> logger)
		{
			_logger = logger;
			_logger.LogInformation("QueueProcessor started");
		}

		public override void DoWork()
		{
			if (_queue.Count == 0)
			{
				return;
			}

			var t = _locker.Lock(() =>
			{
				if (_queue.Count == 0)
				{
					return false;
				}
				else
				{
					_logger.LogInformation("found item");
					_currentItem = _queue[0];
					_queue.RemoveAt(0);
					return true;
				}
			});

			if (!t)
				return;

			_logger.LogInformation("got current item");

			var img = CallExternalSd(_currentItem);
			/*if (img == null)
			{
				_locker.Lock(() =>
				{
					_queue.Insert(0, _currentItem);
					_currentItem = null;
					return true;
				});

				return;
			}*/

			_locker.Lock(() =>
			{
				if (img == null)
				{
					_failed.Add(_currentItem);
					// TODO add timestamp to failed items to gc 'em
				}
				else
				{
					_readyItems.Add(new ReadyItem()
					{
						img = img,
						finishedAt = DateTime.UtcNow,
						sessionId = _currentItem.sessionId
					});
				}

				_currentItem = null;
				return true;
			});
		}

		public string AddItem(GenerateRequest req)
		{
			_logger.LogInformation("new item");

			QueueItem item = new QueueItem(req);

			_locker.Lock(() =>
			{
				_queue.Add(item);
				return true;
			});

			return item.sessionId;
		}

		public string GetStatus(string sessionId)
		{
			var status = _locker.Lock(() =>
			{
				if (_currentItem != null && _currentItem.sessionId.Equals(sessionId, StringComparison.InvariantCultureIgnoreCase))
					return "processed";

				if (_queue.Find(x => x.sessionId.Equals(sessionId, StringComparison.InvariantCultureIgnoreCase)) != null)
						return "queued";

				if (_readyItems.Find(x => x.sessionId.Equals(sessionId, StringComparison.InvariantCultureIgnoreCase)) != null)
					return "ready";

				if (_failed.Find(x => x.sessionId.Equals(sessionId, StringComparison.InvariantCultureIgnoreCase)) != null)
					return "failed";

				return "unknown";
			});

			return status;
		}

		public void GetQueueLen(string sessionId, out int totalLen, out int queuedBeforeMe)
		{
			var len = 0;
			var queued = 0;

			var status = _locker.Lock(() =>
			{
				len = _queue.Count;
				queued = _queue.FindIndex(x => x.sessionId == sessionId);
				return true;
			});

			totalLen = len;
			queuedBeforeMe = queued;
		}

		public ReadyItem GetItem(string sessionId)
		{
			return _readyItems.Find(x => x.sessionId.Equals(sessionId, StringComparison.InvariantCultureIgnoreCase));
		}

		private byte[] CallExternalSd(QueueItem item)
		{
			if (_sdAddress == null)
			{
				var sdAddress = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("AppSettings:SdAddress");
				if (string.IsNullOrEmpty(sdAddress.Value))
					_sdAddress = "http://127.0.0.1:7680";
				else
					_sdAddress = sdAddress.Value;
			}

			_logger.LogInformation($"sd address value {_sdAddress}");

			var body = $"{{\"fn_index\":77,\"data\":[\"task(1aybowcggeky34z)\",\"{item.prompt}\",\"{item.negativePrompt}\",[],{item.samplingSteps},\"Euler a\",false,false,1,1,{item.cfgScale},-1,-1,0,0,0,false,512,512,false,0.7,2,\"Latent\",0,0,0,[],\"None\",false,false,\"positive\",\"comma\",0,false,false,\"\",\"Seed\",\"\",\"Nothing\",\"\",\"Nothing\",\"\",true,false,false,false,0,[],\"\",\"\",\"\"],\"session_hash\":\"7vhly9l8v9o\"}}";

			var client = new RestClient(_sdAddress);
			var request = new RestRequest($"run/predict/", Method.Post);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("Connection", "keep-alive");
			request.AddBody(body);

			RestResponse response;
			try
			{
				response = client.Execute(request);
				if (response.StatusCode != System.Net.HttpStatusCode.OK)
					return null;
			}
			catch(Exception ex)
			{
				// TODO
				return null;
			}

			try
			{
				using (JsonDocument document = JsonDocument.Parse(response.Content!))
				{
					JsonElement root = document.RootElement;
					JsonElement dataArray = root.GetProperty("data");
					var name = dataArray[0][0].GetProperty("name");

					request = new RestRequest($"file={name}", Method.Get);
					return client.DownloadData(request);
				}
			}
			catch (Exception)
			{
				// TODO
				return null;
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			return base.StartAsync(cancellationToken, 1000);
		}
	}
}
