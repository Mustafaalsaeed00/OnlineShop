using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OnlineShop.Models.Db;
using OnlineShop.Models.ViewModel;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnlineShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly OnlineShopContext _context;
		public AccountController(OnlineShopContext context)
		{
            _context = context;
		}
		[HttpGet]
		public IActionResult Register()
        {
            return View();
        }
		[HttpPost]
		public IActionResult Register(User user)
		{
			user.Email = user.Email?.Trim();
			user.FullName = user.FullName?.Trim();
			user.Password = user.Password?.Trim();
			user.RegisterDate = DateTime.Now;
			user.IsAdmin = false;
			user.RecoveryCode = 0;

			if (!ModelState.IsValid)
			{
				return View(user);
			}
			//------Valid Email Checking------------
			Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
			Match match = regex.Match(user.Email);
			if (!match.Success)
			{
				ModelState.AddModelError("Email", "Email is not valid");
				return View(user);
			}
			//--------Duplicate Email Checking---------
			var prevUser = _context.Users.Any(u => u.Email == user.Email);
			if(prevUser)
			{
				ModelState.AddModelError("Email", "Email is Used");
				return View(user);
			}
			_context.Add(user);
			_context.SaveChanges();
			return RedirectToAction("Login");
		}
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel user)
		{
			if(!ModelState.IsValid)
			{
				return View(user);
			}

			var foundUser = _context.Users.FirstOrDefault(u => u.Email == user.Email && u.Password == user.Password);
			if(foundUser is null)
			{
				ModelState.AddModelError("Email", "Email or Password is not valid!");
				return View(user);
			}

			var claims = new List<Claim>();
			claims.Add(new Claim(ClaimTypes.NameIdentifier, foundUser.Id.ToString()));
			claims.Add(new Claim(ClaimTypes.Name, foundUser.FullName.ToString()));
			claims.Add(new Claim(ClaimTypes.Email, foundUser.Email.ToString()));

			if (foundUser.IsAdmin)
			{
				claims.Add(new Claim(ClaimTypes.Role, "admin"));
			}
			else
			{
				claims.Add(new Claim(ClaimTypes.Role, "user"));
			}

			// Create an identity based on claims
			var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

			// Create a principal based on the identity
			var principal = new ClaimsPrincipal(identity);

			// Sign in the user with the created principal
			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			return Redirect("/");
		}

		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Login");
		}

		[HttpGet]
		public IActionResult RecoveryPassword()
		{
			return View();
		}

		[HttpPost]
		public IActionResult RecoveryPassword(RecoveryPasswordViewModel recoveryPassword)
		{
			if(!ModelState.IsValid)
			{
				return View(recoveryPassword);
			}

			//------Valid Email Checking------------
			Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
			Match match = regex.Match(recoveryPassword.Email);
			if (!match.Success)
			{
				ModelState.AddModelError("Email", "Email is not valid");
				return View(recoveryPassword);
			}

			//--------------------------------------
			var foundUser = _context.Users.FirstOrDefault(u => u.Email == recoveryPassword.Email);
			if(foundUser == null)
			{
				ModelState.AddModelError("Email", "Email is not Valid");
				return View(recoveryPassword);
			}

			//---------------------------------------

			foundUser.RecoveryCode = new Random().Next(10000, 100000);
			_context.Users.Update(foundUser);
			_context.SaveChanges();

			//----------------------------------------

			MailMessage mail = new MailMessage();
			SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

			mail.From = new MailAddress("mustafadrive00@gmail.com");
			mail.To.Add(foundUser.Email);
			mail.Subject = "Recovery Code";
			mail.Body = "Your Recovery Code is: " + foundUser.RecoveryCode;

			smtpServer.Port = 587;
			smtpServer.Credentials = new System.Net.NetworkCredential("mustafadrive00@gmail.com", "nsut bfec wskj ujsp");
			smtpServer.EnableSsl = true;
			smtpServer.Send(mail);
			//-------------------------------
			return Redirect($"/Account/ConfirmRecoveryCode?email={foundUser.Email}");

		}

		[HttpGet]
		public IActionResult ConfirmRecoveryCode(string email)
		{
			ConfirmRecoveryCodeViewModel confirm = new ConfirmRecoveryCodeViewModel();
			confirm.Email = email;
			TempData["RecoveryCode"] = "We have sent a Recovery Code To your Email";
			return View(confirm);
		}
		[HttpPost]
		public IActionResult ConfirmRecoveryCode(ConfirmRecoveryCodeViewModel confirmRecoveryCode)
		{
			if(!ModelState.IsValid)
			{
				return View(confirmRecoveryCode);
			}

			var foundUser = _context.Users.FirstOrDefault(u => u.Email == confirmRecoveryCode.Email);
			
			if(foundUser.RecoveryCode != confirmRecoveryCode.RecoveryCode)
			{
				ModelState.AddModelError("RecoveryCode", "Recovery Code is not valid");
				return View(confirmRecoveryCode);
			}
			ResetPasswordViewModel resetPasswordViewModel = new ResetPasswordViewModel();
			resetPasswordViewModel.Email = foundUser.Email;


			return View("ResetPassword" , resetPasswordViewModel);
		}
		[HttpGet]
		public IActionResult ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
		{
			return View(resetPasswordViewModel);
		}

		[HttpPost]
		public IActionResult ConfirmResetPassword(ResetPasswordViewModel resetPasswordViewModel)
		{
			if(!ModelState.IsValid)
			{
				return View("ResetPassword",resetPasswordViewModel);
			}

			var foundUser = _context.Users.FirstOrDefault(u => u.Email == resetPasswordViewModel.Email);

			foundUser.Password = resetPasswordViewModel.NewPassword;
			_context.Users.Update(foundUser);
			_context.SaveChanges();
			TempData["ResetPassword"] = "Password Reset Successfully";
			return RedirectToAction("Login");
		}

	}

}
