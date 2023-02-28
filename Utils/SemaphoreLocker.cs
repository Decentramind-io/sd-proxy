using System;
using System.Threading;
using System.Threading.Tasks;

namespace sd_proxy.Utils
{
	public class SemaphoreLocker
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public async Task LockAsync(Func<Task> worker)
		{
			await _semaphore.WaitAsync();
			try
			{
				await worker();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/*public void Lock(Func<Task> worker)
		{
			_semaphore.Wait();
			try
			{
				worker();
			}
			finally
			{
				_semaphore.Release();
			}
		}*/

		// overloading variant for non-void methods with return type (generic T)
		public async Task<T> LockAsync<T>(Func<Task<T>> worker)
		{
			await _semaphore.WaitAsync();
			try
			{
				return await worker();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public T Lock<T>(Func<T> worker)
		{
			_semaphore.Wait();
			try
			{
				return worker();
			}
			finally
			{
				_semaphore.Release();
			}
		}
	}
}
