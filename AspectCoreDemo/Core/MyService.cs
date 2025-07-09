namespace AspectCoreDemo.Core
{
    public interface IMyService
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

    public class MyService : BaseCurdService, IMyService
    {
        private readonly IServiceProvider _serviceProvider;
        public MyService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public virtual void DoSomething()
        {
            // 内部调用方法，要获取代理类调用
            var proxy = _serviceProvider.GetRequiredService<MyService>();
            proxy.DoSomething2();
            proxy.DoSomething3();
            Console.WriteLine("执行具体业务逻辑1...");
        }

        public override void DoSomething2()
        {
            Console.WriteLine("执行具体业务逻辑2...");
        }

        public override void DoSomething3()
        {
            Console.WriteLine("执行具体业务逻辑3...");
        }

        public virtual async Task<int> CalculateAsync(int a, int b)
        {
            await Task.Delay(100);
            return a + b;
        }
    }
}
