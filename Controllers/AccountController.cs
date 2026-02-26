using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VegaFileConstructor.ViewModels;

namespace VegaFileConstructor.Controllers;

public class AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = new IdentityUser { UserName = vm.Email, Email = vm.Email };
        var result = await userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(vm);
        }
        await signInManager.SignInAsync(user, false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var result = await signInManager.PasswordSignInAsync(vm.Email, vm.Password, false, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Неверные учетные данные");
            return View(vm);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
