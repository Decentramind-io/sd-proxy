using Microsoft.AspNetCore.Mvc;
using sd_proxy.DTOs;
using sd_proxy.Services;
using System.IO;

namespace sd_proxy.Controllers
{
	[ApiController]
	//[Route("sd")]
	public class SDProxy : Controller
	{
		private QueueProcessor _qProcessor;
		public SDProxy(QueueProcessor qProc)
		{
			_qProcessor = qProc;
		}

		[HttpPost]
		[Route("generate")]
		[Produces("application/json")]
		public GenerateResponse Generate([FromBody] GenerateRequest req)
		{
			return new GenerateResponse()
			{
				session_id = _qProcessor.AddItem(req)
			};
		}

		[HttpGet]
		[Route("status/{sessionId}")]
		[Produces("application/json")]
		public ActionResult<StatusResponse> Status(string sessionId)
		{
			var queue_length = 0;
			var queued_before_me = 0;

			var status = _qProcessor.GetStatus(sessionId);
			if (status == "queued")
			{
				_qProcessor.GetQueueLen(sessionId, out queue_length, out queued_before_me);
			}
			else if (status == "unknown")
				return NotFound();

			return new StatusResponse()
			{
				status = _qProcessor.GetStatus(sessionId),
				queue_length = queue_length,
				queued_before_me = queued_before_me
			};			
		}

		[HttpGet]
		[Route("{sessionId}")]
		public IActionResult GetImage(string sessionId)
		{
			var item = _qProcessor.GetItem(sessionId);
			if (item == null)
				return NotFound();

			MemoryStream ms = new MemoryStream(item.img);
			return File(ms, "image/png");
		}
	}
}
