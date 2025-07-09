using Rougamo.OpenTelemetry;
using Rougamo;

namespace TraceDemo.Core
{
    public interface IMyService : IRougamo<OtelAttribute>
    {
        void DoSomething();

        Task<int> CalculateAsync(int a, int b);
    }

    public class BaseCurdService
    {
        public virtual void DoSomething2()
        {
            Console.WriteLine("Base执行具体业务逻辑...");
        }

        public virtual void DoSomething3()
        {
            Console.WriteLine("Base执行具体业务逻辑...");
        }
    }

    [LoggingMo]
    public class MyService : BaseCurdService, IMyService
    {
        private readonly IServiceProvider _serviceProvider;
        public MyService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public virtual void DoSomething()
        {
            DoSomething2();
            DoSomething3();
            var a = DoSomething4(111);
            Console.WriteLine("执行具体业务逻辑1..." + a);
        }

        public override void DoSomething2()
        {
            Console.WriteLine("执行具体业务逻辑2...");
        }

        public override void DoSomething3()
        {
            Console.WriteLine("执行具体业务逻辑3...");
        }

        public int DoSomething4(int i)
        {
            Console.WriteLine("执行具体业务逻辑3...");
            return i * 20;
        }

        public virtual async Task<int> CalculateAsync(int a, int b)
        {
            await Task.Delay(100);
            return a + b;
        }
    }
}
