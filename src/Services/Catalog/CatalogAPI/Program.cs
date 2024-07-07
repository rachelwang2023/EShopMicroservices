using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(5050, listenOptions =>
//     {
//         listenOptions.UseHttps();
//     });
// });
// 添加健康检查服务
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

// 添加健康检查 UI 服务
builder.Services.AddHealthChecksUI(setupSettings: setup =>
{
    setup.SetEvaluationTimeInSeconds(10000); // 每10秒检查一次
    setup.MaximumHistoryEntriesPerEndpoint(50); // 保留最多50个历史条目
    setup.AddHealthCheckEndpoint("default", "/healthz");
}).AddInMemoryStorage(); // 使用 In-Memory 数据库存储

// 添加 Swagger 服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });
});
builder.Services.AddCarter();
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// 添加 Marten 配置
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

var app = builder.Build();

// 启用 Swagger，仅在开发环境中启用
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API V1");
        c.RoutePrefix = "swagger"; // 设置 Swagger UI 路径为 /swagger
    });
}

// 使用健康检查中间件
app.UseHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// 使用健康检查 UI 中间件
app.UseHealthChecksUI(config =>
{
    config.UIPath = "/healthchecks-ui"; // 健康检查 UI 的路径
    config.ApiPath = "/health-ui-api"; // 健康检查 API 的路径
});

app.MapCarter();

app.Run();
