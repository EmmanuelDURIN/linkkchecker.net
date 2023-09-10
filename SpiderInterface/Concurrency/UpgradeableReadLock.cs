namespace SpiderInterface.Concurrency
{
    public class UpgradeableReadLock : IDisposable
    {
        protected ReaderWriterLockSlim innerLock;
        public UpgradeableReadLock(ReaderWriterLockSlim innerLock)
        {
            this.innerLock = innerLock;
            innerLock.EnterUpgradeableReadLock();
        }
        public void Dispose() => innerLock.ExitUpgradeableReadLock();
        public IDisposable GetWriteLock() => new WriteLock(innerLock);
    }
}