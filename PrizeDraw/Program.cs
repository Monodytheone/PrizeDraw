using FluentValidation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PrizeDraw;
using PrizeDraw.Filters;
using PrizeDraw.Services;
using Serilog;
using Serilog.Events;
using SqlSugar;
using SqlSugar.DbConvert;

//Log.Logger = new LoggerConfiguration()
//.MinimumLevel.Debug()
//.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
//.MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
//.MinimumLevel.Override("Quartz", LogEventLevel.Warning)
//.Enrich.FromLogContext()
//.WriteTo.Async(c => c.File("logs/all/log-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug))
//.WriteTo.Async(c => c.File("logs/error/errorlog-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Error))
//.WriteTo.Async(c => c.Console())
//.CreateLogger();


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 跨域
var policyName = "CorsPolicy";
//string[] corsUrls = ["http://localhost:3001"];
//string[] corsUrls = builder.Configuration.GetSection("CorsUrls").Get<string[]>()
    //?? throw new Exception("配置中没有CorsUrls");
builder.Services.AddCors(options =>
{
    options.AddPolicy(policyName,builder =>
    {
        //builder.AllowAnyOrigin(corsUrls)
        builder
            //.WithOrigins(corsUrls)
            .SetIsOriginAllowed(_ => true)
            //.SetPreflightMaxAge(TimeSpan.FromSeconds(2520))
            .AllowAnyOrigin()
            .AllowAnyHeader()
            //.AllowCredentials() // 必须允许凭据传递（否则SingalR协商请求会跨域）
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PrizeDraw", Version = "v1", Description = "達康使勁啊！" });
    string basePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
    var xmlPath = Path.Combine(basePath, "PrizeDraw.xml");
    c.IncludeXmlComments(xmlPath, true);
    var entityXmlPath = Path.Combine(basePath, "PrizeDraw.xml");
    c.IncludeXmlComments(entityXmlPath, true);
});

// SqlSugar
builder.Services.AddSingleton<ISqlSugarClient>(s =>
{
    SqlSugarScope sqlSugar = new SqlSugarScope(new ConnectionConfig()
    {
        DbType = SqlSugar.DbType.SqlServer,
        ConnectionString = builder.Configuration.GetSection("ConnectionString").Value,
        IsAutoCloseConnection = true,
        ConfigureExternalServices = new ConfigureExternalServices
        {
            EntityService = (c, p) =>
            {
                // 除非通過ColumnDataType另外指定，字符串全都映射為nvarchar2
                if (c.PropertyType == typeof(string) && p.DataType.IsNullOrEmpty())
                {
                    p.DataType = "NVARCHAR";
                    p.SqlParameterDbType = typeof(Nvarchar2PropertyConvert);

                    // 若通過這種全局的方式設定為了NVARCHAR2，且未顯示指定Length，需要在此設定一個Length，否則會“"CONCURRENCYSTAMP" NVARCHAR2 NOT NULL”，因NVARCHAR2後沒有括號導致ORA-00906（點名AggregateRoot中的CONCURRENCYSTAMP屬性）
                    if (p.Length <= 0)
                    {
                        p.Length = 255;
                    }
                }
            }
        }
    },
    db =>
    {
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            Console.WriteLine(sql);
        };
    });
    return sqlSugar;
});


builder.Services.AddMvc(options =>
{
    options.Filters.Add<ExceptionLogFilter>();
    options.Filters.Add(typeof(AuthorizeFilter));
    options.Filters.Add(typeof(TransactionFilter));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddSignalR();

builder.Services.AddScoped<PrizeListPageService>();

builder.Services.AddMemoryCache();


var app = builder.Build();

app.UseCors(policyName);
app.UseCors();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();


app.UseAuthorization();

app.MapHub<PublicHub>("/PublicHub");

app.MapControllers();

app.Run();
