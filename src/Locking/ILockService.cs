using System;
using System.Threading.Tasks;

namespace Locking
{
    public abstract class LockToken
    {      
        public LockToken()
        {
        }

        public abstract string GetId();
    }

    public class StringLockToken : LockToken
    {
        private readonly string _lockId;
        public StringLockToken(string lockId)
        {
            _lockId = lockId;
        }

        public override string GetId()
        {
            return _lockId;
        }
    }

    public interface ILockService
    {
        public Task<T> PerformWhileLocked<T>(LockToken token, Func<LockToken, Task<T>> doStuff);
        public Task PerformWhileLocked(LockToken token, Func<LockToken, Task> doStuff);
        public Task PerformWhileLocked(string id, Func<LockToken, Task> doStuff);
        public Task<T> PerformWhileLocked<T>(string id, Func<LockToken, Task<T>> doStuff);
    }

    public class LockingException : Exception
    {
        public LockingException(string message) : base(message)
        {
        }
    }
}