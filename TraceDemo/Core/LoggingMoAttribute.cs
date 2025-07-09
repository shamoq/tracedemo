using Rougamo;
using Rougamo.Context;
using System.Diagnostics;

namespace TraceDemo.Core
{
    public class LoggingMoAttribute : MoAttribute
    {
        public override void OnEntry(MethodContext context)
        {
            Console.WriteLine($"[Rougamo] 进入方法: {context.Method.Name}");

            //var MyActivitySource = new ActivitySource("demo");
            //using var activity = MyActivitySource.StartActivity(context.Method.Name);
        }

        public override void OnExit(MethodContext context)
        {
            Console.WriteLine($"[Rougamo] 退出方法: {context.Method.Name}");
        }
    }
}
