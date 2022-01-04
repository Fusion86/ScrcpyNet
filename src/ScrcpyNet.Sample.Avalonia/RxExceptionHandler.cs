using ReactiveUI;
using Serilog;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;

namespace ScrcpyNet.Sample.Avalonia
{
    // I think this catches unhandled exceptions and then logs and throws them, but honestly I'm not sure.
    public class RxExceptionHandler : IObserver<Exception>
    {
        private static readonly ILogger log = Log.ForContext<RxExceptionHandler>();

        public void OnNext(Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
            log.Error("OnNext: {@Exception}", ex);
            RxApp.MainThreadScheduler.Schedule(() => { throw ex; });
        }

        public void OnError(Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
            log.Error("OnError: {@Exception}", ex);
            RxApp.MainThreadScheduler.Schedule(() => { throw ex; });
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached) Debugger.Break();
            log.Error("OnCompleted");
            RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
        }
    }
}
