
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rougamo.OpenTelemetry;
using System.Diagnostics;
using TraceDemo.Core;

namespace TraceDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var otelOptions = new
            {
                ServiceName = "demo",
                ServiceVersion = "1.0.0.0",
                // 添加后要重启VS
                EndPoint = Environment.GetEnvironmentVariable("demo_end_point"),
            };
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IMyService, MyService>();
            builder.Services.AddScoped<MyService>();


            builder.Services.AddOpenTelemetry() // 注册 OpenTelemetry 服务
       .WithTracing(tracerProviderBuilder => // 配置分布式追踪系统
       {
           tracerProviderBuilder
               .AddSource(otelOptions.ServiceName) // 设置活动源名称，作为追踪数据的生产者
                 .AddRougamoSource() // 初始化Rougamo.OpenTelemetry
               .SetResourceBuilder( // 配置附加到所有遥测数据的资源信息
                   ResourceBuilder.CreateDefault()
                       .AddService( // 添加服务标识信息
                           serviceName: otelOptions.ServiceName,
                           serviceVersion: otelOptions.ServiceVersion)
                       .AddEnvironmentVariableDetector()) // 从环境变量自动检测资源标签
               .AddAspNetCoreInstrumentation(options => // 自动收集 ASP.NET Core HTTP 服务器数据
               {
                   options.RecordException = true; // 记录请求处理中发生的异常
                   options.Filter = httpContext => !httpContext.Request.Path.Value.Contains("/health"); // 过滤健康检查端点
                   options.EnrichWithHttpRequest = (activity, request) => // 自定义增强 HTTP 请求追踪数据
                   {
                       activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress); // 添加客户端 IP
                       activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString()); // 添加 User-Agent
                   };
               })
               .AddHttpClientInstrumentation(options => // 自动收集 HttpClient 发出的请求
               {
                   options.RecordException = true; // 记录客户端请求异常
                   options.FilterHttpRequestMessage =
                       request => !request.RequestUri.AbsolutePath.Contains("/metrics"); // 过滤指标请求
               })
               .SetSampler(new TraceIdRatioBasedSampler(0.1)) // 设置采样策略（10% 采样率，平衡性能与数据量）
               .AddOtlpExporter(options => // 配置 OTLP 导出器，发送数据到目标端点
               {
                   options.Endpoint = new Uri(otelOptions.EndPoint); // 设置导出目标地址
                   options.Protocol = OtlpExportProtocol.HttpProtobuf; // 使用 HTTP/Protobuf 协议
                   options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity> // 配置批量导出参数（性能优化）
                   {
                       MaxQueueSize = 2048, // 最大队列大小，达到此值时开始丢弃
                       ScheduledDelayMilliseconds = 1000, // 调度导出的延迟时间（毫秒）
                       ExporterTimeoutMilliseconds = 3000, // 导出超时时间（毫秒）
                       MaxExportBatchSize = 512 // 单次最大导出批量大小
                   };
                   // options.Headers = "Authorization=Bearer YOUR_API_KEY";                         // 设置认证头（如需要）
               })
               .AddConsoleExporter(options => // 添加控制台导出器（开发环境调试用）
               {
                   options.Targets = ConsoleExporterOutputTargets.Console; // 指定输出目标为控制台
               });
       })
       .WithMetrics(metricsProviderBuilder => // 配置指标收集系统
       {
           metricsProviderBuilder
               .AddMeter(otelOptions.ServiceName) // 设置指标仪表名称，作为指标数据的生产者
               .AddAspNetCoreInstrumentation() // 收集 ASP.NET Core 请求指标
               .AddHttpClientInstrumentation() // 收集 HttpClient 请求指标
               .AddRuntimeInstrumentation() // 收集 .NET 运行时指标（CPU/内存/线程等）
               .AddProcessInstrumentation() // 收集进程级指标（CPU/内存使用等）
               .AddReader(new PeriodicExportingMetricReader(
                   new OtlpMetricExporter(new OtlpExporterOptions()
                   {
                       Endpoint = new Uri(otelOptions.EndPoint),
                       Protocol = OtlpExportProtocol.HttpProtobuf
                   }),
                   exportIntervalMilliseconds: 60000 // 生产环境建议 60 秒
               ))
               // .AddReader(new PeriodicExportingMetricReader(   // 修正后的控制台导出器配置
               //     new ConsoleMetricExporter(new ConsoleExporterOptions()),
               //     exportIntervalMilliseconds: 1000 // 开发环境缩短间隔
               // ))
               ;
       });

            // 修改Rougamo.OpenTelemetry默认配置
            builder.Services.AddOpenTelemetryRougamo(options =>
            {
                options.ArgumentsStoreType = ArgumentsStoreType.Tag;
            });

            // 添加自定义活动源，用于手动创建追踪跨度
            builder.Services.AddSingleton<ActivitySource>(sp =>
                new ActivitySource(otelOptions.ServiceName));

            // 配置日志集成，将日志与追踪关联
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true; // 包含格式化后的日志消息
                options.IncludeScopes = true; // 包含日志范围信息
                options.ParseStateValues = true; // 解析状态值为单独属性
                options.AddOtlpExporter(otlpOptions => // 配置 OTLP 日志导出器
                {
                    otlpOptions.Endpoint = new Uri(otelOptions.EndPoint); // 设置日志导出目标地址
                    otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf; // 使用 HTTP/Protobuf 协议
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();


            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var activity = System.Diagnostics.Activity.Current;
                    if (activity != null && !string.IsNullOrEmpty(activity.TraceId.ToString()))
                    {
                        context.Response.Headers["trace-id"] = activity.TraceId.ToString();
                    }
                    return Task.CompletedTask;
                });

                await next();
            });


            app.Run();

        }
    }
}
