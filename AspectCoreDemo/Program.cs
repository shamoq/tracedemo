
using AspectCore.Configuration;
using AspectCore.Extensions.DependencyInjection;
using AspectCoreDemo.Core;

namespace AspectCoreDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ×¢²áÀ¹½ØÆ÷
            builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());
            builder.Services.ConfigureDynamicProxy(config =>
            {
                config.Interceptors.AddTyped<AspectInterceptor>(Predicates.ForService("*"));
            });

            builder.Services.AddScoped<IMyService, MyService>();
            builder.Services.AddScoped<MyService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
