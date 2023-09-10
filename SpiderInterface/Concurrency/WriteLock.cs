namespace SpiderInterface.Concurrency
{
    public class WriteLock : IDisposable
    {
        private ReaderWriterLockSlim innerLock;
        public WriteLock(ReaderWriterLockSlim innerLock)
        {
            this.innerLock = innerLock;
            innerLock.EnterWriteLock();
        }
        public void Dispose() => innerLock.ExitWriteLock();
    }
}
