using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Reception)]
    public class ReceptionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}