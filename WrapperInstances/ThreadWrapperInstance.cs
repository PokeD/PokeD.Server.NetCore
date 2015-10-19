using System;
using System.Threading;

using Aragas.Core.Wrappers;

namespace PokeD.Server.Desktop.WrapperInstances
{
    public class ThreadClass : IThread
    {
        private readonly Thread _thread;

        public string Name { get { return _thread.Name; } set { _thread.Name = value; } }
        public bool IsBackground { get { return _thread.IsBackground; } set { _thread.IsBackground = value; } }

        public bool IsRunning { get; }

        internal ThreadClass(Action action) { _thread = new Thread(new ThreadStart(action)); }

        public void Start() { _thread.Start(); }

        public void Abort() { _thread.Abort(); }
    }

    public class ThreadWrapperInstance : IThreadWrapper
    {
        public IThread CreateThread(Action action) { return new ThreadClass(action); }

        public void Sleep(int milliseconds) { Thread.Sleep(milliseconds); }

        public void QueueUserWorkItem(Aragas.Core.Wrappers.WaitCallback waitCallback) { ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(waitCallback)); }
    }
}
