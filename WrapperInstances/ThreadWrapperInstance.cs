using System.Threading;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class CustomThread : IThread
    {
        private readonly Thread _thread;

        public string Name { get { return _thread.Name; } set { _thread.Name = value; } }
        public bool IsBackground { get { return _thread.IsBackground; } set { _thread.IsBackground = value; } }

        public bool IsRunning => _thread.ThreadState != ThreadState.Stopped;

        internal CustomThread(Aragas.Core.Wrappers.ThreadStart action) { _thread = new Thread(new System.Threading.ThreadStart(action)); }
        internal CustomThread(Aragas.Core.Wrappers.ParameterizedThreadStart action) { _thread = new Thread(new System.Threading.ParameterizedThreadStart(action)); }

        public void Start() { _thread.Start(); }
        public void Start(object obj) { _thread.Start(obj); }

        public void Abort() { _thread.Abort(); }
    }

    public class ThreadWrapperInstance : IThreadWrapper
    {
        public IThread CreateThread(Aragas.Core.Wrappers.ThreadStart action) { return new CustomThread(action); }

        public IThread CreateThread(Aragas.Core.Wrappers.ParameterizedThreadStart action) { return new CustomThread(action); }

        public void Sleep(int milliseconds) { Thread.Sleep(milliseconds); }

        public void QueueUserWorkItem(Aragas.Core.Wrappers.WaitCallback waitCallback) { ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(waitCallback)); }
    }
}
