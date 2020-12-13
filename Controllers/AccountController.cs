using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using CognitoMvc.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CognitoMvc.Controllers
{
    public class AccountController : Controller
    {
        // To register users
        private readonly UserManager<CognitoUser> _userManager;
        // To sign in and out the user
        private readonly SignInManager<CognitoUser> _signInManager;
        // Pool from cognito to get a user or create the new user
        private readonly CognitoUserPool _pool;

        // Required attributes - cognito template custom:{attribute}
        private const string Nombre = "custom:nombre";
        private const string ApellidoPaterno = "custom:apellidoPaterno";
        private const string ApellidoMaterno = "custom:apellidoMaterno";
        private const string Otro = "custom:otro";

        public AccountController(UserManager<CognitoUser> userManager, SignInManager<CognitoUser> signInManager,
            CognitoUserPool pool)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _pool = pool;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterUserViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);
            // As we are creating a new user, this method will retun 
            // a object of type CognitoUser with the username set
            var user = _pool.GetUser(viewModel.UserName);

            // Adding the csutom attributes
            // Remember that all attributes must be set
            user.Attributes.Add(CognitoAttribute.Email.AttributeName, viewModel.Email);
            user.Attributes.Add(Nombre, viewModel.Nombre);
            user.Attributes.Add(ApellidoPaterno, viewModel.ApellidoPaterno);
            user.Attributes.Add(ApellidoMaterno, viewModel.ApellidoMaterno);
            user.Attributes.Add(Otro, viewModel.Otro);

            // Create a new user as usual
            var result = await _userManager.CreateAsync(user, viewModel.Password);
            if (result.Succeeded)
            {
                // If the user is created correctly, we sign them in
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            // Add erros on failure
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);
            // We use the sign in methods as usual
            var result = await _signInManager.PasswordSignInAsync(viewModel.UserName, viewModel.Password, viewModel.RememberMe, false);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            // Depending in how we configured our User Pool, we might not need this part
            if (result.IsNotAllowed)
                ModelState.AddModelError(string.Empty, "Por favor verifica tu correo");
            return View(viewModel);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
