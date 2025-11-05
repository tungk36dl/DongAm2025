using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebFindLove.Controllers
{
	[AllowAnonymous]
	public class ErrorController : Controller
	{
		[HttpGet]
		public IActionResult AccessDenied()
		{
			Response.StatusCode = 403;
			return View("~/Views/Shared/AccessDenied.cshtml");
		}
	}
}


