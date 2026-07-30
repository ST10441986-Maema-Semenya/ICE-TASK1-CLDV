using UniversityEnrollment.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind the "AzureStorage" section of appsettings.json (or environment
// variables / Azure App Service configuration in production) to AzureStorageOptions.
// ValidateOnStart() means a missing/empty connection string fails immediately at
// app startup with a clear message, instead of surfacing later as a raw
// ArgumentNullException the first time some service tries to use it.
builder.Services.AddOptions<AzureStorageOptions>()
    .Bind(builder.Configuration.GetSection(AzureStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// (e) Table/queue-backed services. Registered as scoped since each holds a
// TableClient/QueueClient, which are thread-safe and cheap to resolve per request.
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<EnrollmentQueueSender>();

// (h) Background worker that continuously processes CourseEnrollmentQueue.
builder.Services.AddHostedService<EnrollmentQueueProcessor>();

// MVC with Razor views (server-rendered pages) instead of a bare Web API.
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
