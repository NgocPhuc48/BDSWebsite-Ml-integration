var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Views";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
