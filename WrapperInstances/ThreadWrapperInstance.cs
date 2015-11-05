using System;
using System.Threading;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class Thread : IThread
    {
        private readonly System.Threading.Thread _thread;

        public string Name { get { return _thread.Name; } set { _thread.Name = value; } }
        public bool IsBackground { get { return _thread.IsBackground; } set { _thread.IsBackground = value; } }

        public bool IsRunning { get; }

        internal Thread(Action action) { _thread = new System.Threading.Thread(new ThreadStart(action)); }

        public void Start() { _thread.Start(); }

        public void Abort() { _thread.Abort(); }
    }

    public class ThreadWrapperInstance : IThreadWrapper
    {
        public IThread CreateThread(Action action) { return new Thread(action); }

        public void Sleep(int milliseconds) { System.Threading.Thread.Sleep(milliseconds); }

        public void QueueUserWorkItem(Aragas.Core.Wrappers.WaitCallback waitCallback) { ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(waitCallback)); }
    }
}
