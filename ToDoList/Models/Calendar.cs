using Microsoft.AspNetCore.Mvc;

namespace ToDoList.Models
{
    public class Calendar : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
