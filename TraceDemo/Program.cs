
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
                // ��Ӻ�Ҫ����VS
                EndPoint = Environment.GetEnvironmentVariable("demo_end_point"),
            };
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IMyService, MyService>();
            builder.Services.AddScoped<MyService>();


            builder.Services.AddOpenTelemetry() // ע�� OpenTelemetry ����
       .WithTracing(tracerProviderBuilder => // ���÷ֲ�ʽ׷��ϵͳ
       {
           tracerProviderBuilder
               .AddSource(otelOptions.ServiceName) // ���ûԴ���ƣ���Ϊ׷�����ݵ�������
                 .AddRougamoSource() // ��ʼ��Rougamo.OpenTelemetry
               .SetResourceBuilder( // ���ø��ӵ�����ң�����ݵ���Դ��Ϣ
                   ResourceBuilder.CreateDefault()
                       .AddService( // ��ӷ����ʶ��Ϣ
                           serviceName: otelOptions.ServiceName,
                           serviceVersion: otelOptions.ServiceVersion)
                       .AddEnvironmentVariableDetector()) // �ӻ��������Զ������Դ��ǩ
               .AddAspNetCoreInstrumentation(options => // �Զ��ռ� ASP.NET Core HTTP ����������
               {
                   options.RecordException = true; // ��¼�������з������쳣
                   options.Filter = httpContext => !httpContext.Request.Path.Value.Contains("/health"); // ���˽������˵�
                   options.EnrichWithHttpRequest = (activity, request) => // �Զ�����ǿ HTTP ����׷������
                   {
                       activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress); // ��ӿͻ��� IP
                       activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString()); // ��� User-Agent
                   };
               })
               .AddHttpClientInstrumentation(options => // �Զ��ռ� HttpClient ����������
               {
                   options.RecordException = true; // ��¼�ͻ��������쳣
                   options.FilterHttpRequestMessage =
                       request => !request.RequestUri.AbsolutePath.Contains("/metrics"); // ����ָ������
               })
               .SetSampler(new TraceIdRatioBasedSampler(0.1)) // ���ò������ԣ�10% �����ʣ�ƽ����������������
               .AddOtlpExporter(options => // ���� OTLP ���������������ݵ�Ŀ��˵�
               {
                   options.Endpoint = new Uri(otelOptions.EndPoint); // ���õ���Ŀ���ַ
                   options.Protocol = OtlpExportProtocol.HttpProtobuf; // ʹ�� HTTP/Protobuf Э��
                   options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity> // �����������������������Ż���
                   {
                       MaxQueueSize = 2048, // �����д�С���ﵽ��ֵʱ��ʼ����
                       ScheduledDelayMilliseconds = 1000, // ���ȵ������ӳ�ʱ�䣨���룩
                       ExporterTimeoutMilliseconds = 3000, // ������ʱʱ�䣨���룩
                       MaxExportBatchSize = 512 // ������󵼳�������С
                   };
                   // options.Headers = "Authorization=Bearer YOUR_API_KEY";                         // ������֤ͷ������Ҫ��
               })
               .AddConsoleExporter(options => // ��ӿ���̨���������������������ã�
               {
                   options.Targets = ConsoleExporterOutputTargets.Console; // ָ�����Ŀ��Ϊ����̨
               });
       })
       .WithMetrics(metricsProviderBuilder => // ����ָ���ռ�ϵͳ
       {
           metricsProviderBuilder
               .AddMeter(otelOptions.ServiceName) // ����ָ���Ǳ����ƣ���Ϊָ�����ݵ�������
               .AddAspNetCoreInstrumentation() // �ռ� ASP.NET Core ����ָ��
               .AddHttpClientInstrumentation() // �ռ� HttpClient ����ָ��
               .AddRuntimeInstrumentation() // �ռ� .NET ����ʱָ�꣨CPU/�ڴ�/�̵߳ȣ�
               .AddProcessInstrumentation() // �ռ����̼�ָ�꣨CPU/�ڴ�ʹ�õȣ�
               .AddReader(new PeriodicExportingMetricReader(
                   new OtlpMetricExporter(new OtlpExporterOptions()
                   {
                       Endpoint = new Uri(otelOptions.EndPoint),
                       Protocol = OtlpExportProtocol.HttpProtobuf
                   }),
                   exportIntervalMilliseconds: 60000 // ������������ 60 ��
               ))
               // .AddReader(new PeriodicExportingMetricReader(   // ������Ŀ���̨����������
               //     new ConsoleMetricExporter(new ConsoleExporterOptions()),
               //     exportIntervalMilliseconds: 1000 // �����������̼��
               // ))
               ;
       });

            // �޸�Rougamo.OpenTelemetryĬ������
            builder.Services.AddOpenTelemetryRougamo(options =>
            {
                options.ArgumentsStoreType = ArgumentsStoreType.Tag;
            });

            // ����Զ���Դ�������ֶ�����׷�ٿ��
            builder.Services.AddSingleton<ActivitySource>(sp =>
                new ActivitySource(otelOptions.ServiceName));

            // ������־���ɣ�����־��׷�ٹ���
            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true; // ������ʽ�������־��Ϣ
                options.IncludeScopes = true; // ������־��Χ��Ϣ
                options.ParseStateValues = true; // ����״ֵ̬Ϊ��������
                options.AddOtlpExporter(otlpOptions => // ���� OTLP ��־������
                {
                    otlpOptions.Endpoint = new Uri(otelOptions.EndPoint); // ������־����Ŀ���ַ
                    otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf; // ʹ�� HTTP/Protobuf Э��
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
