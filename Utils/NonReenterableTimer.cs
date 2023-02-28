using System;
using System.Threading;
using System.Threading.Tasks;

namespace sd_proxy.Utils
{
	public abstract class NonReenterableTimer : IDisposable
	{
		private static Timer? _timer;
		private static object _locker = new object();
		private CancellationToken _cancellationToken;

		public void Dispose()
		{
			StopInternal();
		}

		public Task StartAsync(CancellationToken cancellationToken, int timeoutMs)
		{
			_cancellationToken = cancellationToken;

			_timer = new Timer(TimerCallback, null, 0, timeoutMs);

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			StopInternal();

			return Task.CompletedTask;
		}

		// https://gunnarpeipman.com/avoid-overlapping-timer-calls/
		private void TimerCallback(object? state)
		{
			if (_cancellationToken.IsCancellationRequested)
				return;

			var hasLock = false;

			try
			{
				Monitor.TryEnter(_locker, ref hasLock);
				if (!hasLock)
					return;

				DoWork();
			}
			finally
			{
				if (hasLock)
					Monitor.Exit(_locker);
			}
		}

		public abstract void DoWork();

		private void StopInternal()
		{
			Monitor.Enter(_locker);
			_timer?.Dispose();
		}
	}
}
