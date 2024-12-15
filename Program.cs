using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Server;

var builder = WebApplication.CreateBuilder(args);

// ��������� Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // ��������� ��������� API-����� � Swagger
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        Description = "������� API-���� � ���������"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] { }
        }
    });

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User API",
        Version = "v1",
        Description = "API ��� ���������� �������������� � �����������"
    });
});

// ������������ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:6000", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ����������� ��������
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// �������� Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // ��������� Swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User API V1");
        options.RoutePrefix = string.Empty; // ��������� Swagger UI �� ��������� ������
    });
}

app.MapHub<TopUsersHub>("/hubs/topUsers");
app.UseCors("AllowLocalhost");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();