using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using RedLockNet.SERedis;

namespace Locking
{
	public class LockService : ILockService
	{
		private readonly RedLockFactory _redlockFactory;
		private readonly ILogger<LockService> _log;
		private readonly TimeSpan _expiry;
		private readonly TimeSpan _wait;
		private readonly TimeSpan _retry;
		private readonly TelemetryClient _telemetryClient;

		public LockService(
				RedLockFactory redlockFactory,
				TimeSpan expiry,
				TimeSpan wait,
				TimeSpan retry,
				ILogger<LockService> logger,
				TelemetryClient telemetryClient
		)
		{
			_log = logger;
			_redlockFactory = redlockFactory;
			_expiry = expiry;
			_wait = wait;
			_retry = retry;
			_telemetryClient = telemetryClient;
		}

		public async Task PerformWhileLocked(string id, Func<LockToken, Task> doStuff)
		{
			var lockToken = new StringLockToken(id);
			await PerformWhileLocked(lockToken, doStuff);
		}

		public async Task PerformWhileLocked(LockToken lockToken, Func<LockToken, Task> doStuff)
		{
			// This dependency provides a parent for the Acquire, Leave and anything we do while locked.
			string id = lockToken.GetId();
			using var lockDependency = new Dependency(_telemetryClient, "LockService", "Locked", id);

			// This dependency lets us easily see how long it takes to get a lock.
			var dependency = new Dependency(_telemetryClient, "LockService", "Acquire", id);
			var stopwatch = Stopwatch.StartNew();
			var redLock = await _redlockFactory.CreateLockAsync(id, _expiry, _wait, _retry);
			try
			{
				stopwatch.Stop();
				// If the lock took too long to acquire we'd better make a note.
				if (stopwatch.Elapsed > _wait)
				{
					_log?.LogWarning("Acquiring lock on {Resource} took {AcquireDuration}ms", id, stopwatch.Elapsed.TotalMilliseconds);
				}
				dependency.Success = redLock.IsAcquired && stopwatch.Elapsed < _wait;
				dependency.Dispose();
				if (redLock.IsAcquired)
				{
					// do stuff
					await doStuff(lockToken);
				}
				else
				{
					_log?.LogError("Could not acquire lock: {Id}", id);
				}
			}
			finally
			{
				using (new Dependency(_telemetryClient, "LockService", "Release", id))
				{
					redLock.Dispose();
				}
			}
		}

		public async Task<T> PerformWhileLocked<T>(string id, Func<LockToken, Task<T>> doStuff)
		{
			var lockToken = new StringLockToken(id);
			return await PerformWhileLocked(lockToken, doStuff);
		}

		public async Task<T> PerformWhileLocked<T>(LockToken lockToken, Func<LockToken, Task<T>> doStuff)
		{
			string id = lockToken.GetId();
			// This dependency provides a parent for the Acquire, Leave and anything we do while locked.
			using var lockDependency = new Dependency(_telemetryClient, "LockService", "Locked", id);

			// This dependency lets us easily see how long it takes to get a lock.
			var dependency = new Dependency(_telemetryClient, "LockService", "Acquire", id);
			var stopwatch = Stopwatch.StartNew();
			var redLock = await _redlockFactory.CreateLockAsync(id, _expiry, _wait, _retry);
			try
			{
				stopwatch.Stop();

				// If the lock took too long to acquire we'd better make a note.
				if (stopwatch.Elapsed > _wait)
				{
					_log?.LogWarning("Acquiring lock on {resource} took {acquireDuration}ms", id, stopwatch.Elapsed.TotalMilliseconds);
				}
				dependency.Success = redLock.IsAcquired && stopwatch.Elapsed < _wait;
				dependency.Dispose();
				if (redLock.IsAcquired)
				{
					// do stuff
					return await doStuff(lockToken);
				}
				else
				{
					_log?.LogError("Could not acquire lock: {resource}", id);
				}
			}
			finally
			{
				// We want to time how long it takes to unlock as well:-)
				using (new Dependency(_telemetryClient, "LockService", "Release", id))
				{
					redLock.Dispose();
				}
			}

			// the lock is automatically released at the end of the using block
			return default;
		}

	}
}

