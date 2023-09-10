namespace SpiderInterface.Concurrency
{
    public class SafeReaderWriterLockProvider
    {
        private ReaderWriterLockSlim innerLock = new ReaderWriterLockSlim();
        public ReadLock GetReadLock() => new ReadLock(innerLock);
        public IDisposable GetWriteLock() => new WriteLock(innerLock);
        internal UpgradeableReadLock GetUpgradableReadLock() => new UpgradeableReadLock(innerLock);
    }
}
