using ezExam.App.Services;
using ezExam.App.ViewModels;
using ezExam.Core.Services;
using ezExam.Data;
using ezExam.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ezExam.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {  
        // Bắt lỗi WPF không xử lý được
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(ex.Exception.ToString(), "Lỗi chi tiết");
            ex.Handled = true;
        };

        base.OnStartup(e);

        var services = new ServiceCollection();

        // --- Database ---
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite(DatabaseInitializer.GetConnectionString()));

        // --- Repositories ---
        services.AddScoped<ExamSessionRepository>();
        services.AddScoped<CandidateRepository>();
        services.AddScoped<ExamRoomRepository>();

        // --- Core Services ---
        services.AddScoped<ImportService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<IExamSessionService, ExamSessionService>();
        services.AddScoped<ICandidateService, CandidateService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // --- Algorithms ---
        services.AddScoped<IShiftSchedulerService, ShiftSchedulerService>();
        services.AddScoped<IRoomSchedulerService, RoomSchedulerService>();

        // --- ViewModels ---
        services.AddScoped<ExamSessionViewModel>();
        services.AddScoped<CandidateViewModel>();
        services.AddScoped<RoomViewModel>();
        services.AddScoped<MainViewModel>();

        Services = services.BuildServiceProvider();

        // Khởi tạo database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DatabaseInitializer.InitializeAsync(db);

        // Mở MainWindow
        var mainVM = Services.GetRequiredService<MainViewModel>();
        var win = new MainWindow { DataContext = mainVM };
        win.Show();
    }
}