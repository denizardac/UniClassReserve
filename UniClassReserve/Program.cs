using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniClassReserve.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<UniClassReserve.Data.AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<UniClassReserve.Data.ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<UniClassReserve.Data.AppDbContext>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// ROLLERİ OLUŞTUR VE İLK KULLANICIYA ADMIN ROLÜ ATA
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UniClassReserve.Data.ApplicationUser>>();
    string[] roles = new[] { "Admin", "Instructor" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
    var users = userManager.Users.ToList();
    if (users.Count == 0)
    {
        // Otomatik admin kullanıcısı ekle
        var adminUser = new UniClassReserve.Data.ApplicationUser
        {
            UserName = "denizardacinarer@hotmail.com",
            Email = "denizardacinarer@hotmail.com",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, "DenizDarda06!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    else if (users.Count > 0)
    {
        // Sadece denizardacinarer@hotmail.com'a Admin rolü ata
        var adminUser = await userManager.FindByEmailAsync("denizardacinarer@hotmail.com");
        if (adminUser != null)
        {
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        // Diğer tüm kullanıcılara Instructor rolü ata
        foreach (var user in users)
        {
            if (user.Email != "denizardacinarer@hotmail.com" && !await userManager.IsInRoleAsync(user, "Instructor"))
                await userManager.AddToRoleAsync(user, "Instructor");
        }
    }
}

app.Run();
