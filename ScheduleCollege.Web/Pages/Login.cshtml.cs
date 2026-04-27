using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;

    public LoginModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    [Display(Name = "Логин")]
    public string Login { get; set; } = "";

    [BindProperty]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    public string ErrorMessage { get; set; } = "";

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Login == Login);

        if (user == null || !user.IsActive || !PasswordService.Verify(Password, user.PasswordHash))
        {
            ErrorMessage = "Неверный логин или пароль.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim("FullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToPage("/Index");
    }
}
