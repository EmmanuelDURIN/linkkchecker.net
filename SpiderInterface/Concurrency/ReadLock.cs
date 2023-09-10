namespace SpiderInterface.Concurrency
{
    public class ReadLock : IDisposable
    {
        protected ReaderWriterLockSlim innerLock;
        public ReadLock(ReaderWriterLockSlim innerLock)
        {
            this.innerLock = innerLock;
            innerLock.EnterReadLock();
        }
        public void Dispose() => innerLock.ExitReadLock();
    }
}