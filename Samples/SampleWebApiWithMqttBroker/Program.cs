using MqttServices.Core.Broker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// for Broker
builder.Services.AddMqttBrokerService(opts => builder.Configuration.GetSection(nameof(MqttBrokerSettings)).Bind(opts));

var app = builder.Build();

// warm MQTT-Broker up:
app.Services.GetService<IMqttBrokerService>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
