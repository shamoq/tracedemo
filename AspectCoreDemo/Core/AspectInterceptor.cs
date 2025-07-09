using AspectCore.DynamicProxy;

namespace AspectCoreDemo.Core
{
    public class AspectInterceptor : AbstractInterceptor
    {
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            try
            {
                // 前置处理
                Console.WriteLine($"开始执行方法: {context.ServiceMethod.Name}");

                // 执行被拦截的方法
                await next(context);

                // 后置处理
                Console.WriteLine($"方法执行完成: {context.ServiceMethod.Name}");
            }
            catch (Exception ex)
            {
                // 异常处理
                Console.WriteLine($"方法执行异常: {context.ServiceMethod.Name}, 异常: {ex.Message}");
                throw;
            }
        }
    }
}
